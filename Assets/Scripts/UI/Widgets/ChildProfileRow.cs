using System;
using OndeVai.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OndeVai.UI
{
    /// <summary>One child in the profile list.</summary>
    public sealed class ChildProfileRow : MonoBehaviour
    {
        private const float MinDetailFontSize = 22f;

        [SerializeField] private Button _selectButton;
        [SerializeField] private Button _editButton;
        [SerializeField] private AvatarBadge _badge;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _detailText;
        [SerializeField] private GameObject _activeMarker;

        private ChildProfileRecord _profile;
        private Action<ChildProfileRecord> _onSelect;
        private Action<ChildProfileRecord> _onEdit;

        private void Awake()
        {
            if (_selectButton != null)
                _selectButton.onClick.AddListener(() => _onSelect?.Invoke(_profile));

            if (_editButton != null)
                _editButton.onClick.AddListener(() => _onEdit?.Invoke(_profile));

            // The detail line carries the play summary, so it must shrink instead of wrapping.
            if (_detailText != null)
            {
                _detailText.fontSizeMax = _detailText.fontSize;
                _detailText.fontSizeMin = MinDetailFontSize;
                _detailText.enableAutoSizing = true;
                _detailText.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        /// <summary>Shows a child with a one line summary of their history.</summary>
        public void Bind(ChildProfileRecord profile, ChildQuickStats stats, bool isActive, Action<ChildProfileRecord> onSelect, Action<ChildProfileRecord> onEdit)
        {
            _profile = profile;
            _onSelect = onSelect;
            _onEdit = onEdit;

            if (_badge != null)
                _badge.Show(profile);

            if (_nameText != null)
                _nameText.text = profile.Name;

            if (_detailText != null)
                _detailText.text = FormatDetail(profile, stats);

            if (_activeMarker != null)
                _activeMarker.SetActive(isActive);
        }

        private static string FormatDetail(ChildProfileRecord profile, ChildQuickStats stats)
        {
            string age = profile.Age == 1 ? "1 ano" : $"{profile.Age} anos";
            if (stats == null || stats.GamesPlayed == 0)
                return $"{age} • ainda não jogou";

            string games = stats.GamesPlayed == 1 ? "1 partida" : $"{stats.GamesPlayed} partidas";
            return $"{age} • {games} • {DisplayNames.Percent(stats.Accuracy)}";
        }
    }
}
