using System;
using UnityEngine;

namespace GravityLab.UI
{
    /// <summary>
    /// Tiny signed-distance rasteriser for UI sprites. Coordinates are normalised to [-1, 1] (y up);
    /// shapes are alpha-composited with anti-aliased coverage.
    /// </summary>
    public sealed class SpritePainter
    {
        readonly int _size;
        readonly Color[] _px;
        readonly float _aa;

        public SpritePainter(int size)
        {
            _size = size;
            _px = new Color[size * size];
            for (int i = 0; i < _px.Length; i++) _px[i] = new Color(1f, 1f, 1f, 0f);
            _aa = 2.4f / size;
        }

        public SpritePainter Fill(Func<Vector2, float> sdf, Color color)
        {
            return Paint(sdf, color, false, 0f);
        }

        /// <summary>Blurred fill for drop shadows and glows.</summary>
        public SpritePainter Soft(Func<Vector2, float> sdf, Color color, float softness)
        {
            return Paint(sdf, color, true, softness);
        }

        SpritePainter Paint(Func<Vector2, float> sdf, Color color, bool soft, float softness)
        {
            for (int y = 0; y < _size; y++)
            {
                for (int x = 0; x < _size; x++)
                {
                    Vector2 p = new Vector2((x + 0.5f) / _size * 2f - 1f, (y + 0.5f) / _size * 2f - 1f);
                    float d = sdf(p);
                    float cov = soft
                        ? 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(d / softness + 0.5f))
                        : Mathf.Clamp01(0.5f - d / _aa);
                    if (cov > 0f) Blend(y * _size + x, color, cov);
                }
            }
            return this;
        }

        void Blend(int index, Color c, float coverage)
        {
            Color dst = _px[index];
            float a = c.a * coverage;
            float outA = a + dst.a * (1f - a);
            if (outA <= 0.0001f) return;
            Color o = (c * a + dst * dst.a * (1f - a)) / outA;
            o.a = outA;
            _px[index] = o;
        }

        public Sprite ToSprite(float border = 0f)
        {
            Texture2D t = new Texture2D(_size, _size, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            t.SetPixels(_px);
            t.Apply(false, true);
            return Sprite.Create(t, new Rect(0f, 0f, _size, _size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
        }

        // ---- SDF primitives --------------------------------------------------------------

        public static float Circle(Vector2 p, Vector2 c, float r) { return (p - c).magnitude - r; }

        public static float RoundBox(Vector2 p, Vector2 c, Vector2 half, float r)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half + new Vector2(r, r);
            Vector2 qm = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
            return qm.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
        }

        public static float Segment(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - r;
        }

        public static float Ring(Vector2 p, float radius, float halfWidth) { return Mathf.Abs(p.magnitude - radius) - halfWidth; }
    }
}
