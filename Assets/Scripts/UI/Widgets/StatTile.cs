using DG.Tweening;
using OndeVai.Effects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OndeVai.UI
{
    /// <summary>
    /// Colored card with one big number and a caption, e.g. "85%" over "Acertos". Built at runtime
    /// because the progress report decides which numbers to show.
    /// </summary>
    public sealed class StatTile
    {
        private const float Height = 170f;
        private const float ValueSize = 68f;
        private const float CaptionSize = 30f;
        private const float PopDuration = 0.3f;

        private readonly RectTransform _root;
        private readonly TextMeshProUGUI _value;

        /// <param name="style">Any text of the screen. Its font is reused so the tile matches the authored UI.</param>
        public StatTile(Transform parent, TMP_Text style, string caption, Color color)
        {
            Image background = RuntimeUI.Image(caption, parent, ProceduralSprites.RoundedPanel, color);
            _root = background.rectTransform;
            RuntimeUI.Layout(background.gameObject, 0f, Height, 1f);

            _value = RuntimeUI.Text("Value", _root, style, "-", ValueSize, Color.white);
            _value.enableAutoSizing = true;
            _value.fontSizeMin = 30f;
            _value.fontSizeMax = ValueSize;
            _value.textWrappingMode = TextWrappingModes.NoWrap;
            Anchor(_value.rectTransform, 0.38f, 1f);

            TextMeshProUGUI captionText = RuntimeUI.Text("Caption", _root, style, caption, CaptionSize, new Color(1f, 1f, 1f, 0.92f));
            captionText.enableAutoSizing = true;
            captionText.fontSizeMin = 18f;
            captionText.fontSizeMax = CaptionSize;
            Anchor(captionText.rectTransform, 0.06f, 0.4f);
        }

        /// <summary>Sets the number and pops the tile in after <paramref name="delay"/> seconds.</summary>
        public void Show(string value, float delay)
        {
            _value.text = value;
            _root.DOKill();
            _root.localScale = Vector3.one * 0.8f;
            _root.DOScale(1f, PopDuration)
                .SetDelay(delay)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .SetLink(_root.gameObject);
        }

        private static void Anchor(RectTransform rect, float bottom, float top)
        {
            rect.anchorMin = new Vector2(0f, bottom);
            rect.anchorMax = new Vector2(1f, top);
            rect.offsetMin = new Vector2(14f, 0f);
            rect.offsetMax = new Vector2(-14f, -8f);
        }
    }
}
