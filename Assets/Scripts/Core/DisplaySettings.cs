using System.Collections.Generic;
using GravityLab.Audio;
using UnityEngine;

namespace GravityLab.Core
{
    /// <summary>Applies the saved PC settings: window mode, resolution, quality preset, vsync and volumes.</summary>
    public static class DisplaySettings
    {
        static readonly List<Vector2Int> _resolutions = new List<Vector2Int>();

        /// <summary>Distinct supported resolutions, smallest first.</summary>
        public static List<Vector2Int> Resolutions
        {
            get
            {
                if (_resolutions.Count == 0) CollectResolutions();
                return _resolutions;
            }
        }

        public static void ApplyAll()
        {
            ApplyQuality();
            ApplyDisplay();
            ApplyAudio();
        }

        public static void ApplyAudio()
        {
            AudioListener.volume = SaveSystem.Data.MasterVolume;
            AudioManager.RefreshVolumes();
        }

        public static void ApplyDisplay()
        {
            SaveData d = SaveSystem.Data;
            FullScreenMode mode = d.Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            int index = CurrentResolutionIndex();
            if (index >= 0)
            {
                Vector2Int r = Resolutions[index];
                Screen.SetResolution(r.x, r.y, mode);
            }
            else
            {
                Screen.fullScreenMode = mode;
            }
        }

        public static void ApplyQuality()
        {
            SaveData d = SaveSystem.Data;
            int count = QualitySettings.names.Length;
            if (d.QualityLevel < 0 || d.QualityLevel >= count) d.QualityLevel = QualitySettings.GetQualityLevel();
            QualitySettings.SetQualityLevel(d.QualityLevel, true);
            QualitySettings.vSyncCount = d.VSync ? 1 : 0;
            Application.targetFrameRate = d.VSync ? -1 : 240;
            ApplyShadowTuning(d.QualityLevel, count);
        }

        /// <summary>Index into <see cref="Resolutions"/> matching the saved size, or the closest to the current screen.</summary>
        public static int CurrentResolutionIndex()
        {
            List<Vector2Int> list = Resolutions;
            if (list.Count == 0) return -1;
            SaveData d = SaveSystem.Data;
            int targetW = d.ResolutionWidth > 0 ? d.ResolutionWidth : Screen.width;
            int targetH = d.ResolutionHeight > 0 ? d.ResolutionHeight : Screen.height;
            int best = list.Count - 1;
            int bestScore = int.MaxValue;
            for (int i = 0; i < list.Count; i++)
            {
                int score = Mathf.Abs(list[i].x - targetW) + Mathf.Abs(list[i].y - targetH);
                if (score < bestScore) { bestScore = score; best = i; }
            }
            return best;
        }

        public static string QualityName(int level)
        {
            string[] names = QualitySettings.names;
            return level >= 0 && level < names.Length ? names[level] : "Default";
        }

        static void ApplyShadowTuning(int level, int count)
        {
            // Keep the lab's soft-shadow look on medium and up; the lowest presets keep their own (cheaper) settings.
            if (count > 2 && level < 2) return;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            QualitySettings.shadowDistance = 45f;
            QualitySettings.shadowCascades = level >= count - 2 ? 4 : 2;
            QualitySettings.shadowResolution = level >= count - 2 ? ShadowResolution.VeryHigh : ShadowResolution.High;
        }

        static void CollectResolutions()
        {
            _resolutions.Clear();
            Resolution[] all = Screen.resolutions;
            for (int i = 0; i < all.Length; i++)
            {
                Vector2Int r = new Vector2Int(all[i].width, all[i].height);
                if (r.x < 800 || r.y < 450 || _resolutions.Contains(r)) continue;
                _resolutions.Add(r);
            }
            if (_resolutions.Count == 0) _resolutions.Add(new Vector2Int(Mathf.Max(800, Screen.width), Mathf.Max(450, Screen.height)));
            _resolutions.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        }
    }
}
