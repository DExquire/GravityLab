using GravityLab.Chambers;
using GravityLab.Core;
using GravityLab.Gadgets;
using UnityEngine;
using UnityEngine.UI;

namespace GravityLab.UI
{
    /// <summary>In-test HUD: chamber label, timer with par, crosshair, key hints and narrator subtitles.</summary>
    public sealed class HudScreen : UIScreen
    {
        UIManager _ui;
        Text _chamberLabel;
        Text _chamberName;
        Text _timer;
        Text _par;
        SubtitleView _subtitles;
        RectTransform _crosshairParent;
        int _shownTenths = -1;
        int _shownIndex = -1;

        protected override void Build(UIManager ui)
        {
            _ui = ui;
            RectTransform pill = UIFactory.Place(UIFactory.Rect(Root, "ChamberPill"), new Vector2(0f, 1f), new Vector2(40f, -36f), new Vector2(420f, 150f));
            Image bg = UIFactory.Image(pill, "Bg", UISprites.Rounded, new Color(0.06f, 0.08f, 0.12f, 0.6f));
            UIFactory.Stretch(bg.rectTransform);
            _chamberLabel = UIFactory.Label(pill, "", Fonts.Bold, 20, Palette.Blue, TextAnchor.UpperLeft);
            UIFactory.Place(_chamberLabel.rectTransform, new Vector2(0f, 1f), new Vector2(26f, -14f), new Vector2(380f, 28f));
            _chamberName = UIFactory.Label(pill, "", Fonts.Title, 34, Color.white, TextAnchor.UpperLeft);
            UIFactory.Place(_chamberName.rectTransform, new Vector2(0f, 1f), new Vector2(26f, -40f), new Vector2(380f, 44f));
            _timer = UIFactory.Label(pill, "", Fonts.Title, 44, Color.white, TextAnchor.LowerLeft);
            UIFactory.Place(_timer.rectTransform, new Vector2(0f, 0f), new Vector2(26f, 12f), new Vector2(220f, 56f));
            _par = UIFactory.Label(pill, "", Fonts.Body, 20, new Color(1f, 1f, 1f, 0.6f), TextAnchor.LowerRight);
            UIFactory.Place(_par.rectTransform, new Vector2(1f, 0f), new Vector2(-24f, 20f), new Vector2(170f, 30f));

            RectTransform keys = UIFactory.Place(UIFactory.Rect(Root, "Keys"), new Vector2(1f, 0f), new Vector2(-36f, 36f), new Vector2(430f, 196f));
            Image keysBg = UIFactory.Image(keys, "Bg", UISprites.Rounded, new Color(0.06f, 0.08f, 0.12f, 0.45f));
            UIFactory.Stretch(keysBg.rectTransform);
            Text keyText = UIFactory.Label(keys,
                "<color=#7CC8FF>WASD</color> Move   <color=#7CC8FF>SPACE</color> Jump\n" +
                "<color=#7CC8FF>CTRL</color> Crouch   <color=#7CC8FF>SHIFT</color> Sprint\n" +
                "<color=#FFB060>LMB</color> Grab   <color=#FFB060>RMB</color> Launch\n" +
                "<color=#C9A6FF>F</color> Flip gravity   <color=#7FF2FF>Q</color> Freeze\n" +
                "<color=#7CC8FF>R</color> Restart   <color=#7CC8FF>ESC</color> Pause",
                Fonts.Body, 21, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleLeft);
            UIFactory.Stretch(keyText.rectTransform);
            keyText.rectTransform.offsetMin = new Vector2(24f, 10f);
            keyText.rectTransform.offsetMax = new Vector2(-16f, -10f);
            keyText.lineSpacing = 1.1f;

            _crosshairParent = UIFactory.Stretch(UIFactory.Rect(Root, "CrosshairArea"));
            _subtitles = SubtitleView.Create(Root);
        }

        /// <summary>Connects the crosshair to the gun (the gun is created after the UI).</summary>
        public void Bind(GravityGun gun)
        {
            Crosshair.Create(_crosshairParent, gun);
        }

        public void Say(string line) { _subtitles.Enqueue(line); }
        public void ClearSubtitles() { _subtitles.Clear(); }

        void Update()
        {
            GameManager game = _ui.Game;
            if (game.ChamberIndex != _shownIndex)
            {
                _shownIndex = game.ChamberIndex;
                ChamberDef def = game.CurrentDef;
                _chamberLabel.text = "TEST CHAMBER " + (_shownIndex + 1).ToString("00") + " / " + ChamberLibrary.Count;
                _chamberName.text = def.Name;
                _par.text = "Par " + RankRules.FormatTime(def.Par);
            }

            int tenths = Mathf.FloorToInt(game.Timer * 10f);
            if (tenths != _shownTenths)
            {
                _shownTenths = tenths;
                _timer.text = RankRules.FormatTime(Mathf.Max(0.01f, game.Timer));
                _timer.color = game.Timer <= game.CurrentDef.Par ? Color.white : new Color(1f, 0.75f, 0.45f);
            }
        }
    }
}
