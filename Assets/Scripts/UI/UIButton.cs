using System;
using GravityLab.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GravityLab.UI
{
    /// <summary>Lightweight button: hover tint + scale, press squash, click sound. Runs on unscaled time.</summary>
    public sealed class UIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        Image _bg;
        Color _normal, _hover;
        Action _onClick;
        bool _isHovered, _isDown;
        float _scale = 1f;
        float _tint;

        public Text Label { get; private set; }
        public bool Interactable { get; set; }

        public void Init(Image bg, Text label, Color normal, Color hover, Action onClick)
        {
            _bg = bg;
            Label = label;
            _normal = normal;
            _hover = hover;
            _onClick = onClick;
            Interactable = true;
        }

        void OnDisable()
        {
            _isHovered = false;
            _isDown = false;
            _scale = 1f;
            _tint = 0f;
            transform.localScale = Vector3.one;
            if (_bg != null) _bg.color = _normal;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float target = !Interactable ? 1f : (_isDown ? 0.94f : (_isHovered ? 1.05f : 1f));
            _scale = Mathf.Lerp(_scale, target, 1f - Mathf.Exp(-dt * 18f));
            transform.localScale = new Vector3(_scale, _scale, 1f);
            _tint = Mathf.MoveTowards(_tint, _isHovered && Interactable ? 1f : 0f, dt * 8f);
            Color c = Color.Lerp(_normal, _hover, _tint);
            if (!Interactable) c.a *= 0.45f;
            _bg.color = c;
        }

        public void OnPointerEnter(PointerEventData e)
        {
            _isHovered = true;
            if (Interactable) AudioManager.Play(SoundId.Hover, 0.5f);
        }

        public void OnPointerExit(PointerEventData e) { _isHovered = false; _isDown = false; }
        public void OnPointerDown(PointerEventData e) { _isDown = true; }
        public void OnPointerUp(PointerEventData e) { _isDown = false; }

        public void OnPointerClick(PointerEventData e)
        {
            if (!Interactable) return;
            AudioManager.Play(SoundId.Click, 0.8f);
            if (_onClick != null) _onClick();
        }
    }
}
