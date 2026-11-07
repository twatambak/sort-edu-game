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
        }

        public void Bind(ChildProfileRecord profile, bool isActive, Action<ChildProfileRecord> onSelect, Action<ChildProfileRecord> onEdit)
        {
            _profile = profile;
            _onSelect = onSelect;
            _onEdit = onEdit;

            if (_badge != null)
                _badge.Show(profile);

            if (_nameText != null)
                _nameText.text = profile.Name;

            if (_detailText != null)
                _detailText.text = profile.Age == 1 ? "1 ano" : $"{profile.Age} anos";

            if (_activeMarker != null)
                _activeMarker.SetActive(isActive);
        }
    }
}
