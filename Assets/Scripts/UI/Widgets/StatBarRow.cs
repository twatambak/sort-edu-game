using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OndeVai.UI
{
    /// <summary>Label, horizontal fill bar and value text. Used for accuracy per category.</summary>
    public sealed class StatBarRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Image _fill;
        [SerializeField] private TMP_Text _value;
        [SerializeField] private Gradient _colorByRatio = DefaultGradient();

        /// <summary>Shows a ratio in 0..1. Negative ratios show an empty bar.</summary>
        public void Show(string label, float ratio, string valueText)
        {
            if (_label != null)
                _label.text = label;

            if (_value != null)
                _value.text = valueText;

            if (_fill == null)
                return;

            float clamped = Mathf.Clamp01(ratio);
            _fill.fillAmount = ratio < 0f ? 0f : clamped;
            _fill.color = _colorByRatio.Evaluate(clamped);
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
