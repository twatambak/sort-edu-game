using System;
using DG.Tweening;
using UnityEngine;

namespace OndeVai.UI
{
    /// <summary>
    /// Full screen panel with animated show/hide. Animations use unscaled time so they work while
    /// the game is paused.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIScreen : MonoBehaviour
    {
        private const float FadeDuration = 0.2f;

        [Tooltip("Optional child animated with a small pop when the screen opens.")]
        [SerializeField] private RectTransform _content;

        private CanvasGroup _canvasGroup;
        private Sequence _transition;

        public event Action Shown;
        public event Action Hidden;

        public bool IsVisible { get; private set; }

        protected CanvasGroup Group => _canvasGroup != null ? _canvasGroup : _canvasGroup = GetComponent<CanvasGroup>();

        protected virtual void Awake()
        {
            Group.alpha = gameObject.activeSelf ? 1f : 0f;
            IsVisible = gameObject.activeSelf;
        }

        public void Show()
        {
            _transition?.Kill();
            gameObject.SetActive(true);
            IsVisible = true;
            transform.SetAsLastSibling();
            Group.interactable = true;
            Group.blocksRaycasts = true;

            _transition = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _transition.Append(Group.DOFade(1f, FadeDuration).From(0f));
            if (_content != null)
                _transition.Join(_content.DOScale(1f, FadeDuration * 1.5f).From(0.9f).SetEase(Ease.OutBack));

            OnShown();
            Shown?.Invoke();
        }

        public void Hide()
        {
            if (!IsVisible)
            {
                gameObject.SetActive(false);
                return;
            }

            _transition?.Kill();
            IsVisible = false;
            Group.interactable = false;
            Group.blocksRaycasts = false;

            _transition = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _transition.Append(Group.DOFade(0f, FadeDuration));
            _transition.OnComplete(() => gameObject.SetActive(false));

            OnHidden();
            Hidden?.Invoke();
        }

        /// <summary>Hides immediately without animation.</summary>
        public void HideInstant()
        {
            _transition?.Kill();
            IsVisible = false;
            Group.alpha = 0f;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Gives the screen a chance to consume the back button (e.g. to close an inner dialog).
        /// Return false to let the flow close the screen.
        /// </summary>
        public virtual bool HandleBack()
        {
            return false;
        }

        /// <summary>Called every time the screen opens. Refresh displayed data here.</summary>
        protected virtual void OnShown()
        {
        }

        protected virtual void OnHidden()
        {
        }
    }
}
