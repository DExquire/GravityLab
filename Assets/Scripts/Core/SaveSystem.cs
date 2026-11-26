using System;
using UnityEngine;

namespace GravityLab.Core
{
    /// <summary>PlayerPrefs + JsonUtility persistence with validation, so a corrupt or old save never breaks the game.</summary>
    public static class SaveSystem
    {
        const string Key = "GravityLab.Save.v1";

        static SaveData _data;

        public static SaveData Data
        {
            get
            {
                if (_data == null) Load();
                return _data;
            }
        }

        public static void Load()
        {
            SaveData loaded = null;
            string json = PlayerPrefs.GetString(Key, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try { loaded = JsonUtility.FromJson<SaveData>(json); }
                catch (Exception e) { Debug.LogWarning("GravityLab: save data unreadable, starting fresh. " + e.Message); }
            }
            _data = loaded ?? new SaveData();
            Validate(_data);
        }

        public static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        public static float BestTime(int chamber)
        {
            return chamber >= 0 && chamber < Data.BestTimes.Length ? Data.BestTimes[chamber] : 0f;
        }

        public static Rank BestRank(int chamber)
        {
            return chamber >= 0 && chamber < Data.BestRanks.Length ? (Rank)Data.BestRanks[chamber] : Rank.None;
        }

        /// <summary>Stores a completion; returns true when it beat the previous best time.</summary>
        public static bool RecordCompletion(int chamber, float time, Rank rank)
        {
            SaveData d = Data;
            bool newBest = d.BestTimes[chamber] <= 0f || time < d.BestTimes[chamber];
            if (newBest) d.BestTimes[chamber] = time;
            if ((int)rank > d.BestRanks[chamber]) d.BestRanks[chamber] = (int)rank;
            d.Unlocked = Mathf.Clamp(Mathf.Max(d.Unlocked, chamber + 2), 1, d.BestTimes.Length);
            Save();
            return newBest;
        }

        static void Validate(SaveData d)
        {
            int count = Chambers.ChamberLibrary.Count;
            d.BestTimes = Resize(d.BestTimes, count);
            d.BestRanks = Resize(d.BestRanks, count);
            d.Unlocked = Mathf.Clamp(d.Unlocked, 1, count);
            d.MasterVolume = Mathf.Clamp01(d.MasterVolume);
            d.MusicVolume = Mathf.Clamp01(d.MusicVolume);
            d.SfxVolume = Mathf.Clamp01(d.SfxVolume);
            d.MouseSensitivity = Mathf.Clamp(d.MouseSensitivity, 0.2f, 6f);
            d.FieldOfView = Mathf.Clamp(d.FieldOfView, 60f, 100f);
        }

        static T[] Resize<T>(T[] source, int length)
        {
            if (source != null && source.Length == length) return source;
            T[] result = new T[length];
            if (source != null) Array.Copy(source, result, Mathf.Min(source.Length, length));
            return result;
        }
    }
}
