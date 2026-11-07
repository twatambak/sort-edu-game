using OndeVai.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace OndeVai.EditorTools
{
    /// <summary>
    /// Small helpers to author uGUI hierarchies from editor code with a consistent, kid friendly style.
    /// </summary>
    public sealed class UIBuilderKit
    {
        public const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LondrinaSolid-Regular SDF.asset";

        public static readonly Color Cream = new(1f, 0.965f, 0.9f, 1f);
        public static readonly Color Sky = new(0.56f, 0.83f, 1f, 1f);
        public static readonly Color White = Color.white;
        public static readonly Color Ink = new(0.24f, 0.24f, 0.36f, 1f);
        public static readonly Color Green = new(0.3f, 0.78f, 0.45f, 1f);
        public static readonly Color Orange = new(1f, 0.62f, 0.26f, 1f);
        public static readonly Color Blue = new(0.33f, 0.63f, 1f, 1f);
        public static readonly Color Red = new(0.95f, 0.4f, 0.4f, 1f);
        public static readonly Color Purple = new(0.62f, 0.48f, 0.92f, 1f);
        public static readonly Color Yellow = new(1f, 0.85f, 0.25f, 1f);
        public static readonly Color Dim = new(0.1f, 0.1f, 0.2f, 0.6f);
        public static readonly Color PanelColor = new(1f, 1f, 1f, 0.97f);
        public static readonly Color Track = new(0.88f, 0.86f, 0.82f, 1f);

        public readonly TMP_FontAsset Font;
        public readonly Sprite Rounded;
        public readonly Sprite Circle;
        public readonly Sprite Check;

        public UIBuilderKit()
        {
            Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (Font == null)
                Debug.LogError($"Font not found at {FontPath}. TextMeshPro will use its default font.");

            Rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            Check = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.layer = LayerMask.NameToLayer("UI");
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

        /// <summary>Places a fixed size rect at an anchor point (0..1 in both axes).</summary>
        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>Stretches horizontally and anchors to the top with a fixed height.</summary>
        public static RectTransform TopBand(RectTransform rect, float top, float height, float sidePadding)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(sidePadding, -top - height);
            rect.offsetMax = new Vector2(-sidePadding, -top);
            return rect;
        }

        /// <summary>Stretches horizontally and anchors to the bottom with a fixed height.</summary>
        public static RectTransform BottomBand(RectTransform rect, float bottom, float height, float sidePadding)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(sidePadding, bottom);
            rect.offsetMax = new Vector2(-sidePadding, bottom + height);
            return rect;
        }

        public Image Image(string name, Transform parent, Color color, Sprite sprite = null, float cornerScale = 0.2f)
        {
            RectTransform rect = Rect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            if (sprite == Rounded && sprite != null)
            {
                image.type = UnityEngine.UI.Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = cornerScale;
            }

            return image;
        }

        public Image Panel(string name, Transform parent, Color color)
        {
            return Image(name, parent, color, Rounded);
        }

        public TextMeshProUGUI Text(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            RectTransform rect = Rect(name, parent);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (Font != null)
                label.font = Font;

            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        /// <summary>Rounded button with an optional icon and an optional label.</summary>
        public Button Button(string name, Transform parent, Vector2 size, Color color, Sprite icon, string label = null, float labelSize = 44f, bool circle = false)
        {
            Image background = Image(name, parent, color, circle ? Circle : Rounded);
            background.rectTransform.sizeDelta = size;
            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
            button.colors = colors;
            background.gameObject.AddComponent<UIButtonFeedback>();

            LayoutElement layout = background.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;

            bool hasLabel = !string.IsNullOrEmpty(label);
            if (icon != null)
            {
                Image iconImage = Image("Icon", background.transform, White, icon);
                iconImage.raycastTarget = false;
                iconImage.preserveAspect = true;
                float iconSize = Mathf.Min(size.x, size.y) * (hasLabel ? 0.55f : 0.6f);
                if (hasLabel)
                    Place(iconImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(size.y * 0.2f, 0f), new Vector2(iconSize, iconSize));
                else
                    Place(iconImage.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(iconSize, iconSize));
            }

            if (hasLabel)
            {
                TextMeshProUGUI text = Text("Label", background.transform, label, labelSize, White);
                Stretch(text.rectTransform);
                if (icon != null)
                    text.rectTransform.offsetMin = new Vector2(size.y * 0.8f, 0f);

                text.enableAutoSizing = true;
                text.fontSizeMin = 20f;
                text.fontSizeMax = labelSize;
            }

            return button;
        }

        /// <summary>Button that fires after being held, with a radial progress overlay.</summary>
        public HoldToConfirmButton HoldButton(string name, Transform parent, Vector2 size, Color color, Sprite icon, string label = null, bool circle = false, float holdSeconds = 1.5f)
        {
            Image background = Image(name, parent, color, circle ? Circle : Rounded);
            background.rectTransform.sizeDelta = size;
            LayoutElement layout = background.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;

            bool hasLabel = !string.IsNullOrEmpty(label);
            if (icon != null)
            {
                Image iconImage = Image("Icon", background.transform, White, icon);
                iconImage.raycastTarget = false;
                iconImage.preserveAspect = true;
                float iconSize = Mathf.Min(size.x, size.y) * 0.55f;
                Vector2 anchor = hasLabel ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f);
                Place(iconImage.rectTransform, anchor, hasLabel ? new Vector2(size.y * 0.2f, 0f) : Vector2.zero, new Vector2(iconSize, iconSize));
            }

            if (hasLabel)
            {
                TextMeshProUGUI text = Text("Label", background.transform, label, 36f, White);
                Stretch(text.rectTransform);
                if (icon != null)
                    text.rectTransform.offsetMin = new Vector2(size.y * 0.8f, 0f);

                text.enableAutoSizing = true;
                text.fontSizeMin = 18f;
                text.fontSizeMax = 36f;
            }

            Image fill = Image("HoldProgress", background.transform, new Color(1f, 1f, 1f, 0.45f), circle ? Circle : Rounded);
            Stretch(fill.rectTransform);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = circle ? UnityEngine.UI.Image.FillMethod.Radial360 : UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillAmount = 0f;
            fill.raycastTarget = false;

            HoldToConfirmButton hold = background.gameObject.AddComponent<HoldToConfirmButton>();
            SerializedObject serialized = new(hold);
            serialized.FindProperty("_holdSeconds").floatValue = holdSeconds;
            serialized.FindProperty("_progressFill").objectReferenceValue = fill;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return hold;
        }

        public Slider Slider(string name, Transform parent, Vector2 size, Color fillColor)
        {
            RectTransform root = Rect(name, parent);
            root.sizeDelta = size;
            AddLayout(root.gameObject, size);

            Image background = Image("Background", root, Track, Rounded, 0.4f);
            Stretch(background.rectTransform);
            background.rectTransform.anchorMin = new Vector2(0f, 0.3f);
            background.rectTransform.anchorMax = new Vector2(1f, 0.7f);

            RectTransform fillArea = Rect("Fill Area", root);
            Stretch(fillArea);
            fillArea.anchorMin = new Vector2(0f, 0.3f);
            fillArea.anchorMax = new Vector2(1f, 0.7f);
            fillArea.offsetMin = new Vector2(10f, 0f);
            fillArea.offsetMax = new Vector2(-10f, 0f);
            Image fill = Image("Fill", fillArea, fillColor, Rounded, 0.4f);
            fill.rectTransform.sizeDelta = new Vector2(10f, 0f);

            RectTransform handleArea = Rect("Handle Slide Area", root);
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(size.y * 0.5f, 0f);
            handleArea.offsetMax = new Vector2(-size.y * 0.5f, 0f);
            Image handle = Image("Handle", handleArea, White, Circle);
            handle.rectTransform.sizeDelta = new Vector2(size.y, 0f);

            Slider slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            return slider;
        }

        public Toggle Toggle(string name, Transform parent, float size, Color onColor)
        {
            Image background = Image(name, parent, Track, Rounded, 0.3f);
            background.rectTransform.sizeDelta = new Vector2(size, size);
            AddLayout(background.gameObject, new Vector2(size, size));

            Image checkmark = Image("Checkmark", background.transform, onColor, Check);
            Stretch(checkmark.rectTransform, size * 0.12f);
            checkmark.preserveAspect = true;

            Toggle toggle = background.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = checkmark;
            toggle.isOn = true;
            background.gameObject.AddComponent<UIButtonFeedback>();
            return toggle;
        }

        public TMP_InputField InputField(string name, Transform parent, Vector2 size, string placeholder)
        {
            Image background = Image(name, parent, White, Rounded, 0.3f);
            background.rectTransform.sizeDelta = size;
            AddLayout(background.gameObject, size);

            RectTransform textArea = Rect("Text Area", background.transform);
            Stretch(textArea, 0f);
            textArea.offsetMin = new Vector2(30f, 10f);
            textArea.offsetMax = new Vector2(-30f, -10f);
            textArea.gameObject.AddComponent<RectMask2D>();

            TextMeshProUGUI placeholderText = Text("Placeholder", textArea, placeholder, size.y * 0.45f, new Color(Ink.r, Ink.g, Ink.b, 0.4f), TextAlignmentOptions.MidlineLeft);
            Stretch(placeholderText.rectTransform);
            placeholderText.textWrappingMode = TextWrappingModes.NoWrap;

            TextMeshProUGUI text = Text("Text", textArea, string.Empty, size.y * 0.45f, Ink, TextAlignmentOptions.MidlineLeft);
            Stretch(text.rectTransform);
            text.textWrappingMode = TextWrappingModes.NoWrap;

            TMP_InputField input = background.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = background;
            input.textViewport = textArea;
            input.textComponent = text;
            input.placeholder = placeholderText;
            input.fontAsset = Font;
            input.pointSize = size.y * 0.45f;
            input.lineType = TMP_InputField.LineType.SingleLine;
            return input;
        }

        /// <summary>"&lt; value &gt;" selector wired to an <see cref="OptionSelector"/>.</summary>
        public OptionSelector Selector(string name, Transform parent, Vector2 size, Color buttonColor)
        {
            RectTransform root = Rect(name, parent);
            root.sizeDelta = size;
            AddLayout(root.gameObject, size);

            float buttonSize = size.y;
            Button previous = Button("Previous", root, new Vector2(buttonSize, buttonSize), buttonColor, IconLibrary.Get("icon_left"), circle: true);
            Place(previous.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(buttonSize, buttonSize));

            Button next = Button("Next", root, new Vector2(buttonSize, buttonSize), buttonColor, IconLibrary.Get("icon_right"), circle: true);
            Place(next.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(buttonSize, buttonSize));

            TextMeshProUGUI value = Text("Value", root, "-", size.y * 0.5f, Ink);
            Stretch(value.rectTransform);
            value.rectTransform.offsetMin = new Vector2(buttonSize + 10f, 0f);
            value.rectTransform.offsetMax = new Vector2(-buttonSize - 10f, 0f);
            value.enableAutoSizing = true;
            value.fontSizeMin = 18f;
            value.fontSizeMax = size.y * 0.5f;

            OptionSelector selector = root.gameObject.AddComponent<OptionSelector>();
            SerializedObject serialized = new(selector);
            serialized.FindProperty("_valueText").objectReferenceValue = value;
            serialized.FindProperty("_previousButton").objectReferenceValue = previous;
            serialized.FindProperty("_nextButton").objectReferenceValue = next;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return selector;
        }

        /// <summary>Vertical scroll view. Returns the content rect, which grows with its children.</summary>
        public RectTransform ScrollView(string name, Transform parent, float spacing, int padding, out ScrollRect scrollRect)
        {
            RectTransform root = Rect(name, parent);
            scrollRect = root.gameObject.AddComponent<ScrollRect>();

            RectTransform viewport = Rect("Viewport", root);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image hitArea = viewport.gameObject.AddComponent<Image>();
            hitArea.color = new Color(1f, 1f, 1f, 0f);

            RectTransform content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = UnityEngine.UI.ScrollRect.MovementType.Elastic;
            scrollRect.scrollSensitivity = 30f;
            return content;
        }

        public static HorizontalLayoutGroup Row(RectTransform rect, float spacing, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            HorizontalLayoutGroup layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static VerticalLayoutGroup Column(RectTransform rect, float spacing, int padding = 0, TextAnchor alignment = TextAnchor.UpperCenter)
        {
            VerticalLayoutGroup layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static LayoutElement AddLayout(GameObject gameObject, Vector2 preferredSize, float flexibleWidth = -1f)
        {
            if (!gameObject.TryGetComponent(out LayoutElement layout))
                layout = gameObject.AddComponent<LayoutElement>();

            layout.preferredWidth = preferredSize.x;
            layout.preferredHeight = preferredSize.y;
            layout.flexibleWidth = flexibleWidth;
            return layout;
        }

        /// <summary>Assigns a serialized field by name and logs loudly when the field does not exist.</summary>
        public static void Wire(Object target, string field, Object value)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"Field '{field}' not found on {target.GetType().Name}.", target);
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
