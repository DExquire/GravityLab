using System;

namespace GravityLab.Chambers
{
    /// <summary>A handcrafted chamber: display name, par time for the S rank, narrator intro and its build script.</summary>
    public sealed class ChamberDef
    {
        public readonly string Name;
        public readonly float Par;
        public readonly string[] Intro;
        public readonly Action<ChamberBuilder> Build;

        public ChamberDef(string name, float par, string[] intro, Action<ChamberBuilder> build)
        {
            Name = name;
            Par = par;
            Intro = intro;
            Build = build;
        }
    }

    public enum Side { North, South, East, West }

    /// <summary>A hole in one of the four shell walls (doors, exits). <see cref="Along"/> is the world X (N/S) or Z (E/W) of its centre.</summary>
    public struct Opening
    {
        public Side Side;
        public float Along;
        public float Bottom;
        public float Width;
        public float Height;

        public static Opening Door(Side side, float along, float bottom = 0f)
        {
            Opening o;
            o.Side = side;
            o.Along = along;
            o.Bottom = bottom;
            o.Width = 2.4f;
            o.Height = 3f;
            return o;
        }
    }

    /// <summary>A rectangular hole in a wall segment, in that wall's own coordinates.</summary>
    public struct Gap
    {
        public float Center;
        public float Width;
        public float Bottom;
        public float Height;

        public Gap(float center, float width, float bottom, float height)
        {
            Center = center;
            Width = width;
            Bottom = bottom;
            Height = height;
        }
    }
}
