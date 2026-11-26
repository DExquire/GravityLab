using GravityLab.Audio;
using GravityLab.Core;
using UnityEngine;

namespace GravityLab.Puzzle
{
    /// <summary>
    /// Mass-sensitive plate. Sums the mass of unique rigidbodies resting in the box above it (held objects don't count),
    /// plus the player on <see cref="PlateKind.Any"/> plates. Works in any orientation, so it can be mounted on ceilings.
    /// </summary>
    public sealed class PressurePlate : SignalSource
    {
        const float PlayerMass = 70f;
        static readonly Collider[] _hits = new Collider[24];
        static readonly int ColorId = Shader.PropertyToID("_Color");

        PlateKind _kind;
        float _requiredMass;
        Transform _pad;
        Renderer _ring;
        MaterialPropertyBlock _mpb;
        Vector3 _padUp, _padDown;
        bool _active;
        float _press;

        public override bool IsActive { get { return _active; } }
        public bool IsHeavyPlate { get { return _requiredMass > 30f; } }

        public void Init(PlateKind kind, float requiredMass, Transform pad, Renderer ring)
        {
            _kind = kind;
            _requiredMass = requiredMass;
            _pad = pad;
            _ring = ring;
            _mpb = new MaterialPropertyBlock();
            _padUp = pad.localPosition;
            _padDown = _padUp - Vector3.up * 0.06f;
            UpdateRing();
        }

        void FixedUpdate()
        {
            Vector3 up = transform.up;
            Vector3 center = transform.position + up * 0.5f;
            int count = Physics.OverlapBoxNonAlloc(center, new Vector3(0.8f, 0.4f, 0.8f), _hits, transform.rotation, Layers.PlateMask, QueryTriggerInteraction.Ignore);

            float mass = 0f;
            for (int i = 0; i < count; i++)
            {
                Collider c = _hits[i];
                if (c.gameObject.layer == Layers.Player)
                {
                    if (_kind == PlateKind.Any) mass += PlayerMass;
                    continue;
                }
                Rigidbody rb = c.attachedRigidbody;
                if (rb == null || !ZoneUtil.IsFirstOccurrence(_hits, i)) continue;
                PhysicsProp prop = rb.GetComponent<PhysicsProp>();
                if (prop != null && (prop.IsHeld || prop.IsDissolving)) continue;
                mass += rb.mass;
            }

            bool active = mass >= _requiredMass - 0.01f;
            if (active != _active)
            {
                _active = active;
                AudioManager.PlayAt(active ? SoundId.PlateOn : SoundId.PlateOff, transform.position);
                UpdateRing();
            }
        }

        void Update()
        {
            _press = Mathf.MoveTowards(_press, _active ? 1f : 0f, Time.deltaTime * 6f);
            _pad.localPosition = Vector3.Lerp(_padUp, _padDown, _press);
        }

        void UpdateRing()
        {
            _mpb.SetColor(ColorId, _active ? Palette.OrangeGlow : Palette.BlueGlow);
            _ring.SetPropertyBlock(_mpb);
        }
    }
}
