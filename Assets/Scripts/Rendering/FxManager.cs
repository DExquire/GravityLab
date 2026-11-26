using UnityEngine;

namespace GravityLab.Rendering
{
    /// <summary>Two shared world-space particle systems (floaty glow + falling sparks) driven by Emit() calls.</summary>
    public sealed class FxManager : MonoBehaviour
    {
        static FxManager _instance;

        ParticleSystem _glow;
        ParticleSystem _sparks;

        public static void Init(Transform parent)
        {
            if (_instance != null) return;
            GameObject go = new GameObject("FX");
            go.transform.SetParent(parent, false);
            _instance = go.AddComponent<FxManager>();
            _instance._glow = CreateSystem(go.transform, "Glow", 0f, 600);
            _instance._sparks = CreateSystem(go.transform, "Sparks", 1.2f, 400);
        }

        static ParticleSystem CreateSystem(Transform parent, string name, float gravity, int max)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max;
            main.gravityModifier = gravity;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = false;

            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);

            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = MatLib.Particle;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            ps.Play();
            return ps;
        }

        /// <summary>Soft glowing puff of particles flying outward from <paramref name="position"/>.</summary>
        public static void Burst(Vector3 position, Color color, int count, float speed = 2f, float size = 0.18f, float life = 0.7f, float radius = 0.2f)
        {
            if (_instance == null) return;
            Emit(_instance._glow, position, Vector3.zero, color, count, speed, size, life, radius);
        }

        /// <summary>Falling sparks biased along <paramref name="normal"/>.</summary>
        public static void Sparks(Vector3 position, Vector3 normal, Color color, int count)
        {
            if (_instance == null) return;
            Emit(_instance._sparks, position, normal * 1.5f, color, count, 2.2f, 0.06f, 0.45f, 0.02f);
        }

        /// <summary>Flat expanding ring of particles in the plane perpendicular to <paramref name="axis"/>.</summary>
        public static void Ring(Vector3 center, Vector3 axis, Color color, float radius, int count)
        {
            if (_instance == null) return;
            Vector3 a = Vector3.Cross(axis, Vector3.right);
            if (a.sqrMagnitude < 0.01f) a = Vector3.Cross(axis, Vector3.forward);
            a.Normalize();
            Vector3 b = Vector3.Cross(axis, a);
            ParticleSystem.EmitParams p = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                float ang = i / (float)count * Mathf.PI * 2f;
                Vector3 dir = a * Mathf.Cos(ang) + b * Mathf.Sin(ang);
                p.position = center + dir * radius;
                p.velocity = dir * 2.5f + axis * 0.4f;
                p.startSize = 0.14f;
                p.startLifetime = 0.55f;
                p.startColor = color;
                _instance._glow.Emit(p, 1);
            }
        }

        static void Emit(ParticleSystem ps, Vector3 position, Vector3 bias, Color color, int count, float speed, float size, float life, float radius)
        {
            ParticleSystem.EmitParams p = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                Vector3 dir = Random.onUnitSphere;
                p.position = position + dir * radius;
                p.velocity = dir * speed * Random.Range(0.4f, 1f) + bias;
                p.startSize = size * Random.Range(0.6f, 1.2f);
                p.startLifetime = life * Random.Range(0.6f, 1.1f);
                p.startColor = color;
                ps.Emit(p, 1);
            }
        }
    }
}
