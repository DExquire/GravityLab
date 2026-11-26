using GravityLab.Audio;
using GravityLab.Core;
using UnityEngine;

namespace GravityLab.Puzzle
{
    /// <summary>Two kinematic panels that slide apart while all inputs are powered. Header lamps show the state.</summary>
    public sealed class Door : MonoBehaviour
    {
        const float Speed = 2.2f;
        static readonly int ColorId = Shader.PropertyToID("_Color");

        Rigidbody _left, _right;
        Vector3 _leftClosed, _rightClosed, _axis;
        float _slide;
        float _open01;
        bool _targetOpen;
        Renderer[] _lamps;
        MaterialPropertyBlock _mpb;

        public SignalSource[] Inputs { get; set; }
        /// <summary>Keeps the door shut regardless of inputs (entry door before the test starts, exit after use).</summary>
        public bool ForceClosed { get; set; }
        public bool IsOpen { get { return _open01 > 0.85f; } }

        public void Init(Rigidbody left, Rigidbody right, float slide, Renderer[] lamps)
        {
            _left = left;
            _right = right;
            _leftClosed = left.position;
            _rightClosed = right.position;
            _axis = transform.right;
            _slide = slide;
            _lamps = lamps;
            _mpb = new MaterialPropertyBlock();
            UpdateLamps();
        }

        void FixedUpdate()
        {
            bool want = !ForceClosed && Signals.All(Inputs);
            if (want != _targetOpen)
            {
                _targetOpen = want;
                AudioManager.PlayAt(want ? SoundId.DoorOpen : SoundId.DoorClose, transform.position + Vector3.up * 1.5f, 0.8f);
                UpdateLamps();
            }

            float prev = _open01;
            _open01 = Mathf.MoveTowards(_open01, want ? 1f : 0f, Time.fixedDeltaTime * Speed);
            if (Mathf.Approximately(prev, _open01)) return;
            float eased = _open01 * _open01 * (3f - 2f * _open01);
            _left.MovePosition(_leftClosed - _axis * (_slide * eased));
            _right.MovePosition(_rightClosed + _axis * (_slide * eased));
        }

        void UpdateLamps()
        {
            _mpb.SetColor(ColorId, _targetOpen ? Palette.OrangeGlow : Palette.BlueGlow);
            for (int i = 0; i < _lamps.Length; i++) _lamps[i].SetPropertyBlock(_mpb);
        }
    }
}
