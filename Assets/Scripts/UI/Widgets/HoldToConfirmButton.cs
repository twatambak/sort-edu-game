using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OndeVai.UI
{
    /// <summary>
    /// Button that must be held for a while before it fires. Used as a simple parental gate for
    /// adult areas and as confirmation for destructive actions.
    /// </summary>
    public sealed class HoldToConfirmButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField, Min(0.1f)] private float _holdSeconds = 1.5f;
        [Tooltip("Radial or horizontal filled image that shows the hold progress.")]
        [SerializeField] private Image _progressFill;
        [SerializeField] private UnityEvent _onConfirmed = new();

        private float _heldTime;
        private bool _isHolding;

        public UnityEvent OnConfirmed => _onConfirmed;

        private void Awake()
        {
            SetProgress(0f);
        }

        private void OnDisable()
        {
            _isHolding = false;
            SetProgress(0f);
        }

        private void Update()
        {
            if (!_isHolding)
                return;

            _heldTime += Time.unscaledDeltaTime;
            SetProgress(_heldTime / _holdSeconds);
            if (_heldTime < _holdSeconds)
                return;

            Release();
            _onConfirmed.Invoke();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isHolding = true;
            _heldTime = 0f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Release();
        }

        private void Release()
        {
            _isHolding = false;
            SetProgress(0f);
        }

        private void SetProgress(float progress)
        {
            if (_progressFill != null)
                _progressFill.fillAmount = Mathf.Clamp01(progress);
        }
    }
}
