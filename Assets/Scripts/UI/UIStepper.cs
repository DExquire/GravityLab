using System;
using GravityLab.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GravityLab.UI
{
    /// <summary>"&lt; value &gt;" selector for discrete options (resolution, quality preset).</summary>
    public sealed class UIStepper : MonoBehaviour
    {
        Text _value;
        Func<string> _describe;

        public static UIStepper Create(RectTransform area, Func<string> describe, Action onPrevious, Action onNext)
        {
            float w = area.sizeDelta.x;
            UIStepper s = area.gameObject.AddComponent<UIStepper>();
            s._describe = describe;

            UIButton left = UIFactory.Button(area, "", new Vector2(52f, 48f), ButtonStyle.Secondary, () => { onPrevious(); s.Refresh(); });
            UIFactory.Place((RectTransform)left.transform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(52f, 48f));
            Image la = UIFactory.Image(left.transform, "Icon", UISprites.ArrowLeft, Palette.UiText);
            la.rectTransform.sizeDelta = new Vector2(24f, 24f);

            UIButton right = UIFactory.Button(area, "", new Vector2(52f, 48f), ButtonStyle.Secondary, () => { onNext(); s.Refresh(); });
            UIFactory.Place((RectTransform)right.transform, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(52f, 48f));
            Image ra = UIFactory.Image(right.transform, "Icon", UISprites.ArrowRight, Palette.UiText);
            ra.rectTransform.sizeDelta = new Vector2(24f, 24f);

            s._value = UIFactory.Label(area, "", Fonts.Bold, 26, Palette.UiText);
            UIFactory.Place(s._value.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w - 120f, 48f));
            s.Refresh();
            return s;
        }

        public void Refresh()
        {
            _value.text = _describe();
        }
    }
}
