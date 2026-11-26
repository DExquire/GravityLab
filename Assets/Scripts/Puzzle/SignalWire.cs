using GravityLab.Core;
using UnityEngine;

namespace GravityLab.Puzzle
{
    /// <summary>Floor indicator strip from a signal source to the element it powers: blue when idle, orange when live.</summary>
    public sealed class SignalWire : MonoBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");

        SignalSource _source;
        Renderer[] _segments;
        MaterialPropertyBlock _mpb;
        bool _shown;

        public void Init(SignalSource source, Renderer[] segments)
        {
            _source = source;
            _segments = segments;
            _mpb = new MaterialPropertyBlock();
            Apply(false);
        }

        void Update()
        {
            bool active = _source != null && _source.IsActive;
            if (active != _shown) Apply(active);
        }

        void Apply(bool active)
        {
            _shown = active;
            _mpb.SetColor(ColorId, active ? Palette.OrangeGlow : Palette.Blue * 0.8f);
            for (int i = 0; i < _segments.Length; i++) _segments[i].SetPropertyBlock(_mpb);
        }
    }
}
