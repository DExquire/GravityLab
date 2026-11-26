using GravityLab.Core;
using GravityLab.Puzzle;
using UnityEngine;
using UnityEngine.Rendering;

namespace GravityLab.Rendering
{
    /// <summary>Shared materials, created once from the shaders in Resources/Shaders.</summary>
    public static class MatLib
    {
        public static Material Panel { get; private set; }
        public static Material Floor { get; private set; }
        public static Material Ceiling { get; private set; }
        public static Material Metal { get; private set; }
        public static Material MetalLight { get; private set; }
        public static Material Plastic { get; private set; }
        public static Material Mirror { get; private set; }
        public static Material Glass { get; private set; }
        public static Material Field { get; private set; }
        public static Material Goo { get; private set; }
        public static Material Conveyor { get; private set; }
        public static Material Particle { get; private set; }
        public static Material LightPanel { get; private set; }
        public static Material AccentBlue { get; private set; }
        public static Material AccentOrange { get; private set; }
        public static Material Signal { get; private set; }
        public static Material LaserCore { get; private set; }
        public static Material LaserGlow { get; private set; }
        public static Material Beam { get; private set; }
        public static Material Lens { get; private set; }
        public static Material LaserHousing { get; private set; }

        static Material[] _cubes;
        static Shader _lab, _unlit;

        public static void Init()
        {
            if (Panel != null) return;
            _lab = Find("GravityLab/Lab", "Standard");
            _unlit = Find("GravityLab/Unlit", "Unlit/Color");

            Panel = Lab(Palette.Panel, true, 0.3f, 0.3f);
            Floor = Lab(Palette.Floor, true, 0.45f, 0.4f);
            Ceiling = Lab(Palette.Ceiling, true, 0.1f, 0.1f);
            Metal = Lab(Palette.Metal, false, 0.5f, 0.6f);
            MetalLight = Lab(Palette.MetalLight, false, 0.45f, 0.5f);
            Plastic = Lab(new Color(0.95f, 0.96f, 0.98f), false, 0.4f, 0.5f);
            Mirror = Lab(new Color(0.55f, 0.7f, 0.85f), false, 1f, 1f);
            Mirror.SetColor("_FresnelColor", new Color(0.85f, 0.95f, 1f));
            Mirror.SetColor("_EmissionColor", new Color(0.08f, 0.14f, 0.2f));

            Conveyor = Lab(Color.white, false, 0.2f, 0.3f);
            Conveyor.SetTexture("_MainTex", ProcTex.Chevrons());
            Conveyor.SetColor("_TexEmission", Palette.Blue * 0.35f);

            Glass = new Material(Find("GravityLab/Glass", "Transparent/Diffuse"));
            Field = new Material(Find("GravityLab/Field", "Particles/Standard Unlit"));
            Field.SetColor("_Color", new Color(0.35f, 0.75f, 1f));
            Particle = new Material(Find("GravityLab/Particle", "Particles/Standard Unlit"));

            Goo = Unlit(Color.white, BlendKind.Opaque);
            Goo.SetTexture("_MainTex", ProcTex.Goo());
            LightPanel = Unlit(Palette.LightPanel, BlendKind.Opaque);
            AccentBlue = Unlit(Palette.BlueGlow, BlendKind.Opaque);
            AccentOrange = Unlit(Palette.OrangeGlow, BlendKind.Opaque);
            Signal = Unlit(Palette.Blue, BlendKind.Opaque);
            LaserCore = Unlit(new Color(1f, 0.85f, 0.8f), BlendKind.Additive);
            LaserGlow = Unlit(Palette.Laser, BlendKind.Additive);
            Beam = Unlit(Palette.Orange, BlendKind.Additive);
            Lens = Unlit(new Color(0.35f, 0.08f, 0.08f), BlendKind.Opaque);
            LaserHousing = Unlit(Palette.Laser * 1.6f, BlendKind.Opaque);

            _cubes = new Material[System.Enum.GetValues(typeof(PropKind)).Length];
        }

        public enum BlendKind { Opaque, Transparent, Additive }

        public static Material Unlit(Color color, BlendKind blend)
        {
            Material m = new Material(_unlit);
            m.SetColor("_Color", color);
            switch (blend)
            {
                case BlendKind.Transparent:
                    m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                    m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    m.SetFloat("_ZWrite", 0f);
                    m.renderQueue = 3000;
                    break;
                case BlendKind.Additive:
                    m.SetFloat("_SrcBlend", (float)BlendMode.One);
                    m.SetFloat("_DstBlend", (float)BlendMode.One);
                    m.SetFloat("_ZWrite", 0f);
                    m.SetFloat("_Additive", 1f);
                    m.renderQueue = 3100;
                    break;
            }
            return m;
        }

        /// <summary>Per prop kind cube material with its face texture and emission colour.</summary>
        public static Material Cube(PropKind kind)
        {
            int i = (int)kind;
            if (_cubes[i] != null) return _cubes[i];
            Material m = Lab(Color.white, false, 0.35f, 0.45f);
            m.SetTexture("_MainTex", ProcTex.CubeFace(kind));
            Color emission = kind == PropKind.Heavy ? Palette.OrangeGlow
                : kind == PropKind.Companion ? new Color(1.4f, 0.5f, 0.8f)
                : Palette.BlueGlow;
            m.SetColor("_TexEmission", emission);
            _cubes[i] = m;
            return m;
        }

        static Material Lab(Color color, bool grid, float fresnel, float gloss)
        {
            Material m = new Material(_lab);
            m.SetColor("_Color", color);
            m.SetFloat("_UseGrid", grid ? 1f : 0f);
            m.SetFloat("_FresnelStrength", fresnel);
            m.SetFloat("_Gloss", gloss);
            return m;
        }

        static Shader Find(string name, string fallback)
        {
            Shader s = Shader.Find(name);
            if (s == null)
            {
                Debug.LogWarning("GravityLab: shader '" + name + "' not found, using " + fallback);
                s = Shader.Find(fallback);
            }
            return s;
        }
    }
}
