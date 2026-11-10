using DG.Tweening;
using NaughtyAttributes;
using OndeVai.Effects;
using UnityEngine;

namespace OndeVai.Gameplay
{
    /// <summary>
    /// Owns the category imagery of a sorting target and every visual reaction it has: the outline
    /// that invites a drop, the hover and swipe lean highlights, the tutorial pulse and the
    /// right or wrong flash.
    /// </summary>
    public sealed class SortingSourcePresentation : MonoBehaviour
    {
        private const float FrameInset = 0.12f;
        private const float FlashDuration = 0.45f;
        private const float FrameFadeDuration = 0.15f;
        // Above the category images, below the swipe card slot and the items.
        private const int FrameSortingOrder = -1;

        private static readonly Color RestColor = Color.white;
        private static readonly Color DimColor = new(0.6f, 0.6f, 0.62f, 1f);
        private static readonly Color CorrectTint = new(0.82f, 1f, 0.82f, 1f);
        private static readonly Color CorrectFrameColor = new(0.35f, 0.9f, 0.5f, 1f);
        private static readonly Color WrongFrameColor = new(1f, 0.35f, 0.35f, 1f);
        private static readonly Color InviteColor = Color.white;
        private static readonly Color TutorialColor = new(1f, 0.85f, 0.25f, 1f);

        [SerializeField, Required] private SpriteRenderer _swipeCardBackground;
        [SerializeField, Required] private SpriteRenderer _dragAndDropTarget;
        [SerializeField] private Color _errorFlashColor = new(1f, 0.55f, 0.55f, 1f);

        private SpriteRenderer _activeImage;
        private SpriteRenderer _frame;
        private Tween _frameTween;
        private Color _categoryColor = Color.white;
        private bool _hovered;
        private bool _inviting;
        private bool _highlighted;
        private bool _leaning;

        /// <summary>Color of the category shown by this target.</summary>
        public Color CategoryColor => _categoryColor;

        private void OnDisable()
        {
            ResetState();
        }

        /// <summary>Shows the imagery for a game mode and category, and clears every highlight.</summary>
        public void Initialize(GameMode gameMode, Sprite image, Color categoryColor)
        {
            ResetState();

            bool swipe = gameMode == GameMode.SwipeTwoDirections;
            _swipeCardBackground.gameObject.SetActive(swipe);
            _dragAndDropTarget.gameObject.SetActive(!swipe);
            _activeImage = swipe ? _swipeCardBackground : _dragAndDropTarget;
            _activeImage.sprite = image;
            _activeImage.color = RestColor;

            categoryColor.a = 1f;
            _categoryColor = categoryColor;
            FitFrame();
        }

        /// <summary>Soft pulsing outline shown while an item is in the child's hand.</summary>
        public void SetInviting(bool inviting)
        {
            if (_inviting == inviting)
                return;

            _inviting = inviting;
            RefreshFrame();
        }

        /// <summary>Strong outline in the category color while a dragged item is over the target.</summary>
        public void SetHovered(bool hovered)
        {
            if (_hovered == hovered)
                return;

            _hovered = hovered;
            RefreshFrame();
        }

        /// <summary>Pulsing outline used by the tutorial to point at the right target.</summary>
        public void SetHighlighted(bool highlighted)
        {
            if (_highlighted == highlighted)
                return;

            _highlighted = highlighted;
            RefreshFrame();
        }

        /// <summary>
        /// Follows a swipe card. Positive values (up to 1) mean the card leans toward this target and
        /// light its outline; negative values (down to -1) mean it leans away and dim the image.
        /// </summary>
        public void SetLean(float amount)
        {
            if (_activeImage == null)
                return;

            _activeImage.DOKill();
            _activeImage.color = Color.Lerp(RestColor, DimColor, Mathf.Clamp01(-amount));

            if (amount > 0f)
            {
                _leaning = true;
                Color color = _categoryColor;
                color.a = Mathf.Clamp01(amount);
                ShowFrame(color);
            }
            else if (_leaning)
            {
                _leaning = false;
                RefreshFrame();
            }
        }

        /// <summary>Flashes the image and the outline green for a right answer or red for a wrong one.</summary>
        public void FlashResult(bool correct)
        {
            if (_activeImage != null)
            {
                _activeImage.DOKill();
                _activeImage.color = correct ? CorrectTint : _errorFlashColor;
                _activeImage.DOColor(RestColor, FlashDuration).SetLink(gameObject);
            }

            if (_frame == null)
                return;

            _leaning = false;
            ShowFrame(correct ? CorrectFrameColor : WrongFrameColor);
            _frameTween = _frame.DOFade(0f, FlashDuration)
                .SetEase(Ease.InQuad)
                .SetLink(gameObject)
                .OnComplete(RefreshFrame);
        }

        private void RefreshFrame()
        {
            if (_frame == null)
                return;

            if (_hovered)
                PulseFrame(_categoryColor, 0.75f, 1f, 0.22f);
            else if (_highlighted)
                PulseFrame(TutorialColor, 0.35f, 1f, 0.5f);
            else if (_inviting)
                PulseFrame(InviteColor, 0.25f, 0.65f, 0.6f);
            else
                HideFrame();
        }

        private void PulseFrame(Color color, float minAlpha, float maxAlpha, float halfPeriod)
        {
            color.a = maxAlpha;
            ShowFrame(color);
            _frameTween = _frame.DOFade(minAlpha, halfPeriod)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetLink(gameObject);
        }

        private void ShowFrame(Color color)
        {
            if (_frame == null)
                return;

            _frameTween?.Kill();
            _frameTween = null;
            _frame.color = color;
            _frame.gameObject.SetActive(true);
        }

        private void HideFrame()
        {
            _frameTween?.Kill();
            _frameTween = null;
            if (!_frame.gameObject.activeSelf)
                return;

            _frameTween = _frame.DOFade(0f, FrameFadeDuration)
                .SetLink(gameObject)
                .OnComplete(DeactivateFrame);
        }

        private void DeactivateFrame()
        {
            _frame.gameObject.SetActive(false);
        }

        private void ResetState()
        {
            _hovered = false;
            _inviting = false;
            _highlighted = false;
            _leaning = false;
            _frameTween?.Kill();
            _frameTween = null;

            // Runs from OnDisable too, where changing the active state of a child is not allowed.
            if (_frame != null)
                _frame.color = new Color(1f, 1f, 1f, 0f);

            if (_activeImage != null)
            {
                _activeImage.DOKill();
                _activeImage.color = RestColor;
            }
        }

        /// <summary>Sizes the outline to the visible part of the active image.</summary>
        private void FitFrame()
        {
            Camera viewCamera = Camera.main;
            if (viewCamera == null || !viewCamera.orthographic)
            {
                Debug.LogError("SortingSourcePresentation needs an orthographic main camera to size its highlight.", this);
                return;
            }

            Bounds image = _activeImage.bounds;
            float halfHeight = viewCamera.orthographicSize;
            float halfWidth = halfHeight * viewCamera.aspect;
            Vector3 view = viewCamera.transform.position;

            float minX = Mathf.Max(image.min.x, view.x - halfWidth) + FrameInset;
            float maxX = Mathf.Min(image.max.x, view.x + halfWidth) - FrameInset;
            float minY = Mathf.Max(image.min.y, view.y - halfHeight) + FrameInset;
            float maxY = Mathf.Min(image.max.y, view.y + halfHeight) - FrameInset;
            if (maxX <= minX || maxY <= minY)
                return;

            EnsureFrame();
            Transform frameTransform = _frame.transform;
            frameTransform.position = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, transform.position.z);

            Vector3 scale = frameTransform.lossyScale;
            _frame.size = new Vector2(
                (maxX - minX) / Mathf.Max(0.0001f, scale.x),
                (maxY - minY) / Mathf.Max(0.0001f, scale.y));
        }

        private void EnsureFrame()
        {
            if (_frame != null)
                return;

            GameObject frameObject = new("HighlightFrame");
            frameObject.transform.SetParent(transform, false);
            _frame = frameObject.AddComponent<SpriteRenderer>();
            _frame.sprite = ProceduralSprites.RoundedFrame;
            _frame.drawMode = SpriteDrawMode.Sliced;
            _frame.sortingLayerID = _activeImage.sortingLayerID;
            _frame.sortingOrder = FrameSortingOrder;
            frameObject.SetActive(false);
        }
    }
}
