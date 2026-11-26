using System.Collections.Generic;
using GravityLab.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GravityLab.UI
{
    /// <summary>PC settings: volumes, mouse, FOV, head bob, window mode, resolution, quality preset, vsync. Saved on change.</summary>
    public sealed class SettingsScreen : UIScreen
    {
        const float RowWidth = 660f;

        protected override void Build(UIManager ui)
        {
            Image dim = UIFactory.Image(Root, "Dim", null, new Color(0.04f, 0.06f, 0.1f, 0.6f), true);
            UIFactory.Stretch(dim.rectTransform);
            RectTransform card = UIFactory.Card(Root, "Card", new Vector2(1560f, 940f), Palette.UiCard);

            Text title = UIFactory.Label(card, "SETTINGS", Fonts.Title, 64, Palette.UiText);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(600f, 80f));

            RectTransform left = Column(card, "AUDIO & CONTROLS", -380f);
            RectTransform right = Column(card, "DISPLAY", 380f);
            SaveData d = SaveSystem.Data;

            List<RectTransform> leftRows = new List<RectTransform>();
            leftRows.Add(SliderRow(left, "Master volume", d.MasterVolume, Percent, v => { d.MasterVolume = v; DisplaySettings.ApplyAudio(); }));
            leftRows.Add(SliderRow(left, "Music", d.MusicVolume, Percent, v => { d.MusicVolume = v; DisplaySettings.ApplyAudio(); }));
            leftRows.Add(SliderRow(left, "Effects", d.SfxVolume, Percent, v => { d.SfxVolume = v; DisplaySettings.ApplyAudio(); }));
            leftRows.Add(SliderRow(left, "Mouse sensitivity", (d.MouseSensitivity - 0.2f) / 5.8f, v => (0.2f + v * 5.8f).ToString("0.0"), v => d.MouseSensitivity = 0.2f + v * 5.8f));
            leftRows.Add(ToggleRow(left, "Invert mouse Y", d.InvertY, v => d.InvertY = v));
            leftRows.Add(SliderRow(left, "Field of view", (d.FieldOfView - 60f) / 40f, v => Mathf.RoundToInt(60f + v * 40f).ToString(), v => d.FieldOfView = Mathf.Round(60f + v * 40f)));
            leftRows.Add(ToggleRow(left, "Head bob", d.HeadBob, v => d.HeadBob = v));
            UIFactory.Stack(leftRows.ToArray(), 66f, 8f);

            List<RectTransform> rightRows = new List<RectTransform>();
            rightRows.Add(ToggleRow(right, "Fullscreen", d.Fullscreen, v => { d.Fullscreen = v; DisplaySettings.ApplyDisplay(); }));
            rightRows.Add(StepperRow(right, "Resolution", DescribeResolution, () => StepResolution(-1), () => StepResolution(1)));
            rightRows.Add(StepperRow(right, "Quality", () => DisplaySettings.QualityName(d.QualityLevel), () => StepQuality(-1), () => StepQuality(1)));
            rightRows.Add(ToggleRow(right, "V-Sync", d.VSync, v => { d.VSync = v; DisplaySettings.ApplyQuality(); }));
            UIFactory.Stack(rightRows.ToArray(), 66f, 8f);

            Text note = UIFactory.Label(right, "Display changes apply immediately. In the editor, resolution\nand window mode are controlled by the Game view.", Fonts.Body, 20, Palette.UiMuted, TextAnchor.UpperLeft);
            UIFactory.Place(note.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -380f), new Vector2(RowWidth, 60f));

            UIButton back = UIFactory.Button(card, "BACK", new Vector2(320f, 76f), ButtonStyle.Primary, () => ui.Back());
            UIFactory.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(320f, 76f));
        }

        static RectTransform Column(RectTransform card, string header, float x)
        {
            RectTransform col = UIFactory.Place(UIFactory.Rect(card, header), new Vector2(0.5f, 1f), new Vector2(x, -130f), new Vector2(RowWidth, 640f));
            Text h = UIFactory.Label(col, header, Fonts.Bold, 24, Palette.Blue, TextAnchor.MiddleLeft);
            UIFactory.Place(h.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(RowWidth, 40f));
            Image line = UIFactory.Image(col, "Line", null, new Color(0.82f, 0.86f, 0.9f));
            UIFactory.Place(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(RowWidth, 2f));
            return col;
        }

        static string Percent(float v) { return Mathf.RoundToInt(v * 100f) + "%"; }

        static RectTransform SliderRow(RectTransform parent, string label, float value, System.Func<float, string> format, System.Action<float> onChanged)
        {
            RectTransform control = UIFactory.Row(parent, label, RowWidth);
            UISlider.Create(control, value, format, onChanged, SaveSystem.Save);
            return (RectTransform)control.parent;
        }

        static RectTransform ToggleRow(RectTransform parent, string label, bool value, System.Action<bool> onChanged)
        {
            RectTransform control = UIFactory.Row(parent, label, RowWidth);
            UIToggle.Create(control, value, v => { onChanged(v); SaveSystem.Save(); });
            return (RectTransform)control.parent;
        }

        static RectTransform StepperRow(RectTransform parent, string label, System.Func<string> describe, System.Action previous, System.Action next)
        {
            RectTransform control = UIFactory.Row(parent, label, RowWidth);
            UIStepper.Create(control, describe, previous, next);
            return (RectTransform)control.parent;
        }

        static string DescribeResolution()
        {
            int i = DisplaySettings.CurrentResolutionIndex();
            if (i < 0) return Screen.width + " x " + Screen.height;
            Vector2Int r = DisplaySettings.Resolutions[i];
            return r.x + " x " + r.y;
        }

        static void StepResolution(int delta)
        {
            List<Vector2Int> list = DisplaySettings.Resolutions;
            if (list.Count == 0) return;
            int i = (DisplaySettings.CurrentResolutionIndex() + delta + list.Count) % list.Count;
            SaveSystem.Data.ResolutionWidth = list[i].x;
            SaveSystem.Data.ResolutionHeight = list[i].y;
            DisplaySettings.ApplyDisplay();
            SaveSystem.Save();
        }

        static void StepQuality(int delta)
        {
            int count = QualitySettings.names.Length;
            if (count == 0) return;
            SaveSystem.Data.QualityLevel = (SaveSystem.Data.QualityLevel + delta + count) % count;
            DisplaySettings.ApplyQuality();
            SaveSystem.Save();
        }
    }
}
