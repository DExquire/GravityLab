using UnityEngine;

namespace GravityLab.Core
{
    /// <summary>One palette for the whole facility: white panels, slate text, blue = idle, orange = active.</summary>
    public static class Palette
    {
        public static readonly Color Panel = new Color(0.93f, 0.95f, 0.97f);
        public static readonly Color Floor = new Color(0.8f, 0.83f, 0.87f);
        public static readonly Color Ceiling = new Color(0.86f, 0.88f, 0.91f);
        public static readonly Color Metal = new Color(0.27f, 0.3f, 0.35f);
        public static readonly Color MetalLight = new Color(0.55f, 0.59f, 0.65f);
        public static readonly Color Blue = new Color(0.2f, 0.65f, 1f);
        public static readonly Color BlueGlow = new Color(0.35f, 0.8f, 1.6f);
        public static readonly Color Orange = new Color(1f, 0.55f, 0.12f);
        public static readonly Color OrangeGlow = new Color(1.6f, 0.72f, 0.18f);
        public static readonly Color Cyan = new Color(0.4f, 0.95f, 1f);
        public static readonly Color Violet = new Color(0.7f, 0.45f, 1f);
        public static readonly Color Laser = new Color(1f, 0.18f, 0.12f);
        public static readonly Color Goo = new Color(0.25f, 0.85f, 0.55f);
        public static readonly Color LightPanel = new Color(1.25f, 1.3f, 1.35f);

        public static readonly Color UiText = new Color(0.11f, 0.15f, 0.21f);
        public static readonly Color UiMuted = new Color(0.42f, 0.48f, 0.56f);
        public static readonly Color UiCard = new Color(0.97f, 0.98f, 0.99f, 0.97f);
        public static readonly Color UiCardDark = new Color(0.1f, 0.13f, 0.18f, 0.88f);
        public static readonly Color UiTrack = new Color(0.82f, 0.86f, 0.9f);

        public static Color RankColor(Rank rank)
        {
            switch (rank)
            {
                case Rank.S: return new Color(1f, 0.72f, 0.15f);
                case Rank.A: return new Color(0.25f, 0.75f, 1f);
                case Rank.B: return new Color(0.45f, 0.85f, 0.5f);
                case Rank.C: return new Color(0.65f, 0.68f, 0.74f);
                default: return new Color(0.8f, 0.83f, 0.87f);
            }
        }
    }
}
