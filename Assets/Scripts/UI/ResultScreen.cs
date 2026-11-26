using GravityLab.Audio;
using GravityLab.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GravityLab.UI
{
    /// <summary>Data shown on the completion card.</summary>
    public struct ResultInfo
    {
        public int ChamberIndex;
        public string Name;
        public float Time;
        public float PreviousBest;
        public float Par;
        public Rank Rank;
        public bool NewBest;
        public bool IsLast;
    }

    /// <summary>Completion card (time, best, stamped rank badge with a particle burst) or the "subject lost" card.</summary>
    public sealed class ResultScreen : UIScreen
    {
        const int BurstCount = 18;

        UIManager _ui;
        Text _header, _name, _time, _detail, _badgeLetter, _newBest, _keys;
        Image _badge;
        RectTransform _badgeRoot;
        RectTransform[] _burst;
        Image[] _burstImages;
        UIButton _next;
        Coroutine _stamp;
        bool _failed;

        protected override void Build(UIManager ui)
        {
            _ui = ui;
            Image dim = UIFactory.Image(Root, "Dim", null, new Color(0.04f, 0.06f, 0.1f, 0.5f), true);
            UIFactory.Stretch(dim.rectTransform);
            RectTransform card = UIFactory.Card(Root, "Card", new Vector2(760f, 720f), Palette.UiCard);

            _header = UIFactory.Label(card, "", Fonts.Bold, 26, Palette.Blue);
            UIFactory.Place(_header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(700f, 36f));
            _name = UIFactory.Label(card, "", Fonts.Title, 60, Palette.UiText);
            UIFactory.Place(_name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(700f, 80f));

            _badgeRoot = UIFactory.Place(UIFactory.Rect(card, "BadgeRoot"), new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(180f, 180f));
            _burst = new RectTransform[BurstCount];
            _burstImages = new Image[BurstCount];
            for (int i = 0; i < BurstCount; i++)
            {
                Image dot = UIFactory.Image(_badgeRoot, "Burst" + i, UISprites.Circle, Color.white);
                dot.rectTransform.sizeDelta = new Vector2(18f, 18f);
                _burst[i] = dot.rectTransform;
                _burstImages[i] = dot;
            }
            Image ring = UIFactory.Image(_badgeRoot, "Ring", UISprites.ThickRing, new Color(1f, 1f, 1f, 0.9f));
            UIFactory.Stretch(ring.rectTransform);
            _badge = UIFactory.Image(_badgeRoot, "Badge", UISprites.Circle, Color.white);
            UIFactory.Stretch(_badge.rectTransform);
            _badge.rectTransform.offsetMin = new Vector2(14f, 14f);
            _badge.rectTransform.offsetMax = new Vector2(-14f, -14f);
            _badgeLetter = UIFactory.Label(_badge.rectTransform, "", Fonts.Title, 110, Color.white);
            UIFactory.Stretch(_badgeLetter.rectTransform);
            Shadow s = _badgeLetter.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.25f);
            s.effectDistance = new Vector2(0f, -4f);

            _time = UIFactory.Label(card, "", Fonts.Title, 64, Palette.UiText);
            UIFactory.Place(_time.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -385f), new Vector2(700f, 80f));
            _newBest = UIFactory.Label(card, "NEW RECORD", Fonts.Bold, 24, Palette.Orange);
            UIFactory.Place(_newBest.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -360f), new Vector2(700f, 30f));
            _detail = UIFactory.Label(card, "", Fonts.Body, 24, Palette.UiMuted);
            UIFactory.Place(_detail.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -462f), new Vector2(700f, 34f));

            _next = UIFactory.Button(card, "NEXT CHAMBER", new Vector2(400f, 84f), ButtonStyle.Primary, OnNext);
            UIFactory.Place((RectTransform)_next.transform, new Vector2(0.5f, 0f), new Vector2(0f, 112f), new Vector2(400f, 84f));
            UIButton retry = UIFactory.Button(card, "RETRY", new Vector2(230f, 66f), ButtonStyle.Secondary, () => _ui.Game.Restart());
            UIFactory.Place((RectTransform)retry.transform, new Vector2(0.5f, 0f), new Vector2(-125f, 28f), new Vector2(230f, 66f));
            UIButton menu = UIFactory.Button(card, "MENU", new Vector2(230f, 66f), ButtonStyle.Ghost, () => _ui.Game.ReturnToMenu());
            UIFactory.Place((RectTransform)menu.transform, new Vector2(0.5f, 0f), new Vector2(125f, 28f), new Vector2(230f, 66f));

            _keys = UIFactory.Label(Root, "", Fonts.Body, 22, new Color(1f, 1f, 1f, 0.7f));
            UIFactory.Place(_keys.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(900f, 30f));
        }

        public void SetResult(ResultInfo info)
        {
            _failed = false;
            _header.text = "TEST CHAMBER " + (info.ChamberIndex + 1).ToString("00") + " COMPLETE";
            _header.color = Palette.Blue;
            _name.text = info.Name;
            _time.text = RankRules.FormatTime(info.Time);
            _newBest.gameObject.SetActive(info.NewBest && info.PreviousBest > 0f);
            string best = info.NewBest ? RankRules.FormatTime(info.Time) : RankRules.FormatTime(info.PreviousBest);
            _detail.text = "Best " + best + "      Par " + RankRules.FormatTime(info.Par);
            _badgeRoot.gameObject.SetActive(true);
            _badge.color = Palette.RankColor(info.Rank);
            _badgeLetter.text = info.Rank.ToString();
            _next.gameObject.SetActive(true);
            _next.Label.text = info.IsLast ? "FINISH" : "NEXT CHAMBER";
            _keys.text = "[ENTER] " + (info.IsLast ? "Finish" : "Next") + "     [R] Retry";
            _time.rectTransform.anchoredPosition = new Vector2(0f, -385f);
            PlayStamp(info.Rank);
        }

        public void SetFailed(string chamberName)
        {
            _failed = true;
            _header.text = "TEST SUBJECT LOST";
            _header.color = new Color(0.9f, 0.35f, 0.3f);
            _name.text = chamberName;
            _time.text = "Goo is not a solid.";
            _time.rectTransform.anchoredPosition = new Vector2(0f, -250f);
            _newBest.gameObject.SetActive(false);
            _detail.text = "The facility has prepared a fresh test subject.";
            _badgeRoot.gameObject.SetActive(false);
            _next.gameObject.SetActive(false);
            _keys.text = "[ENTER] or [R] Retry";
        }

        void OnNext()
        {
            if (!_failed) _ui.Game.NextChamber();
        }

        void PlayStamp(Rank rank)
        {
            Tweener.Cancel(_stamp);
            _badgeRoot.localScale = Vector3.zero;
            Color color = Palette.RankColor(rank);
            for (int i = 0; i < BurstCount; i++) _burstImages[i].color = new Color(color.r, color.g, color.b, 0f);
            bool played = false;
            _stamp = Tweener.Run(0.9f, t =>
            {
                float scale = t < 0.35f ? Mathf.LerpUnclamped(2.2f, 1f, Tweener.EaseOutBack(t / 0.35f)) : 1f;
                _badgeRoot.localScale = new Vector3(scale, scale, 1f);
                if (t >= 0.2f && !played)
                {
                    played = true;
                    AudioManager.Play(SoundId.RankStamp);
                }
                float bt = Mathf.Clamp01((t - 0.2f) / 0.8f);
                for (int i = 0; i < BurstCount; i++)
                {
                    float ang = i / (float)BurstCount * Mathf.PI * 2f;
                    float r = 60f + Tweener.EaseOutCubic(bt) * (110f + (i % 3) * 25f);
                    _burst[i].anchoredPosition = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
                    _burstImages[i].color = new Color(color.r, color.g, color.b, bt > 0f ? 1f - bt : 0f);
                }
            }, null, 0.25f);
        }
    }
}
