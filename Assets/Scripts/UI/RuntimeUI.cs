using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OndeVai.UI
{
    /// <summary>
    /// Small helpers to create uGUI elements from runtime code, for widgets that depend on data
    /// (stat tiles, star rows) and therefore cannot be authored once in a prefab.
    /// </summary>
    public static class RuntimeUI
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.layer = parent.gameObject.layer;
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static RectTransform Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
            return rect;
        }

        /// <summary>Non interactive image. Sprites with a border are drawn sliced.</summary>
        public static Image Image(string name, Transform parent, Sprite sprite, Color color)
        {
            Image image = Rect(name, parent).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            if (sprite != null && sprite.border != Vector4.zero)
                image.type = UnityEngine.UI.Image.Type.Sliced;

            return image;
        }

        /// <summary>Text that copies the font of <paramref name="style"/>, so it matches the authored screens.</summary>
        public static TextMeshProUGUI Text(string name, Transform parent, TMP_Text style, string text, float size, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            TextMeshProUGUI label = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (style != null)
                label.font = style.font;

            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>Sets the preferred size used by layout groups. Negative values leave an axis unset.</summary>
        public static LayoutElement Layout(GameObject gameObject, float preferredWidth, float preferredHeight, float flexibleWidth = -1f)
        {
            if (!gameObject.TryGetComponent(out LayoutElement layout))
                layout = gameObject.AddComponent<LayoutElement>();

            layout.preferredWidth = preferredWidth;
            layout.preferredHeight = preferredHeight;
            layout.flexibleWidth = flexibleWidth;
            return layout;
        }

        /// <summary>Horizontal layout that sizes its children. With <paramref name="expandChildren"/> they share the width equally.</summary>
        public static HorizontalLayoutGroup Row(RectTransform rect, float spacing, bool expandChildren = false)
        {
            HorizontalLayoutGroup layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = expandChildren;
            layout.childForceExpandHeight = expandChildren;
            return layout;
        }

        /// <summary>Vertical layout whose children fill the width and keep their preferred height.</summary>
        public static VerticalLayoutGroup Column(RectTransform rect, float spacing)
        {
            VerticalLayoutGroup layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }
    }
}
