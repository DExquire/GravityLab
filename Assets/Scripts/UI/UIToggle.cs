using System;
using GravityLab.Audio;
using GravityLab.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GravityLab.UI
{
    /// <summary>Pill switch with an animated knob.</summary>
    public sealed class UIToggle : MonoBehaviour, IPointerClickHandler
    {
        Image _bg;
        RectTransform _knob;
        Action<bool> _onChanged;
        float _anim;

        public bool Value { get; private set; }

        public static UIToggle Create(RectTransform area, bool value, Action<bool> onChanged)
        {
            Image bg = UIFactory.Image(area, "Toggle", UISprites.Rounded, Palette.UiTrack, true);
            UIFactory.Place(bg.rectTransform, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(96f, 48f));
            Image knob = UIFactory.Image(bg.rectTransform, "Knob", UISprites.Circle, Color.white);
            knob.rectTransform.sizeDelta = new Vector2(38f, 38f);

            UIToggle t = bg.gameObject.AddComponent<UIToggle>();
            t._bg = bg;
            t._knob = knob.rectTransform;
            t._onChanged = onChanged;
            t.Value = value;
            t._anim = value ? 1f : 0f;
            t.Apply();
            return t;
        }

        public void OnPointerClick(PointerEventData e)
        {
            Value = !Value;
            AudioManager.Play(SoundId.Click, 0.8f);
            if (_onChanged != null) _onChanged(Value);
        }

        void Update()
        {
            float target = Value ? 1f : 0f;
            if (Mathf.Approximately(_anim, target)) return;
            _anim = Mathf.MoveTowards(_anim, target, Time.unscaledDeltaTime * 7f);
            Apply();
        }

        void Apply()
        {
            float e = _anim * _anim * (3f - 2f * _anim);
            _knob.anchorMin = _knob.anchorMax = new Vector2(0.5f, 0.5f);
            _knob.anchoredPosition = new Vector2(Mathf.Lerp(-24f, 24f, e), 0f);
            _bg.color = Color.Lerp(Palette.UiTrack, Palette.Blue, e);
        }
    }
}
