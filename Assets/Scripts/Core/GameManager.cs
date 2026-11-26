using System.Collections;
using GravityLab.Audio;
using GravityLab.Chambers;
using GravityLab.Gadgets;
using GravityLab.Player;
using GravityLab.Rendering;
using GravityLab.UI;
using UnityEngine;

namespace GravityLab.Core
{
    public enum GameState { Menu, Loading, Intro, Playing, Paused, Exiting, Results, Dead }

    /// <summary>
    /// Game flow: main menu (with a chamber drifting behind it), loading chambers, the test timer, pause,
    /// death by goo, exit-elevator completion with ranks, and saving progress.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        PlayerController _player;
        GravityGun _gun;
        MenuCamera _menuCamera;
        UIManager _ui;
        Chamber _chamber;
        int _index;
        Coroutine _flow;

        public GameState State { get; private set; }
        public int ChamberIndex { get { return _index; } }
        public ChamberDef CurrentDef { get { return ChamberLibrary.Get(_index); } }
        public float Timer { get; private set; }
        public GravityGun Gun { get { return _gun; } }

        public void Init(PlayerController player, GravityGun gun, MenuCamera menuCamera, UIManager ui)
        {
            _player = player;
            _gun = gun;
            _menuCamera = menuCamera;
            _ui = ui;
            _ui.Hud.Bind(gun);
        }

        void Start()
        {
            _index = Mathf.Clamp(SaveSystem.Data.Unlocked - 1, 0, ChamberLibrary.Count - 1);
            EnterMenu();
        }

        // ---- flow --------------------------------------------------------------------------------

        void EnterMenu()
        {
            StopFlow();
            Time.timeScale = 1f;
            _gun.ResetState();
            BuildChamber(_index);
            SetPlayerActive(false);
            _menuCamera.gameObject.SetActive(true);
            _menuCamera.Frame(_chamber.Center, _chamber.Size);
            State = GameState.Menu;
            SetCursorLocked(false);
            _ui.ShowMainMenu();
        }

        public void ReturnToMenu()
        {
            Time.timeScale = 1f;
            State = GameState.Loading;
            _ui.Fade(EnterMenu);
        }

        public void StartChamber(int index)
        {
            if (State == GameState.Loading) return;
            _index = Mathf.Clamp(index, 0, ChamberLibrary.Count - 1);
            Time.timeScale = 1f;
            State = GameState.Loading;
            SetCursorLocked(true);
            _ui.Fade(LoadCurrent);
        }

        public void Restart() { StartChamber(_index); }

        public void NextChamber()
        {
            if (_index + 1 < ChamberLibrary.Count) StartChamber(_index + 1);
            else ReturnToMenu();
        }

        void LoadCurrent()
        {
            StopFlow();
            BuildChamber(_index);
            _menuCamera.gameObject.SetActive(false);
            SetPlayerActive(true);
            _player.Teleport(_chamber.SpawnPosition, _chamber.SpawnYaw);
            _player.InputEnabled = false;
            _gun.ResetState();
            CameraShake.Clear();
            Timer = 0f;
            State = GameState.Intro;
            _ui.ShowHud();
            _flow = StartCoroutine(IntroRoutine());
        }

        IEnumerator IntroRoutine()
        {
            yield return new WaitForSeconds(0.7f);
            _chamber.BeginTest();
            _player.InputEnabled = true;
            State = GameState.Playing;
            string[] intro = CurrentDef.Intro;
            for (int i = 0; i < intro.Length; i++) Narrate(intro[i]);
        }

        void BuildChamber(int index)
        {
            if (_chamber != null)
            {
                _chamber.Exit.Reached -= OnExitReached;
                Destroy(_chamber.gameObject);
            }
            _ui.Hud.ClearSubtitles();
            GameObject root = new GameObject("Chamber " + (index + 1));
            _chamber = root.AddComponent<Chamber>();
            _chamber.Index = index;
            ChamberDef def = ChamberLibrary.Get(index);
            def.Build(new ChamberBuilder(_chamber, def, Narrate));
            _chamber.Exit.Reached += OnExitReached;
        }

        public void Narrate(string line)
        {
            _ui.Hud.Say(line);
        }

        void Update()
        {
            switch (State)
            {
                case GameState.Playing:
                    Timer += Time.deltaTime;
                    if (GameInput.PauseDown) Pause();
                    else if (GameInput.RestartDown) Restart();
                    else if (_player.transform.position.y < _chamber.KillY) Die();
                    break;
                case GameState.Paused:
                    if (GameInput.PauseDown && !_ui.Back()) Resume();
                    break;
                case GameState.Menu:
                    if (GameInput.PauseDown) _ui.Back();
                    break;
                case GameState.Results:
                case GameState.Dead:
                    if (GameInput.RestartDown) Restart();
                    else if (GameInput.ConfirmDown)
                    {
                        if (State == GameState.Results) NextChamber();
                        else Restart();
                    }
                    break;
            }
        }

        public void Pause()
        {
            if (State != GameState.Playing) return;
            State = GameState.Paused;
            Time.timeScale = 0f;
            _player.InputEnabled = false;
            SetCursorLocked(false);
            _ui.ShowPause();
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            State = GameState.Playing;
            Time.timeScale = 1f;
            _player.InputEnabled = true;
            SetCursorLocked(true);
            _ui.ShowHud();
        }

        void Die()
        {
            State = GameState.Dead;
            _player.InputEnabled = false;
            _gun.ResetState();
            AudioManager.Play(SoundId.Fail);
            CameraShake.Add(0.5f);
            _ui.Flash(new Color(0.3f, 1f, 0.5f, 0.6f), 0.6f);
            SetCursorLocked(false);
            _ui.ShowFail();
        }

        void OnExitReached()
        {
            if (State != GameState.Playing) return;
            State = GameState.Exiting;
            _player.InputEnabled = false;
            _gun.ResetState();
            _flow = StartCoroutine(ExitRoutine());
        }

        IEnumerator ExitRoutine()
        {
            float time = Timer;
            yield return new WaitForSeconds(0.6f);
            AudioManager.Play(SoundId.Elevator);
            CameraShake.Add(0.25f);
            yield return new WaitForSeconds(1.1f);

            ChamberDef def = CurrentDef;
            Rank rank = RankRules.ForTime(time, def.Par);
            float previousBest = SaveSystem.BestTime(_index);
            bool newBest = SaveSystem.RecordCompletion(_index, time, rank);
            AudioManager.Play(SoundId.Complete);
            State = GameState.Results;
            SetCursorLocked(false);
            _ui.ShowResult(new ResultInfo
            {
                ChamberIndex = _index,
                Name = def.Name,
                Time = time,
                PreviousBest = previousBest,
                Par = def.Par,
                Rank = rank,
                NewBest = newBest,
                IsLast = _index + 1 >= ChamberLibrary.Count
            });
        }

        void StopFlow()
        {
            if (_flow != null) StopCoroutine(_flow);
            _flow = null;
        }

        void SetPlayerActive(bool active)
        {
            _player.gameObject.SetActive(active);
            AudioManager.Listener = active ? _player.Head : _menuCamera.transform;
        }

        static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void OnApplicationFocus(bool focused)
        {
            if (focused && State == GameState.Playing) SetCursorLocked(true);
            else if (!focused && State == GameState.Playing) Pause();
        }
    }
}
