using OndeVai.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OndeVai.UI
{
    /// <summary>Colored circle with the child's initial.</summary>
    public sealed class AvatarBadge : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _initial;
        [SerializeField] private Color _emptyColor = new(1f, 1f, 1f, 0.35f);

        public void Show(ChildProfileRecord profile)
        {
            if (profile == null)
            {
                Show(string.Empty, -1);
                return;
            }

            Show(profile.Name, profile.AvatarIndex);
        }

        /// <summary>Shows an initial on a palette color. A negative index shows the empty state.</summary>
        public void Show(string childName, int avatarIndex)
        {
            if (_background != null)
                _background.color = avatarIndex < 0 ? _emptyColor : AvatarPalette.Get(avatarIndex);

            if (_initial != null)
                _initial.text = AvatarPalette.Initial(childName);
        }
    }
}
