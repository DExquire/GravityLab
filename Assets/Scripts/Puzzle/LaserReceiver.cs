using GravityLab.Audio;
using GravityLab.Core;
using GravityLab.Rendering;
using UnityEngine;

namespace GravityLab.Puzzle
{
    /// <summary>Powered while a beam hits it (or forever after the first hit, when latching).</summary>
    public sealed class LaserReceiver : SignalSource
    {
        const float HoldTime = 0.12f;
        static readonly int ColorId = Shader.PropertyToID("_Color");

        Renderer _core;
        Transform _ring;
        MaterialPropertyBlock _mpb;
        float _lastHit = -10f;
        bool _latching;
        bool _latched;
        bool _shownActive = true;
        Vector3 _wireAnchor;

        public override bool IsActive { get { return _latched || Time.time - _lastHit < HoldTime; } }
        public override Vector3 WireAnchor { get { return _wireAnchor; } }

        public void Init(Renderer core, Transform ring, bool latching, Vector3 wireAnchor)
        {
            _core = core;
            _ring = ring;
            _latching = latching;
            _wireAnchor = wireAnchor;
            _mpb = new MaterialPropertyBlock();
            Refresh();
        }

        public void Hit()
        {
            _lastHit = Time.time;
            if (_latching) _latched = true;
        }

        void Update()
        {
            bool active = IsActive;
            _ring.localRotation = Quaternion.Euler(0f, 0f, Time.time * (active ? 240f : 20f));
            if (active != _shownActive) Refresh();
        }

        void Refresh()
        {
            bool active = IsActive;
            if (active && !_shownActive)
            {
                AudioManager.PlayAt(SoundId.LaserOn, transform.position);
                FxManager.Burst(transform.position, Palette.Orange, 14, 1.5f, 0.14f, 0.5f, 0.3f);
            }
            _shownActive = active;
            _mpb.SetColor(ColorId, active ? Palette.OrangeGlow * 1.4f : Palette.BlueGlow * 0.7f);
            _core.SetPropertyBlock(_mpb);
        }
    }
}
