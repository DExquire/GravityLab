using System;
using GravityLab.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GravityLab.UI
{
    /// <summary>Horizontal 0..1 slider with a rounded track, fill and knob; reports values while dragging.</summary>
    public sealed class UISlider : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        RectTransform _track;
        RectTransform _fill;
        RectTransform _knob;
        Text _valueText;
        Func<float, string> _format;
        Action<float> _onChanged;
        Action _onCommit;

        public float Value { get; private set; }

        public static UISlider Create(RectTransform area, float value, Func<float, string> format, Action<float> onChanged, Action onCommit)
        {
            RectTransform track = UIFactory.Place(UIFactory.Rect(area, "Track"), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(area.sizeDelta.x - 110f, 14f));
            Image trackImg = track.gameObject.AddComponent<Image>();
            trackImg.sprite = UISprites.Rounded;
            trackImg.type = Image.Type.Sliced;
            trackImg.color = Palette.UiTrack;

            // Generous invisible hit area around the thin track.
            Image hit = UIFactory.Image(track, "Hit", null, new Color(1f, 1f, 1f, 0f), true);
            UIFactory.Stretch(hit.rectTransform);
            hit.rectTransform.offsetMin = new Vector2(-14f, -22f);
            hit.rectTransform.offsetMax = new Vector2(14f, 22f);

            Image fill = UIFactory.Image(track, "Fill", UISprites.Rounded, Palette.Blue);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;

            Image knob = UIFactory.Image(track, "Knob", UISprites.Circle, Color.white);
            knob.rectTransform.sizeDelta = new Vector2(34f, 34f);
            Image knobRing = UIFactory.Image(knob.rectTransform, "Ring", UISprites.ThickRing, Palette.Blue);
            UIFactory.Stretch(knobRing.rectTransform);

            Text valueText = UIFactory.Label(area, "", Fonts.Bold, 26, Palette.UiMuted, TextAnchor.MiddleRight);
            UIFactory.Place(valueText.rectTransform, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(96f, 40f));

            UISlider slider = track.gameObject.AddComponent<UISlider>();
            slider._track = track;
            slider._fill = fill.rectTransform;
            slider._knob = knob.rectTransform;
            slider._valueText = valueText;
            slider._format = format;
            slider._onChanged = onChanged;
            slider._onCommit = onCommit;
            slider.SetValue(value, false);
            return slider;
        }

        public void SetValue(float value, bool notify)
        {
            Value = Mathf.Clamp01(value);
            _fill.anchorMax = new Vector2(Value, 1f);
            _knob.anchorMin = _knob.anchorMax = new Vector2(Value, 0.5f);
            _knob.anchoredPosition = Vector2.zero;
            _valueText.text = _format(Value);
            if (notify && _onChanged != null) _onChanged(Value);
        }

        public void OnPointerDown(PointerEventData e) { Track(e); }
        public void OnDrag(PointerEventData e) { Track(e); }

        public void OnPointerUp(PointerEventData e)
        {
            if (_onCommit != null) _onCommit();
        }

        void Track(PointerEventData e)
        {
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_track, e.position, e.pressEventCamera, out local)) return;
            Rect r = _track.rect;
            SetValue((local.x - r.xMin) / r.width, true);
        }
    }
}
