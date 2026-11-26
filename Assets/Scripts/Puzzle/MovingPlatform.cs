using GravityLab.Core;
using UnityEngine;

namespace GravityLab.Puzzle
{
    /// <summary>Kinematic platform shuttling between two points with eased motion and a pause at each end.</summary>
    public sealed class MovingPlatform : MonoBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");

        Rigidbody _body;
        Vector3 _a, _b;
        float _speed, _pause, _length;
        float _t;
        int _direction = 1;
        float _pauseTimer;
        Renderer[] _strips;
        MaterialPropertyBlock _mpb;
        bool _shownPowered = true;

        public SignalSource[] Inputs { get; set; }

        public void Init(Vector3 a, Vector3 b, float speed, float pause, Renderer[] strips)
        {
            _body = GetComponent<Rigidbody>();
            _a = a;
            _b = b;
            _speed = speed;
            _pause = pause;
            _length = Mathf.Max(0.01f, Vector3.Distance(a, b));
            _strips = strips;
            _mpb = new MaterialPropertyBlock();
            RefreshStrips(false);
        }

        void FixedUpdate()
        {
            bool powered = Signals.All(Inputs);
            if (powered != _shownPowered) RefreshStrips(powered);
            if (!powered) return;

            float dt = Time.fixedDeltaTime;
            if (_pauseTimer > 0f)
            {
                _pauseTimer -= dt;
                return;
            }

            _t += _direction * _speed * dt / _length;
            if (_t >= 1f) { _t = 1f; _direction = -1; _pauseTimer = _pause; }
            else if (_t <= 0f) { _t = 0f; _direction = 1; _pauseTimer = _pause; }
            float eased = _t * _t * (3f - 2f * _t);
            _body.MovePosition(Vector3.Lerp(_a, _b, eased));
        }

        void RefreshStrips(bool powered)
        {
            _shownPowered = powered;
            _mpb.SetColor(ColorId, powered ? Palette.OrangeGlow : Palette.BlueGlow);
            for (int i = 0; i < _strips.Length; i++) _strips[i].SetPropertyBlock(_mpb);
        }
    }
}
