using GravityLab.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GravityLab.UI
{
    public sealed class PauseScreen : UIScreen
    {
        protected override void Build(UIManager ui)
        {
            Image dim = UIFactory.Image(Root, "Dim", null, new Color(0.04f, 0.06f, 0.1f, 0.55f), true);
            UIFactory.Stretch(dim.rectTransform);

            RectTransform card = UIFactory.Card(Root, "Card", new Vector2(560f, 640f), Palette.UiCard);
            Text title = UIFactory.Label(card, "PAUSED", Fonts.Title, 72, Palette.UiText);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(500f, 90f));

            RectTransform[] buttons =
            {
                (RectTransform)UIFactory.Button(card, "RESUME", new Vector2(420f, 80f), ButtonStyle.Primary, ui.Game.Resume).transform,
                (RectTransform)UIFactory.Button(card, "RESTART CHAMBER", new Vector2(420f, 72f), ButtonStyle.Secondary, ui.Game.Restart).transform,
                (RectTransform)UIFactory.Button(card, "SETTINGS", new Vector2(420f, 72f), ButtonStyle.Secondary, ui.ShowSettings).transform,
                (RectTransform)UIFactory.Button(card, "MAIN MENU", new Vector2(420f, 72f), ButtonStyle.Ghost, ui.Game.ReturnToMenu).transform
            };
            UIFactory.Stack(buttons, 160f, 20f);

            Text hint = UIFactory.Label(card, "Esc to resume", Fonts.Body, 22, Palette.UiMuted);
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(400f, 30f));
        }
    }
}
