using GravityLab.Chambers;
using GravityLab.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GravityLab.UI
{
    /// <summary>5x4 grid of chamber cards with rank badges, best times and locks. Number keys 1-9 start a chamber.</summary>
    public sealed class ChamberSelectScreen : UIScreen
    {
        const int Columns = 5;
        static readonly Vector2 CardSize = new Vector2(300f, 178f);

        sealed class Card
        {
            public UIButton Button;
            public Text Number, Name, Time;
            public Image Badge, Lock;
            public Text BadgeLetter;
        }

        Card[] _cards;
        UIManager _ui;

        protected override void Build(UIManager ui)
        {
            _ui = ui;
            Image dim = UIFactory.Image(Root, "Dim", null, new Color(0.06f, 0.08f, 0.12f, 0.72f), true);
            UIFactory.Stretch(dim.rectTransform);

            Text header = UIFactory.Label(Root, "SELECT TEST CHAMBER", Fonts.Title, 64, Color.white, TextAnchor.MiddleLeft);
            UIFactory.Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(-180f, -70f), new Vector2(1240f, 90f));
            UIButton back = UIFactory.Button(Root, "BACK", new Vector2(200f, 70f), ButtonStyle.Secondary, ui.ShowMainMenu);
            UIFactory.Place((RectTransform)back.transform, new Vector2(0.5f, 1f), new Vector2(700f, -80f), new Vector2(200f, 70f));

            int count = ChamberLibrary.Count;
            _cards = new Card[count];
            int rows = Mathf.CeilToInt(count / (float)Columns);
            float gap = 24f;
            float gridW = Columns * CardSize.x + (Columns - 1) * gap;
            float gridH = rows * CardSize.y + (rows - 1) * gap;
            for (int i = 0; i < count; i++)
            {
                int index = i;
                int col = i % Columns, row = i / Columns;
                Vector2 pos = new Vector2(-gridW * 0.5f + CardSize.x * 0.5f + col * (CardSize.x + gap), gridH * 0.5f - CardSize.y * 0.5f - row * (CardSize.y + gap) - 50f);
                _cards[i] = BuildCard(index, pos);
            }

            Text hint = UIFactory.Label(Root, "Ranks: S at or under par time  ·  A  ·  B  ·  C          Press 1-9 to jump straight in", Fonts.Body, 24, new Color(1f, 1f, 1f, 0.6f));
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1600f, 40f));
        }

        Card BuildCard(int index, Vector2 pos)
        {
            Card c = new Card();
            c.Button = UIFactory.Button(Root, "", CardSize, ButtonStyle.Secondary, () => _ui.Game.StartChamber(index));
            RectTransform rt = (RectTransform)c.Button.transform;
            UIFactory.Place(rt, new Vector2(0.5f, 0.5f), pos, CardSize);

            c.Number = UIFactory.Label(rt, (index + 1).ToString("00"), Fonts.Title, 64, Palette.UiText, TextAnchor.UpperLeft);
            UIFactory.Place(c.Number.rectTransform, new Vector2(0f, 1f), new Vector2(22f, -12f), new Vector2(150f, 80f));
            c.Name = UIFactory.Label(rt, ChamberLibrary.Get(index).Name, Fonts.Bold, 24, Palette.UiText, TextAnchor.LowerLeft);
            UIFactory.Place(c.Name.rectTransform, new Vector2(0f, 0f), new Vector2(22f, 48f), new Vector2(260f, 36f));
            c.Time = UIFactory.Label(rt, "", Fonts.Body, 20, Palette.UiMuted, TextAnchor.LowerLeft);
            UIFactory.Place(c.Time.rectTransform, new Vector2(0f, 0f), new Vector2(22f, 18f), new Vector2(260f, 30f));

            c.Badge = UIFactory.Image(rt, "Badge", UISprites.Circle, Color.white);
            UIFactory.Place(c.Badge.rectTransform, new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(64f, 64f));
            c.BadgeLetter = UIFactory.Label(c.Badge.rectTransform, "", Fonts.Title, 40, Color.white);
            UIFactory.Stretch(c.BadgeLetter.rectTransform);

            c.Lock = UIFactory.Image(rt, "Lock", UISprites.Lock, Palette.UiMuted);
            UIFactory.Place(c.Lock.rectTransform, new Vector2(1f, 1f), new Vector2(-28f, -26f), new Vector2(46f, 46f));
            return c;
        }

        protected override void OnShow()
        {
            int unlocked = SaveSystem.Data.Unlocked;
            for (int i = 0; i < _cards.Length; i++)
            {
                Card c = _cards[i];
                bool open = i < unlocked;
                Rank rank = SaveSystem.BestRank(i);
                float best = SaveSystem.BestTime(i);
                c.Button.Interactable = open;
                c.Lock.gameObject.SetActive(!open);
                c.Badge.gameObject.SetActive(open && rank != Rank.None);
                c.Badge.color = Palette.RankColor(rank);
                c.BadgeLetter.text = rank.ToString();
                c.Number.color = open ? Palette.UiText : Palette.UiMuted;
                c.Time.text = !open ? "Locked" : (best > 0f ? "Best " + RankRules.FormatTime(best) : "Par " + RankRules.FormatTime(ChamberLibrary.Get(i).Par));

            }
        }

        void Update()
        {
            int n = GameInput.NumberDown();
            if (n > 0 && n <= SaveSystem.Data.Unlocked) _ui.Game.StartChamber(n - 1);
        }
    }
}
