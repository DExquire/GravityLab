using GravityLab.Chambers;
using GravityLab.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GravityLab.UI
{
    /// <summary>Title, continue / chambers / settings / quit, over the drifting chamber camera.</summary>
    public sealed class MainMenuScreen : UIScreen
    {
        RectTransform _title;
        UIButton _continue;
        RectTransform[] _buttons;
        Text _progress;

        protected override void Build(UIManager ui)
        {
            Image side = UIFactory.Image(Root, "Side", UISprites.SideGradient, new Color(0.96f, 0.97f, 0.99f, 0.93f));
            side.rectTransform.anchorMin = Vector2.zero;
            side.rectTransform.anchorMax = new Vector2(0f, 1f);
            side.rectTransform.pivot = new Vector2(0f, 0.5f);
            side.rectTransform.sizeDelta = new Vector2(1250f, 0f);
            side.rectTransform.anchoredPosition = Vector2.zero;

            _title = UIFactory.Place(UIFactory.Rect(Root, "Title"), new Vector2(0f, 1f), new Vector2(140f, -150f), new Vector2(900f, 260f));
            Text title = UIFactory.Label(_title, "GRAVITY<color=#2FA4FF>LAB</color>", Fonts.Title, 150, Palette.UiText, TextAnchor.UpperLeft);
            UIFactory.Stretch(title.rectTransform);
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            Shadow shadow = title.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.1f, 0.3f, 0.6f, 0.18f);
            shadow.effectDistance = new Vector2(0f, -8f);
            Text subtitle = UIFactory.Label(_title, "PHYSICS TESTING FACILITY  ·  20 CHAMBERS", Fonts.Bold, 30, Palette.UiMuted, TextAnchor.UpperLeft);
            UIFactory.Place(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(8f, -175f), new Vector2(900f, 40f));
            Image accent = UIFactory.Image(_title, "Accent", UISprites.Rounded, Palette.Orange);
            UIFactory.Place(accent.rectTransform, new Vector2(0f, 1f), new Vector2(10f, -232f), new Vector2(120f, 10f));

            _continue = UIFactory.Button(Root, "CONTINUE", new Vector2(480f, 88f), ButtonStyle.Primary, () => ui.Game.StartChamber(SaveSystem.Data.Unlocked - 1));
            UIButton chambers = UIFactory.Button(Root, "CHAMBERS", new Vector2(480f, 80f), ButtonStyle.Secondary, ui.ShowChamberSelect);
            UIButton settings = UIFactory.Button(Root, "SETTINGS", new Vector2(480f, 80f), ButtonStyle.Secondary, ui.ShowSettings);
            UIButton quit = UIFactory.Button(Root, "QUIT", new Vector2(480f, 80f), ButtonStyle.Ghost, Quit);
            _buttons = new[] { (RectTransform)_continue.transform, (RectTransform)chambers.transform, (RectTransform)settings.transform, (RectTransform)quit.transform };
            for (int i = 0; i < _buttons.Length; i++) UIFactory.Place(_buttons[i], new Vector2(0f, 1f), new Vector2(140f, -500f - i * 104f), _buttons[i].sizeDelta);

            _progress = UIFactory.Label(Root, "", Fonts.Body, 26, Palette.UiMuted, TextAnchor.LowerLeft);
            UIFactory.Place(_progress.rectTransform, new Vector2(0f, 0f), new Vector2(144f, 60f), new Vector2(900f, 80f));
        }

        protected override void OnShow()
        {
            SaveData d = SaveSystem.Data;
            int next = Mathf.Clamp(d.Unlocked, 1, ChamberLibrary.Count);
            _continue.Label.text = d.Unlocked <= 1 && SaveSystem.BestTime(0) <= 0f ? "START TESTING" : "CONTINUE  ·  " + next.ToString("00");

            int completed = 0, sRanks = 0;
            for (int i = 0; i < ChamberLibrary.Count; i++)
            {
                if (SaveSystem.BestTime(i) > 0f) completed++;
                if (SaveSystem.BestRank(i) == Rank.S) sRanks++;
            }
            _progress.text = "Chambers cleared  <b>" + completed + "/" + ChamberLibrary.Count + "</b>     S ranks  <b>" + sRanks + "</b>\n<size=20>Mouse + keyboard  ·  Esc to pause in tests</size>";

            for (int i = 0; i < _buttons.Length; i++)
            {
                RectTransform b = _buttons[i];
                Vector2 rest = new Vector2(140f, -500f - i * 104f);
                b.anchoredPosition = rest + new Vector2(-60f, 0f);
                Tweener.Run(0.35f, t => b.anchoredPosition = rest + new Vector2(-60f * (1f - Tweener.EaseOutCubic(t)), 0f), null, 0.05f + i * 0.06f);
            }
        }

        void Update()
        {
            float bob = Mathf.Sin(Time.unscaledTime * 1.3f) * 6f;
            _title.anchoredPosition = new Vector2(140f, -150f + bob);
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
