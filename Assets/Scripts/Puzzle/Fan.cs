using GravityLab.Audio;
using GravityLab.Core;
using GravityLab.Player;
using UnityEngine;

namespace GravityLab.Puzzle
{
    /// <summary>
    /// Updraft column above a floor fan. Lifts light props (heavy cubes are too dense) and the player,
    /// unless the player is carrying a heavy cube.
    /// </summary>
    public sealed class Fan : MonoBehaviour
    {
        public const float ColumnRadius = 1.25f;
        static readonly Collider[] _hits = new Collider[16];

        float _height;
        Transform _rotor;
        ParticleSystem _particles;
        float _power;
        bool _wasPowered;
        float _rotorAngle;

        public SignalSource[] Inputs { get; set; }

        public void Init(float height, Transform rotor, ParticleSystem particles)
        {
            _height = height;
            _rotor = rotor;
            _particles = particles;
        }

        void FixedUpdate()
        {
            bool powered = Signals.All(Inputs);
            if (powered && !_wasPowered) AudioManager.PlayAt(SoundId.FanOn, transform.position);
            _wasPowered = powered;
            _power = Mathf.MoveTowards(_power, powered ? 1f : 0f, Time.fixedDeltaTime * 1.5f);
            if (_power < 0.05f) return;

            Vector3 up = transform.up;
            Vector3 center = transform.position + up * (_height * 0.5f);
            Vector3 half = new Vector3(ColumnRadius, _height * 0.5f, ColumnRadius);
            int count = Physics.OverlapBoxNonAlloc(center, half, _hits, transform.rotation, 1 << Layers.Props, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Rigidbody rb = _hits[i].attachedRigidbody;
                if (rb == null || rb.isKinematic || !ZoneUtil.IsFirstOccurrence(_hits, i)) continue;
                PhysicsProp prop = rb.GetComponent<PhysicsProp>();
                if (prop == null || prop.IsHeavy || prop.IsFrozen || prop.IsDissolving) continue;
                Vector3 rel = rb.position - transform.position;
                float h = Vector3.Dot(rel, up);
                if ((rel - up * h).magnitude > ColumnRadius) continue;
                float vUp = Vector3.Dot(rb.velocity, up);
                Vector3 accel = up * (PhysicsProp.Gravity * 2.1f * _power) - up * (vUp * 0.8f);
                rb.AddForce(accel, ForceMode.Acceleration);
            }
        }

        void Update()
        {
            _rotorAngle += Time.deltaTime * 900f * _power;
            _rotor.localRotation = Quaternion.Euler(0f, _rotorAngle, 0f);

            ParticleSystem.EmissionModule emission = _particles.emission;
            emission.rateOverTime = 45f * _power;

            PlayerController player = PlayerController.Active;
            if (player == null || _power < 0.05f) return;
            Vector3 rel = player.transform.position - transform.position;
            float h = rel.y;
            if (h < -0.2f || h > _height) return;
            if (new Vector2(rel.x, rel.z).magnitude > ColumnRadius) return;
            player.AddLift(44f * _power, 7f);
        }
    }
}
