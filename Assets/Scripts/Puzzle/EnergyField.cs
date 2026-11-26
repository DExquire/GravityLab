using GravityLab.Audio;
using GravityLab.Core;
using GravityLab.Player;
using UnityEngine;

namespace GravityLab.Puzzle
{
    /// <summary>
    /// Curtain that dissolves every prop passing through it (held ones included). Players pass freely.
    /// Optionally gated by inputs; <c>invert</c> turns it into "field is OFF while powered".
    /// </summary>
    public sealed class EnergyField : MonoBehaviour
    {
        static readonly Collider[] _hits = new Collider[16];
        static readonly int PowerId = Shader.PropertyToID("_Power");

        Vector3 _half;
        Renderer _curtain;
        MaterialPropertyBlock _mpb;
        bool _invert;
        float _power = 1f;
        float _shownPower = -1f;
        bool _playerInside;

        public SignalSource[] Inputs { get; set; }

        public bool IsActive
        {
            get
            {
                if (Inputs == null || Inputs.Length == 0) return true;
                bool all = Signals.All(Inputs);
                return _invert ? !all : all;
            }
        }

        public void Init(Vector3 halfExtents, Renderer curtain, bool invert)
        {
            _half = halfExtents;
            _curtain = curtain;
            _invert = invert;
            _mpb = new MaterialPropertyBlock();
        }

        void FixedUpdate()
        {
            if (!IsActive) return;
            Vector3 center = transform.position + transform.up * _half.y;
            int count = Physics.OverlapBoxNonAlloc(center, _half, _hits, transform.rotation, Layers.PropMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Rigidbody rb = _hits[i].attachedRigidbody;
                if (rb == null) continue;
                PhysicsProp prop = rb.GetComponent<PhysicsProp>();
                if (prop != null) prop.Dissolve();
            }
        }

        void Update()
        {
            _power = Mathf.MoveTowards(_power, IsActive ? 1f : 0.06f, Time.deltaTime * 3f);
            if (!Mathf.Approximately(_power, _shownPower))
            {
                _shownPower = _power;
                _mpb.SetFloat(PowerId, _power);
                _curtain.SetPropertyBlock(_mpb);
            }

            PlayerController player = PlayerController.Active;
            bool inside = player != null && ZoneUtil.Contains(transform, Vector3.up * _half.y, _half, player.Center);
            if (inside && !_playerInside && IsActive) AudioManager.Play(SoundId.FieldZap, 0.5f);
            _playerInside = inside;
        }
    }
}
