using System.Collections.Generic;
using GravityLab.Audio;
using GravityLab.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GravityLab.UI
{
    /// <summary>Narrator subtitles: queued lines typed out letter by letter, held, then faded. Pauses with the game.</summary>
    public sealed class SubtitleView : MonoBehaviour
    {
        const float CharsPerSecond = 48f;

        readonly Queue<string> _queue = new Queue<string>();
        CanvasGroup _group;
        Text _text;
        string _line;
        float _shown;
        float _hold;
        int _visibleChars = -1;

        public static SubtitleView Create(Transform parent)
        {
            RectTransform root = UIFactory.Place(UIFactory.Rect(parent, "Subtitles"), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(1240f, 130f));
            SubtitleView view = root.gameObject.AddComponent<SubtitleView>();
            view._group = root.gameObject.AddComponent<CanvasGroup>();
            view._group.alpha = 0f;
            view._group.blocksRaycasts = false;

            Image bg = UIFactory.Image(root, "Bg", UISprites.Rounded, new Color(0.06f, 0.08f, 0.12f, 0.78f));
            UIFactory.Stretch(bg.rectTransform);
            Text speaker = UIFactory.Label(root, "OVERSEER", Fonts.Title, 24, Palette.Blue, TextAnchor.UpperLeft);
            UIFactory.Place(speaker.rectTransform, new Vector2(0f, 1f), new Vector2(32f, -14f), new Vector2(300f, 32f));
            view._text = UIFactory.Label(root, "", Fonts.Body, 30, Color.white, TextAnchor.UpperLeft);
            UIFactory.Place(view._text.rectTransform, new Vector2(0f, 1f), new Vector2(32f, -48f), new Vector2(1176f, 80f));
            return view;
        }

        public void Enqueue(string line)
        {
            _queue.Enqueue(line);
        }

        public void Clear()
        {
            _queue.Clear();
            _line = null;
            _group.alpha = 0f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (_line == null)
            {
                _group.alpha = Mathf.MoveTowards(_group.alpha, 0f, dt * 3f);
                if (_queue.Count == 0 || dt <= 0f) return;
                _line = _queue.Dequeue();
                _shown = 0f;
                _visibleChars = -1;
                _hold = 2.2f + _line.Length * 0.035f;
                AudioManager.Play(SoundId.Blip, 0.6f);
            }

            _group.alpha = Mathf.MoveTowards(_group.alpha, 1f, dt * 5f);
            _shown += dt * CharsPerSecond;
            int chars = Mathf.Min(_line.Length, Mathf.FloorToInt(_shown));
            if (chars != _visibleChars)
            {
                _visibleChars = chars;
                _text.text = _line.Substring(0, chars);
            }
            if (chars < _line.Length) return;

            _hold -= dt;
            if (_hold <= 0f) _line = null;
        }
    }
}
