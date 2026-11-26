using UnityEngine;

namespace GravityLab.Rendering
{
    /// <summary>Slow cinematic drift through the current chamber behind the main menu.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class MenuCamera : MonoBehaviour
    {
        Vector3 _center;
        Vector3 _extents;
        float _time;

        public void Frame(Vector3 center, Vector3 size)
        {
            _center = center;
            _extents = new Vector3(Mathf.Max(1f, size.x * 0.32f), 0f, Mathf.Max(1f, size.z * 0.32f));
            _time = Random.Range(0f, 10f);
            Step(0f);
        }

        void Update()
        {
            Step(Time.unscaledDeltaTime);
        }

        void Step(float dt)
        {
            _time += dt * 0.08f;
            Vector3 pos = _center + new Vector3(Mathf.Sin(_time) * _extents.x, 2.6f + Mathf.Sin(_time * 0.7f) * 0.5f, Mathf.Cos(_time * 0.8f) * _extents.z);
            Vector3 look = _center + new Vector3(Mathf.Sin(_time + 2.2f) * _extents.x * 0.5f, 1.2f, Mathf.Cos(_time * 0.8f + 2.2f) * _extents.z * 0.5f);
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos));
        }
    }
}
