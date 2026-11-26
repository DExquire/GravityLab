using System.Collections;
using GravityLab.Audio;
using GravityLab.Core;
using GravityLab.Rendering;
using UnityEngine;

namespace GravityLab.Puzzle
{
    /// <summary>
    /// A gravity-gun target: a cube with its own gravity direction (down or up), stasis freeze, held state,
    /// dissolve/respawn at its dispenser and the per-object shader effects (glow, frozen tint, dissolve).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PhysicsProp : MonoBehaviour
    {
        public const float Gravity = 12f;

        static readonly int GlowId = Shader.PropertyToID("_Glow");
        static readonly int FrozenId = Shader.PropertyToID("_Frozen");
        static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        Renderer _renderer;
        Renderer _lens;
        Collider _collider;
        MaterialPropertyBlock _mpb;
        MaterialPropertyBlock _lensMpb;
        Vector3 _spawnPos;
        Quaternion _spawnRot;
        float _glow, _frozen01, _dissolve = -1f;
        float _shownGlow = -1f, _shownFrozen = -1f, _shownDissolve = -1f;
        float _highlightUntil, _litUntil, _lastImpact;
        bool _lensShownLit = true;
        bool _lensShownVisible = true;

        public PropKind Kind { get; private set; }
        public Rigidbody Body { get; private set; }
        /// <summary>+1 = falls down, -1 = falls up (sticks to ceilings).</summary>
        public float GravitySign { get; private set; }
        public bool IsHeld { get; private set; }
        public bool IsFrozen { get; private set; }
        public bool IsDissolving { get; private set; }
        public float FreezeTimeLeft { get; private set; }
        public bool IsHeavy { get { return Kind == PropKind.Heavy; } }
        public bool IsLit { get { return Time.time < _litUntil; } }
        public Vector3 GravityDirection { get { return Vector3.down * GravitySign; } }

        public void Init(PropKind kind, Renderer lens)
        {
            Kind = kind;
            Body = GetComponent<Rigidbody>();
            _renderer = GetComponent<Renderer>();
            _collider = GetComponent<Collider>();
            _lens = lens;
            _mpb = new MaterialPropertyBlock();
            _lensMpb = new MaterialPropertyBlock();
            _spawnPos = transform.position;
            _spawnRot = transform.rotation;
            GravitySign = 1f;
            _dissolve = 0f;
            gameObject.layer = Layers.Props;
            ApplyVisuals(true);
        }

        void FixedUpdate()
        {
            if (IsHeld || IsFrozen || IsDissolving || Body.isKinematic) return;
            Body.AddForce(GravityDirection * Gravity, ForceMode.Acceleration);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (IsFrozen)
            {
                FreezeTimeLeft -= dt;
                if (FreezeTimeLeft <= 0f) Unfreeze();
            }

            float glowTarget = IsHeld ? 0.75f + 0.25f * Mathf.Sin(Time.time * 6f) : (Time.time < _highlightUntil ? 0.35f : 0f);
            _glow = Mathf.MoveTowards(_glow, glowTarget, dt * 4f);
            float frozenTarget = IsFrozen ? (FreezeTimeLeft < 1.2f ? 0.55f + 0.45f * Mathf.Sin(Time.time * 18f) : 1f) : 0f;
            _frozen01 = Mathf.MoveTowards(_frozen01, frozenTarget, dt * 6f);
            ApplyVisuals(false);
        }

        void ApplyVisuals(bool force)
        {
            if (force || !Mathf.Approximately(_glow, _shownGlow) || !Mathf.Approximately(_frozen01, _shownFrozen) || !Mathf.Approximately(_dissolve, _shownDissolve))
            {
                _shownGlow = _glow;
                _shownFrozen = _frozen01;
                _shownDissolve = _dissolve;
                _mpb.SetFloat(GlowId, _glow);
                _mpb.SetFloat(FrozenId, _frozen01);
                _mpb.SetFloat(DissolveId, _dissolve);
                _renderer.SetPropertyBlock(_mpb);
            }

            if (_lens != null)
            {
                bool lit = IsLit && !IsDissolving;
                if (force || lit != _lensShownLit)
                {
                    _lensShownLit = lit;
                    _lensMpb.SetColor(ColorId, lit ? new Color(2.2f, 0.45f, 0.3f) : new Color(0.35f, 0.08f, 0.08f));
                    _lens.SetPropertyBlock(_lensMpb);
                }
                bool visible = _dissolve < 0.5f;
                if (force || visible != _lensShownVisible)
                {
                    _lensShownVisible = visible;
                    _lens.enabled = visible;
                }
            }
        }

        /// <summary>Called by the gun every frame it aims at this prop.</summary>
        public void Highlight() { _highlightUntil = Time.time + 0.08f; }

        /// <summary>Called by a laser every frame it passes through this (reflector) prop.</summary>
        public void MarkLit() { _litUntil = Time.time + 0.1f; }

        public void SetHeld(bool held)
        {
            IsHeld = held;
            gameObject.layer = held ? Layers.Held : Layers.Props;
            Body.drag = held ? 2f : 0.05f;
            Body.angularDrag = held ? 6f : 0.4f;
            Body.WakeUp();
        }

        public void FlipGravity()
        {
            if (IsDissolving) return;
            if (IsFrozen) Unfreeze();
            GravitySign = -GravitySign;
            Body.WakeUp();
            Body.velocity += GravityDirection * 1.5f;
            AudioManager.PlayAt(SoundId.Flip, transform.position, 0.9f, GravitySign > 0f ? 0.85f : 1.1f);
            FxManager.Ring(transform.position, Vector3.up, Palette.Violet, 0.6f, 24);
        }

        public void Freeze(float duration)
        {
            if (IsDissolving) return;
            IsFrozen = true;
            FreezeTimeLeft = duration;
            Body.velocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Body.isKinematic = true;
            AudioManager.PlayAt(SoundId.Freeze, transform.position);
            FxManager.Burst(transform.position, Palette.Cyan, 18, 1.4f, 0.16f, 0.6f, 0.45f);
        }

        public void Unfreeze()
        {
            if (!IsFrozen) return;
            IsFrozen = false;
            FreezeTimeLeft = 0f;
            Body.isKinematic = false;
            Body.WakeUp();
            AudioManager.PlayAt(SoundId.Unfreeze, transform.position, 0.7f);
        }

        /// <summary>Energy-field dissolve: fades out, then re-materialises at the dispenser.</summary>
        public void Dissolve()
        {
            if (IsDissolving) return;
            AudioManager.PlayAt(SoundId.Dissolve, transform.position);
            FxManager.Burst(transform.position, Palette.Blue, 26, 1.2f, 0.14f, 0.9f, 0.45f);
            StartCoroutine(DissolveRoutine(0.75f));
        }

        /// <summary>Lost in goo or out of bounds: a quick fade and respawn.</summary>
        public void Respawn()
        {
            if (IsDissolving) return;
            StartCoroutine(DissolveRoutine(0.25f));
        }

        IEnumerator DissolveRoutine(float outTime)
        {
            IsDissolving = true;
            if (IsFrozen) Unfreeze();
            Body.velocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Body.isKinematic = true;
            _collider.enabled = false;

            for (float t = 0f; t < outTime; t += Time.deltaTime)
            {
                _dissolve = t / outTime;
                yield return null;
            }

            GravitySign = 1f;
            transform.SetPositionAndRotation(_spawnPos, _spawnRot);
            Body.position = _spawnPos;
            Body.rotation = _spawnRot;
            AudioManager.PlayAt(SoundId.Respawn, _spawnPos, 0.8f);
            FxManager.Burst(_spawnPos, Palette.Blue, 14, 0.8f, 0.14f, 0.6f, 0.4f);

            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                _dissolve = 1f - t / 0.5f;
                yield return null;
            }
            _dissolve = 0f;
            _collider.enabled = true;
            Body.isKinematic = false;
            Body.WakeUp();
            IsDissolving = false;
        }

        void OnCollisionEnter(Collision collision)
        {
            float speed = collision.relativeVelocity.magnitude;
            if (speed < 2.2f || Time.time - _lastImpact < 0.15f) return;
            _lastImpact = Time.time;
            AudioManager.PlayAt(SoundId.Impact, transform.position, Mathf.Clamp01(speed / 9f), IsHeavy ? 0.7f : 1f);
            if (speed > 7f && IsHeavy) CameraShake.Add(0.15f);
        }
    }
}
