using GravityLab.Core;
using GravityLab.Player;
using UnityEngine;

namespace GravityLab.Puzzle
{
    /// <summary>
    /// Carries props (and a player standing on a floor belt) along its forward axis, gently centring loads.
    /// The transform sits on the belt surface with +Y pointing away from it, so belts can hang from ceilings
    /// and carry objects with reversed gravity.
    /// </summary>
    public sealed class ConveyorBelt : MonoBehaviour
    {
        const float ZoneHeight = 0.55f;
        static readonly Collider[] _hits = new Collider[16];
        static readonly int OffsetId = Shader.PropertyToID("_UVOffset");
        static readonly int TilingId = Shader.PropertyToID("_MainTex_ST");

        float _speed, _length, _width;
        Renderer _belt;
        MaterialPropertyBlock _mpb;
        float _offset;
        float _power;

        public SignalSource[] Inputs { get; set; }

        public void Init(float speed, float length, float width, Renderer belt)
        {
            _speed = speed;
            _length = length;
            _width = width;
            _belt = belt;
            _mpb = new MaterialPropertyBlock();
            _mpb.SetVector(TilingId, new Vector4(1f, length, 0f, 0f));
        }

        Vector3 HalfExtents { get { return new Vector3(_width * 0.5f, ZoneHeight, _length * 0.5f); } }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            _power = Mathf.MoveTowards(_power, Signals.All(Inputs) ? 1f : 0f, dt * 2f);
            if (_power < 0.01f) return;

            Vector3 up = transform.up;
            Vector3 center = transform.position + up * ZoneHeight;
            int count = Physics.OverlapBoxNonAlloc(center, HalfExtents, _hits, transform.rotation, 1 << Layers.Props, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Rigidbody rb = _hits[i].attachedRigidbody;
                if (rb == null || rb.isKinematic || !ZoneUtil.IsFirstOccurrence(_hits, i)) continue;
                PhysicsProp prop = rb.GetComponent<PhysicsProp>();
                if (prop == null || prop.IsFrozen || prop.IsDissolving) continue;
                if (Vector3.Dot(prop.GravityDirection, -up) < 0.5f) continue; // only loads pressed onto the belt

                Vector3 v = rb.velocity;
                Vector3 inPlane = v - Vector3.Project(v, up);
                float lateral = Vector3.Dot(rb.position - transform.position, transform.right);
                Vector3 desired = transform.forward * (_speed * _power) - transform.right * (lateral * 1.5f);
                rb.velocity = v + (desired - inPlane) * Mathf.Clamp01(10f * dt);
            }
        }

        void Update()
        {
            _offset = Mathf.Repeat(_offset + _speed * _power * Time.deltaTime, 1f);
            _mpb.SetVector(OffsetId, new Vector4(0f, -_offset, 0f, 0f));
            _belt.SetPropertyBlock(_mpb);

            PlayerController player = PlayerController.Active;
            if (player == null || _power < 0.01f || transform.up.y < 0.9f) return;
            if (ZoneUtil.Contains(transform, Vector3.up * ZoneHeight, HalfExtents, player.transform.position + Vector3.up * 0.1f))
            {
                player.AddZoneVelocity(transform.forward * (_speed * _power));
            }
        }
    }
}
