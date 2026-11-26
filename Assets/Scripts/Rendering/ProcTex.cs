using GravityLab.Puzzle;
using UnityEngine;

namespace GravityLab.Rendering
{
    /// <summary>Procedural textures: cube faces (alpha = emission mask), conveyor chevrons and toxic goo.</summary>
    public static class ProcTex
    {
        const int CubeSize = 256;

        public static Texture2D CubeFace(PropKind kind)
        {
            Color baseCol, frameCol, padCol, symbolCol;
            switch (kind)
            {
                case PropKind.Heavy:
                    baseCol = new Color(0.36f, 0.38f, 0.42f); frameCol = new Color(0.22f, 0.23f, 0.26f);
                    padCol = new Color(1f, 0.55f, 0.15f); symbolCol = new Color(1f, 0.6f, 0.2f); break;
                case PropKind.Companion:
                    baseCol = new Color(0.88f, 0.89f, 0.91f); frameCol = new Color(0.6f, 0.62f, 0.66f);
                    padCol = new Color(0.98f, 0.55f, 0.72f); symbolCol = new Color(1f, 0.45f, 0.65f); break;
                case PropKind.Reflector:
                    baseCol = new Color(0.74f, 0.76f, 0.8f); frameCol = new Color(0.4f, 0.42f, 0.47f);
                    padCol = new Color(0.3f, 0.32f, 0.36f); symbolCol = new Color(0.32f, 0.34f, 0.38f); break;
                default:
                    baseCol = new Color(0.84f, 0.86f, 0.89f); frameCol = new Color(0.56f, 0.59f, 0.64f);
                    padCol = new Color(0.68f, 0.71f, 0.76f); symbolCol = new Color(0.25f, 0.7f, 1f); break;
            }

            Color[] px = new Color[CubeSize * CubeSize];
            for (int y = 0; y < CubeSize; y++)
            {
                for (int x = 0; x < CubeSize; x++)
                {
                    Vector2 p = new Vector2((x + 0.5f) / CubeSize * 2f - 1f, (y + 0.5f) / CubeSize * 2f - 1f);
                    float ax = Mathf.Abs(p.x), ay = Mathf.Abs(p.y);
                    float edge = 1f - Mathf.Max(ax, ay);
                    Color c = baseCol;
                    float emission = 0f;

                    if (edge < 0.11f) c = Color.Lerp(frameCol, frameCol * 1.25f, Mathf.Clamp01(edge / 0.11f));
                    else if (edge < 0.13f) c = baseCol * 1.08f;

                    // Corner bumper pads.
                    if (ax > 0.6f && ay > 0.6f && edge >= 0.11f) c = padCol;

                    if (kind == PropKind.Heavy && edge < 0.11f && Mathf.Repeat((p.x + p.y) * 4f, 1f) < 0.5f) c = padCol * 0.9f;

                    float r = p.magnitude;
                    if (kind == PropKind.Companion)
                    {
                        if (Heart(p * 2.3f + new Vector2(0f, 0.25f))) { c = symbolCol; emission = 1f; }
                        else if (r > 0.42f && r < 0.47f) c = frameCol;
                    }
                    else if (kind == PropKind.Reflector)
                    {
                        if (r < 0.45f) c = Mathf.Repeat(r * 9f, 1f) < 0.3f ? symbolCol : baseCol * 0.92f;
                    }
                    else
                    {
                        if (r < 0.3f) c = frameCol * 1.1f;
                        if (r > 0.3f && r < 0.4f) { c = symbolCol; emission = 1f; }
                        if (r >= 0.4f && r < 0.46f) c = frameCol;
                    }

                    c.a = emission;
                    px[y * CubeSize + x] = c;
                }
            }
            return Finish(px, CubeSize, TextureWrapMode.Clamp);
        }

        static bool Heart(Vector2 q)
        {
            float a = q.x * q.x + q.y * q.y - 1f;
            return a * a * a - q.x * q.x * q.y * q.y * q.y < 0f;
        }

        /// <summary>Belt chevrons pointing toward +V; tiles once per metre.</summary>
        public static Texture2D Chevrons()
        {
            const int s = 128;
            Color[] px = new Color[s * s];
            Color belt = new Color(0.17f, 0.19f, 0.22f);
            Color stripe = new Color(0.38f, 0.42f, 0.48f);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                    float chevron = Mathf.Repeat(v * 2f - Mathf.Abs(u - 0.5f) * 1.2f, 1f);
                    bool onStripe = chevron > 0.55f && chevron < 0.8f && u > 0.12f && u < 0.88f;
                    float ridge = Mathf.Repeat(v * 16f, 1f) < 0.12f ? 0.9f : 1f;
                    Color c = (onStripe ? stripe : belt) * ridge;
                    c.a = onStripe ? 0.5f : 0f;
                    px[y * s + x] = c;
                }
            }
            return Finish(px, s, TextureWrapMode.Repeat);
        }

        /// <summary>Tileable toxic goo: layered value noise with bright bubbles.</summary>
        public static Texture2D Goo()
        {
            const int s = 128;
            Color[] px = new Color[s * s];
            Color deep = new Color(0.05f, 0.32f, 0.22f);
            Color bright = new Color(0.35f, 0.95f, 0.6f);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float n = TileNoise(x, y, s, 8) * 0.6f + TileNoise(x, y, s, 16) * 0.3f + TileNoise(x, y, s, 32) * 0.1f;
                    float bubble = Mathf.Clamp01((n - 0.68f) * 8f);
                    Color c = Color.Lerp(deep, bright, n * 0.7f + bubble * 0.5f);
                    c.a = 1f;
                    px[y * s + x] = c;
                }
            }
            return Finish(px, s, TextureWrapMode.Repeat);
        }

        static float TileNoise(int x, int y, int size, int cells)
        {
            float fx = x / (float)size * cells, fy = y / (float)size * cells;
            int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
            float tx = fx - x0, ty = fy - y0;
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);
            float a = Hash(x0 % cells, y0 % cells), b = Hash((x0 + 1) % cells, y0 % cells);
            float c = Hash(x0 % cells, (y0 + 1) % cells), d = Hash((x0 + 1) % cells, (y0 + 1) % cells);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        static float Hash(int x, int y)
        {
            int h = x * 374761393 + y * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
        }

        static Texture2D Finish(Color[] px, int size, TextureWrapMode wrap)
        {
            Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, true);
            t.wrapMode = wrap;
            t.filterMode = FilterMode.Trilinear;
            t.anisoLevel = 4;
            t.SetPixels(px);
            t.Apply(true, true);
            return t;
        }
    }
}
