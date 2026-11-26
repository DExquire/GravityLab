using System;
using System.Collections;
using UnityEngine;

namespace GravityLab.Core
{
    /// <summary>Tiny coroutine-based tween runner (unscaled time by default, so UI animates while paused).</summary>
    public sealed class Tweener : MonoBehaviour
    {
        static Tweener _instance;

        public static void Init(GameObject host)
        {
            if (_instance == null) _instance = host.AddComponent<Tweener>();
        }

        /// <summary>Calls <paramref name="step"/> with 0..1 over <paramref name="duration"/> seconds.</summary>
        public static Coroutine Run(float duration, Action<float> step, Action done = null, float delay = 0f, bool unscaled = true)
        {
            if (_instance == null) return null;
            return _instance.StartCoroutine(_instance.Routine(duration, step, done, delay, unscaled));
        }

        public static void Cancel(Coroutine routine)
        {
            if (_instance != null && routine != null) _instance.StopCoroutine(routine);
        }

        IEnumerator Routine(float duration, Action<float> step, Action done, float delay, bool unscaled)
        {
            while (delay > 0f)
            {
                delay -= unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
            float t = 0f;
            while (t < duration)
            {
                step(t / duration);
                t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
            step(1f);
            if (done != null) done();
        }

        public static float EaseOutCubic(float t) { t = 1f - t; return 1f - t * t * t; }
        public static float EaseInOut(float t) { return t * t * (3f - 2f * t); }

        /// <summary>Overshooting ease used for pop-in animations.</summary>
        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }
    }
}
