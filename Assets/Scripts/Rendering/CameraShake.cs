using UnityEngine;

namespace GravityLab.Rendering
{
    /// <summary>Trauma-based camera shake (offset grows with trauma squared, decays over time). Sampled by the player camera.</summary>
    public static class CameraShake
    {
        const float MaxOffset = 0.07f;
        const float MaxAngle = 1.6f;
        const float Decay = 1.8f;

        static float _trauma;

        public static void Add(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + amount);
        }

        public static void Clear()
        {
            _trauma = 0f;
        }

        /// <summary>Advances decay and returns the position offset; <paramref name="roll"/> receives a small roll angle.</summary>
        public static Vector3 Sample(float dt, out float roll)
        {
            _trauma = Mathf.Max(0f, _trauma - Decay * dt);
            float k = _trauma * _trauma;
            float t = Time.unscaledTime * 22f;
            roll = (Mathf.PerlinNoise(t, 7.3f) * 2f - 1f) * MaxAngle * k;
            return new Vector3(Mathf.PerlinNoise(t, 1.1f) * 2f - 1f, Mathf.PerlinNoise(3.7f, t) * 2f - 1f, 0f) * MaxOffset * k;
        }
    }
}
