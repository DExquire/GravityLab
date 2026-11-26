using UnityEngine;

namespace GravityLab.UI
{
    /// <summary>All UI sprites, drawn procedurally once at startup.</summary>
    public static class UISprites
    {
        public static Sprite Rounded { get; private set; }
        public static Sprite RoundedOutline { get; private set; }
        public static Sprite Shadow { get; private set; }
        public static Sprite Circle { get; private set; }
        public static Sprite Ring { get; private set; }
        public static Sprite ThickRing { get; private set; }
        public static Sprite Vignette { get; private set; }
        public static Sprite SideGradient { get; private set; }
        public static Sprite Lock { get; private set; }
        public static Sprite ArrowLeft { get; private set; }
        public static Sprite ArrowRight { get; private set; }
        public static Sprite Bracket { get; private set; }

        public static void Init()
        {
            if (Rounded != null) return;
            Color w = Color.white;

            Rounded = new SpritePainter(64).Fill(p => SpritePainter.RoundBox(p, Vector2.zero, Vector2.one, 0.55f), w).ToSprite(22f);
            RoundedOutline = new SpritePainter(64)
                .Fill(p => Mathf.Abs(SpritePainter.RoundBox(p, Vector2.zero, new Vector2(0.95f, 0.95f), 0.5f)) - 0.04f, w).ToSprite(22f);
            Shadow = new SpritePainter(64).Soft(p => SpritePainter.RoundBox(p, Vector2.zero, new Vector2(0.55f, 0.55f), 0.3f), new Color(0f, 0f, 0f, 0.5f), 0.7f).ToSprite(30f);
            Circle = new SpritePainter(64).Fill(p => SpritePainter.Circle(p, Vector2.zero, 0.94f), w).ToSprite();
            Ring = new SpritePainter(128).Fill(p => SpritePainter.Ring(p, 0.88f, 0.05f), w).ToSprite();
            ThickRing = new SpritePainter(128).Fill(p => SpritePainter.Ring(p, 0.82f, 0.12f), w).ToSprite();

            Vignette = new SpritePainter(256).Soft(p => 0.95f - new Vector2(p.x * 0.85f, p.y).magnitude, new Color(0.02f, 0.04f, 0.08f, 1f), 1.1f).ToSprite();
            SideGradient = Gradient();

            Lock = new SpritePainter(64)
                .Fill(p => SpritePainter.RoundBox(p, new Vector2(0f, -0.3f), new Vector2(0.62f, 0.48f), 0.14f), w)
                .Fill(p => Mathf.Max(SpritePainter.Ring(p - new Vector2(0f, 0.2f), 0.36f, 0.09f), 0.2f - p.y), w)
                .Fill(p => Mathf.Min(SpritePainter.Segment(p, new Vector2(-0.36f, 0.2f), new Vector2(-0.36f, -0.1f), 0.09f),
                    SpritePainter.Segment(p, new Vector2(0.36f, 0.2f), new Vector2(0.36f, -0.1f), 0.09f)), w)
                .ToSprite();
            ArrowLeft = new SpritePainter(64).Fill(p => Mathf.Min(SpritePainter.Segment(p, new Vector2(0.25f, 0.55f), new Vector2(-0.3f, 0f), 0.12f),
                SpritePainter.Segment(p, new Vector2(-0.3f, 0f), new Vector2(0.25f, -0.55f), 0.12f)), w).ToSprite();
            ArrowRight = new SpritePainter(64).Fill(p => Mathf.Min(SpritePainter.Segment(p, new Vector2(-0.25f, 0.55f), new Vector2(0.3f, 0f), 0.12f),
                SpritePainter.Segment(p, new Vector2(0.3f, 0f), new Vector2(-0.25f, -0.55f), 0.12f)), w).ToSprite();
            Bracket = new SpritePainter(32).Fill(p => Mathf.Min(SpritePainter.Segment(p, new Vector2(-0.8f, 0.8f), new Vector2(0.4f, 0.8f), 0.16f),
                SpritePainter.Segment(p, new Vector2(-0.8f, 0.8f), new Vector2(-0.8f, -0.4f), 0.16f)), w).ToSprite();
        }

        static Sprite Gradient()
        {
            const int width = 256, height = 4;
            Texture2D t = new Texture2D(width, height, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            Color[] px = new Color[width * height];
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);
                float a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - 0.45f) / 0.55f));
                for (int y = 0; y < height; y++) px[y * width + x] = new Color(1f, 1f, 1f, a);
            }
            t.SetPixels(px);
            t.Apply(false, true);
            return Sprite.Create(t, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
