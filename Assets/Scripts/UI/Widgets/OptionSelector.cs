using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OndeVai.UI
{
    /// <summary>
    /// "&lt; value &gt;" selector that cycles through a list of labelled options.
    /// </summary>
    public sealed class OptionSelector : MonoBehaviour
    {
        [SerializeField] private TMP_Text _valueText;
        [SerializeField] private Button _previousButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private bool _wrap = true;

        private readonly List<string> _labels = new();

        /// <summary>Raised with the new index when the player changes the value.</summary>
        public event Action<int> Changed;

        public int Index { get; private set; }

        private void Awake()
        {
            if (_previousButton != null)
                _previousButton.onClick.AddListener(() => Step(-1));

            if (_nextButton != null)
                _nextButton.onClick.AddListener(() => Step(1));
        }

        /// <summary>Replaces the options without raising <see cref="Changed"/>.</summary>
        public void SetOptions(IReadOnlyList<string> labels, int selectedIndex)
        {
            _labels.Clear();
            if (labels != null)
                _labels.AddRange(labels);

            Index = _labels.Count == 0 ? 0 : Mathf.Clamp(selectedIndex, 0, _labels.Count - 1);
            Refresh();
        }

        private void Step(int direction)
        {
            if (_labels.Count == 0)
                return;

            int next = Index + direction;
            next = _wrap ? (next + _labels.Count) % _labels.Count : Mathf.Clamp(next, 0, _labels.Count - 1);
            if (next == Index)
                return;

            Index = next;
            Refresh();
            Changed?.Invoke(Index);
        }

        private void Refresh()
        {
            if (_valueText != null)
                _valueText.text = _labels.Count > 0 ? _labels[Index] : "-";

            bool canStep = _labels.Count > 1;
            if (_previousButton != null)
                _previousButton.interactable = canStep && (_wrap || Index > 0);

            if (_nextButton != null)
                _nextButton.interactable = canStep && (_wrap || Index < _labels.Count - 1);
        }
    }
}
