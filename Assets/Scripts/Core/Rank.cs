namespace GravityLab.Core
{
    /// <summary>Per-chamber completion rank, ordered so a higher value is better. Stored as int in the save.</summary>
    public enum Rank
    {
        None = 0,
        C = 1,
        B = 2,
        A = 3,
        S = 4
    }

    public static class RankRules
    {
        /// <summary>S at or under par, then A/B/C at 1.5x and 2.2x par.</summary>
        public static Rank ForTime(float seconds, float par)
        {
            if (seconds <= par) return Rank.S;
            if (seconds <= par * 1.5f) return Rank.A;
            if (seconds <= par * 2.2f) return Rank.B;
            return Rank.C;
        }

        public static string FormatTime(float seconds)
        {
            if (seconds <= 0f) return "--:--.-";
            int minutes = (int)(seconds / 60f);
            float rest = seconds - minutes * 60f;
            int whole = (int)rest;
            int tenth = (int)((rest - whole) * 10f);
            return minutes.ToString("00") + ":" + whole.ToString("00") + "." + tenth;
        }
    }
}
