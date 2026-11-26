using System;
using GravityLab.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GravityLab.UI
{
    public enum ButtonStyle { Primary, Secondary, Ghost }

    /// <summary>Helpers that build uGUI hierarchies in code with the game's visual style.</summary>
    public static class UIFactory
    {
        public static RectTransform Rect(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Image(Transform parent, string name, Sprite sprite, Color color, bool raycast = false)
        {
            Image img = Rect(parent, name).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null && sprite.border.sqrMagnitude > 0f) img.type = UnityEngine.UI.Image.Type.Sliced;
            return img;
        }

        public static Text Label(Transform parent, string text, Font font, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter)
        {
            Text t = Rect(parent, "Text").gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.text = text;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Rounded card with a soft drop shadow.</summary>
        public static RectTransform Card(Transform parent, string name, Vector2 size, Color color)
        {
            RectTransform root = Place(Rect(parent, name), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            Image shadow = Image(root, "Shadow", UISprites.Shadow, new Color(0f, 0f, 0f, 0.55f));
            Stretch(shadow.rectTransform);
            shadow.rectTransform.offsetMin = new Vector2(-40f, -56f);
            shadow.rectTransform.offsetMax = new Vector2(40f, 24f);
            Image bg = Image(root, "Bg", UISprites.Rounded, color, true);
            Stretch(bg.rectTransform);
            return root;
        }

        public static UIButton Button(Transform parent, string label, Vector2 size, ButtonStyle style, Action onClick)
        {
            RectTransform rt = Rect(parent, "Button");
            rt.sizeDelta = size;
            Color fill, text, hover;
            switch (style)
            {
                case ButtonStyle.Primary: fill = Palette.Blue; hover = new Color(0.35f, 0.75f, 1f); text = Color.white; break;
                case ButtonStyle.Secondary: fill = Color.white; hover = new Color(0.9f, 0.95f, 1f); text = Palette.UiText; break;
                default: fill = new Color(1f, 1f, 1f, 0f); hover = new Color(1f, 1f, 1f, 0.12f); text = Palette.UiText; break;
            }
            Image bg = Image(rt, "Bg", UISprites.Rounded, fill, true);
            Stretch(bg.rectTransform);
            if (style == ButtonStyle.Secondary)
            {
                Image outline = Image(rt, "Outline", UISprites.RoundedOutline, new Color(0.75f, 0.8f, 0.87f));
                Stretch(outline.rectTransform);
            }
            Text t = Label(rt, label, Fonts.Bold, Mathf.RoundToInt(size.y * 0.4f), text);
            Stretch(t.rectTransform);
            UIButton button = rt.gameObject.AddComponent<UIButton>();
            button.Init(bg, t, fill, hover, onClick);
            return button;
        }

        /// <summary>Settings row: label on the left, control area on the right. Returns the control area.</summary>
        public static RectTransform Row(Transform parent, string label, float width, float height = 64f)
        {
            RectTransform row = Rect(parent, label + "Row");
            row.sizeDelta = new Vector2(width, height);
            Text t = Label(row, label, Fonts.Body, 28, Palette.UiText, TextAnchor.MiddleLeft);
            Place(t.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(width * 0.42f, height));
            RectTransform control = Place(Rect(row, "Control"), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(width * 0.56f, height));
            return control;
        }

        /// <summary>Vertical stack layout done once at build time (no LayoutGroup needed).</summary>
        public static void Stack(RectTransform[] items, float startY, float spacing)
        {
            float y = startY;
            for (int i = 0; i < items.Length; i++)
            {
                RectTransform rt = items[i];
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -y);
                y += rt.sizeDelta.y + spacing;
            }
        }
    }
}
