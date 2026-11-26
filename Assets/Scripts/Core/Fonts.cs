using UnityEngine;

namespace GravityLab.Core
{
    /// <summary>Bundled TTF fonts from Resources/Fonts (legacy uGUI Text, no TextMeshPro).</summary>
    public static class Fonts
    {
        static Font _title, _body, _bold;

        public static Font Title { get { return _title != null ? _title : (_title = Load("Fonts/LilitaOne-Regular")); } }
        public static Font Body { get { return _body != null ? _body : (_body = Load("Fonts/Fredoka-SemiBold")); } }
        public static Font Bold { get { return _bold != null ? _bold : (_bold = Load("Fonts/Fredoka-Bold")); } }

        static Font Load(string path)
        {
            Font f = Resources.Load<Font>(path);
            if (f == null)
            {
                Debug.LogWarning("GravityLab: font missing at Resources/" + path + ", using the built-in font.");
                f = Resources.GetBuiltinResource<Font>(BuiltinFontName());
            }
            return f;
        }

        static string BuiltinFontName()
        {
#if UNITY_2022_2_OR_NEWER
            return "LegacyRuntime.ttf";
#else
            return "Arial.ttf";
#endif
        }
    }
}
