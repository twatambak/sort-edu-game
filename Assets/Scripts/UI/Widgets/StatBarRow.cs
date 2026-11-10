using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OndeVai.UI
{
    /// <summary>
    /// Label, horizontal fill bar and value text, with an optional icon before the label.
    /// Used for accuracy per category, scenario and item.
    /// </summary>
    public sealed class StatBarRow : MonoBehaviour
    {
        private const float FillDuration = 0.5f;
        private const float IconSize = 64f;
        private const float IconSpacing = 12f;

        [SerializeField] private TMP_Text _label;
        [SerializeField] private Image _fill;
        [SerializeField] private TMP_Text _value;
        [SerializeField] private Gradient _colorByRatio = DefaultGradient();

        private Image _icon;

        /// <summary>Shows a ratio in 0..1. Negative ratios show an empty bar. The bar grows in after <paramref name="delay"/> seconds.</summary>
        public void Show(string label, float ratio, string valueText, Sprite icon = null, float delay = 0f)
        {
            if (_label != null)
                _label.text = label;

            if (_value != null)
                _value.text = valueText;

            ShowIcon(icon);

            if (_fill == null)
                return;

            float clamped = Mathf.Clamp01(ratio);
            _fill.color = _colorByRatio.Evaluate(clamped);
            _fill.DOKill();
            _fill.fillAmount = 0f;
            if (ratio > 0f)
                _fill.DOFillAmount(clamped, FillDuration).SetDelay(delay).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
        }

        private void ShowIcon(Sprite icon)
        {
            if (_label == null)
                return;

            bool hasIcon = icon != null;
            if (hasIcon && _icon == null)
                _icon = CreateIcon();

            if (_icon != null)
            {
                _icon.gameObject.SetActive(hasIcon);
                _icon.sprite = icon;
            }

            _label.margin = new Vector4(hasIcon ? IconSize + IconSpacing : 0f, 0f, 0f, 0f);
        }

        private Image CreateIcon()
        {
            Image icon = RuntimeUI.Image("Icon", _label.transform, null, Color.white);
            icon.preserveAspect = true;
            RectTransform rect = icon.rectTransform;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(IconSize, IconSize);
            return icon;
        }

        private static Gradient DefaultGradient()
        {
            Gradient gradient = new();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.95f, 0.45f, 0.4f), 0f),
                    new GradientColorKey(new Color(1f, 0.8f, 0.3f), 0.5f),
                    new GradientColorKey(new Color(0.4f, 0.8f, 0.45f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }
    }
}
