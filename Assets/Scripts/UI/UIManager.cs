using System;
using GravityLab.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem.UI;
#endif

namespace GravityLab.UI
{
    /// <summary>Owns the overlay canvas, every screen, the vignette, screen flashes and fade transitions.</summary>
    public sealed class UIManager : MonoBehaviour
    {
        Image _fader;
        Image _flash;
        Image _vignette;
        Coroutine _fadeRoutine;
        Coroutine _flashRoutine;
        UIScreen _settingsReturn;

        public GameManager Game { get; private set; }
        public MainMenuScreen MainMenu { get; private set; }
        public ChamberSelectScreen ChamberSelect { get; private set; }
        public HudScreen Hud { get; private set; }
        public PauseScreen PauseMenu { get; private set; }
        public SettingsScreen Settings { get; private set; }
        public ResultScreen Result { get; private set; }

        public static UIManager Create(GameManager game, Transform parent)
        {
            GameObject go = new GameObject("UI", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            CreateEventSystem(parent);

            UIManager ui = go.AddComponent<UIManager>();
            ui.Game = game;
            ui.Build();
            return ui;
        }

        static void CreateEventSystem(Transform parent)
        {
            if (EventSystem.current != null) return;
            GameObject es = new GameObject("EventSystem");
            es.transform.SetParent(parent, false);
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            es.AddComponent<InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        void Build()
        {
            _vignette = UIFactory.Image(transform, "Vignette", UISprites.Vignette, new Color(1f, 1f, 1f, 0.55f));
            UIFactory.Stretch(_vignette.rectTransform);

            Hud = CreateScreen<HudScreen>("Hud");
            MainMenu = CreateScreen<MainMenuScreen>("MainMenu");
            ChamberSelect = CreateScreen<ChamberSelectScreen>("ChamberSelect");
            PauseMenu = CreateScreen<PauseScreen>("Pause");
            Result = CreateScreen<ResultScreen>("Result");
            Settings = CreateScreen<SettingsScreen>("Settings");

            _flash = UIFactory.Image(transform, "Flash", null, new Color(1f, 1f, 1f, 0f));
            UIFactory.Stretch(_flash.rectTransform);
            _fader = UIFactory.Image(transform, "Fader", null, new Color(0.03f, 0.05f, 0.08f, 1f));
            UIFactory.Stretch(_fader.rectTransform);
            Tweener.Run(0.6f, t => SetFader(1f - t), null, 0.15f);
        }

        T CreateScreen<T>(string name) where T : UIScreen
        {
            RectTransform rt = UIFactory.Stretch(UIFactory.Rect(transform, name));
            rt.gameObject.AddComponent<CanvasGroup>();
            T screen = rt.gameObject.AddComponent<T>();
            screen.Setup(this);
            return screen;
        }

        void ShowOnly(UIScreen screen, bool keepHud = false)
        {
            UIScreen[] all = { MainMenu, ChamberSelect, PauseMenu, Settings, Result, Hud };
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == screen || (keepHud && all[i] == Hud)) continue;
                all[i].Hide();
            }
            if (screen != null && !screen.IsVisible) screen.Show();
            if (keepHud && !Hud.IsVisible) Hud.Show();
            _vignette.color = new Color(1f, 1f, 1f, Hud.IsVisible ? 0.55f : 0.35f);
        }

        public void ShowMainMenu() { ShowOnly(MainMenu); }
        public void ShowChamberSelect() { ShowOnly(ChamberSelect); }
        public void ShowHud() { ShowOnly(Hud); }
        public void ShowPause() { ShowOnly(PauseMenu, true); }

        public void ShowSettings()
        {
            _settingsReturn = PauseMenu.IsVisible ? (UIScreen)PauseMenu : MainMenu;
            ShowOnly(Settings, _settingsReturn == PauseMenu);
        }

        public void ShowResult(ResultInfo info)
        {
            Result.SetResult(info);
            ShowOnly(Result, true);
        }

        public void ShowFail()
        {
            Result.SetFailed(Game.CurrentDef.Name);
            ShowOnly(Result, true);
        }

        /// <summary>Handles Esc inside menus. Returns true when it navigated back.</summary>
        public bool Back()
        {
            if (Settings.IsVisible)
            {
                if (_settingsReturn == PauseMenu) ShowPause();
                else ShowMainMenu();
                return true;
            }
            if (ChamberSelect.IsVisible)
            {
                ShowMainMenu();
                return true;
            }
            return false;
        }

        /// <summary>Fades to dark, runs <paramref name="midpoint"/>, then fades back in.</summary>
        public void Fade(Action midpoint)
        {
            Tweener.Cancel(_fadeRoutine);
            float from = _fader.color.a;
            _fader.raycastTarget = true;
            _fadeRoutine = Tweener.Run(0.3f, t => SetFader(Mathf.Lerp(from, 1f, t)), () =>
            {
                midpoint();
                _fadeRoutine = Tweener.Run(0.45f, t => SetFader(1f - t), () => _fader.raycastTarget = false, 0.1f);
            });
        }

        void SetFader(float alpha)
        {
            Color c = _fader.color;
            c.a = alpha;
            _fader.color = c;
        }

        public void Flash(Color color, float duration)
        {
            Tweener.Cancel(_flashRoutine);
            float peak = color.a;
            _flashRoutine = Tweener.Run(duration, t =>
            {
                color.a = peak * (1f - t) * (1f - t);
                _flash.color = color;
            });
        }
    }
}
