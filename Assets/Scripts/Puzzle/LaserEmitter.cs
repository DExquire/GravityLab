using GravityLab.Core;
using GravityLab.Rendering;
using UnityEngine;

namespace GravityLab.Puzzle
{
    /// <summary>
    /// Traces a beam with repeated raycasts: static mirrors reflect it, reflector cubes re-emit it along their lens
    /// direction (only once released: a held cube just blocks the beam), receivers are powered by it, anything else
    /// stops it. Glass and players are transparent to lasers.
    /// </summary>
    public sealed class LaserEmitter : MonoBehaviour
    {
        const int MaxSegments = 12;
        const float MaxDistance = 80f;

        readonly Vector3[] _points = new Vector3[MaxSegments + 2];
        LineRenderer _core;
        LineRenderer _glow;
        Vector3 _direction;
        float _nextSpark;

        public void Init(Vector3 direction, LineRenderer core, LineRenderer glow)
        {
            _direction = direction.normalized;
            _core = core;
            _glow = glow;
        }

        void Update()
        {
            int count = Trace();
            _core.positionCount = count;
            _glow.positionCount = count;
            for (int i = 0; i < count; i++)
            {
                _core.SetPosition(i, _points[i]);
                _glow.SetPosition(i, _points[i]);
            }
            float flicker = 1f + Mathf.Sin(Time.time * 40f) * 0.08f;
            _glow.widthMultiplier = 0.16f * flicker;
        }

        int Trace()
        {
            Vector3 origin = transform.position;
            Vector3 dir = _direction;
            int count = 0;
            _points[count++] = origin;

            for (int segment = 0; segment < MaxSegments && count < _points.Length - 1; segment++)
            {
                RaycastHit hit;
                if (!Physics.Raycast(origin, dir, out hit, MaxDistance, Layers.LaserMask, QueryTriggerInteraction.Ignore))
                {
                    _points[count++] = origin + dir * MaxDistance;
                    break;
                }

                _points[count++] = hit.point;

                LaserReceiver receiver = hit.collider.GetComponentInParent<LaserReceiver>();
                if (receiver != null)
                {
                    receiver.Hit();
                    break;
                }

                LaserMirror mirror = hit.collider.GetComponent<LaserMirror>();
                if (mirror != null && Vector3.Dot(hit.normal, mirror.Normal) > 0.5f)
                {
                    dir = Vector3.Reflect(dir, hit.normal);
                    origin = hit.point + dir * 0.01f;
                    continue;
                }

                PhysicsProp prop = hit.collider.GetComponent<PhysicsProp>();
                if (prop != null && prop.Kind == PropKind.Reflector && !prop.IsHeld && !prop.IsDissolving && count < _points.Length - 1)
                {
                    prop.MarkLit();
                    Vector3 center = prop.transform.position;
                    _points[count++] = center;
                    dir = prop.transform.forward;
                    origin = center + dir * 0.55f;
                    continue;
                }

                if (Time.time >= _nextSpark)
                {
                    _nextSpark = Time.time + 0.06f;
                    FxManager.Sparks(hit.point, hit.normal, Palette.Laser, 2);
                }
                break;
            }
            return count;
        }
    }
}
