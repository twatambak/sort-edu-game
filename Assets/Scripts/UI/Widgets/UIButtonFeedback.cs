using DG.Tweening;
using OndeVai.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OndeVai.UI
{
    /// <summary>
    /// Press squash and click sound for buttons. Works while the game is paused.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public sealed class UIButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField, Range(0.5f, 1f)] private float _pressedScale = 0.9f;
        [SerializeField] private bool _playClickSound = true;

        private Selectable _selectable;
        private Vector3 _restScale;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
            _restScale = transform.localScale;
        }

        private void OnDisable()
        {
            transform.DOKill();
            transform.localScale = _restScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_selectable.IsInteractable())
                return;

            transform.DOKill();
            transform.DOScale(_restScale * _pressedScale, 0.08f).SetUpdate(true).SetLink(gameObject);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            transform.DOKill();
            transform.DOScale(_restScale, 0.25f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_playClickSound && _selectable.IsInteractable())
                GameAudio.Instance?.PlayClick();
        }
    }
}
