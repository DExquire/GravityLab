using System;

namespace GravityLab.Core
{
    /// <summary>Everything persisted between sessions. Plain fields only so JsonUtility can serialise it.</summary>
    [Serializable]
    public class SaveData
    {
        public int Unlocked = 1;
        public float[] BestTimes = new float[0];
        public int[] BestRanks = new int[0];

        public float MasterVolume = 0.8f;
        public float MusicVolume = 0.55f;
        public float SfxVolume = 0.9f;

        public bool Fullscreen = true;
        public int ResolutionWidth;
        public int ResolutionHeight;
        public int QualityLevel = -1;
        public bool VSync = true;

        public float MouseSensitivity = 2f;
        public bool InvertY;
        public float FieldOfView = 80f;
        public bool HeadBob = true;
    }
}
