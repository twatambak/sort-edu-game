using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

namespace OndeVai.Gameplay
{
    /// <summary>Owns the visual sizing and category imagery of a storage target.</summary>
    public sealed class SortingSourcePresentation : MonoBehaviour
    {
        [SerializeField, Required] private SpriteRenderer _swipeCardBackground;
        [SerializeField, Required] private SpriteRenderer _dragAndDropTarget;
        [SerializeField] private Color _errorFlashColor = new(1f, 0.55f, 0.55f, 1f);

        private Tween _highlightTween;
        private Color _restColor = Color.white;  

        private void OnDisable()
        {
            SetHighlighted(false);
        }
        
        public void Initialize(GameMode gameMode, Sprite image)
        {
            SetHighlighted(false);
            if(gameMode == GameMode.SwipeTwoDirections)
            {
                _swipeCardBackground.gameObject.SetActive(true);
                _dragAndDropTarget.gameObject.SetActive(false);
                _swipeCardBackground.sprite = image;
            }
            else if(gameMode == GameMode.DragAndDrop)
            {
                _swipeCardBackground.gameObject.SetActive(false);
                _dragAndDropTarget.gameObject.SetActive(true);
                _dragAndDropTarget.sprite = image;
            }
        }

        public void FlashError()
        {
            if (_swipeCardBackground == null || _highlightTween != null)
                return;

            _swipeCardBackground.DOKill();
            _swipeCardBackground.color = _errorFlashColor;
            _swipeCardBackground.DOColor(_restColor, 0.4f).SetLink(gameObject);
        }

        public void SetHighlighted(bool highlighted)
        {
            _highlightTween?.Kill();
            _highlightTween = null;
            if (!highlighted)
                return;

            _highlightTween = DOTween.To(() => 0f, value =>
                {
                    if (_swipeCardBackground != null)
                        _swipeCardBackground.color = Color.Lerp(_restColor, Color.white * 1.2f, value);
                }, 1f, 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetLink(gameObject)
                .OnKill(() =>
                {
                    if (_swipeCardBackground != null)
                        _swipeCardBackground.color = _restColor;
                });
        }
    }
}
