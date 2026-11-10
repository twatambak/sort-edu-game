using DG.Tweening;
using OndeVai.Effects;
using UnityEngine;

namespace OndeVai.Gameplay
{
    /// <summary>
    /// Two bobbing arrows beside the swipe card that show, without text, that the card moves sideways.
    /// Each arrow is tinted with the color of the category on its side.
    /// </summary>
    public sealed class SwipeHintArrows
    {
        private const float SideOffset = 1.85f;
        private const float ArrowSize = 0.55f;
        private const float BobDistance = 0.14f;
        private const float BobDuration = 0.5f;
        private const float FadeDuration = 0.2f;
        private const float ArrowOpacity = 0.95f;
        private const float ShadowOpacity = 0.3f;
        private const int SortingOrder = 4;

        private static readonly Vector3 ShadowOffset = new(0.05f, -0.07f, 0f);

        private readonly GameObject _root;
        private readonly SpriteRenderer[] _renderers = new SpriteRenderer[4];
        private bool _visible;

        public SwipeHintArrows(Transform parent, Vector3 center, Color leftColor, Color rightColor)
        {
            _root = new GameObject("SwipeHints");
            _root.transform.SetParent(parent, false);
            _root.transform.position = center;

            CreateArrow(0, -1f, leftColor);
            CreateArrow(2, 1f, rightColor);
        }

        /// <summary>Fades the arrows in or out.</summary>
        public void SetVisible(bool visible)
        {
            if (_visible == visible || _root == null)
                return;

            _visible = visible;
            for (int i = 0; i < _renderers.Length; i++)
            {
                SpriteRenderer arrow = _renderers[i];
                float opacity = i % 2 == 0 ? ShadowOpacity : ArrowOpacity;
                arrow.DOKill();
                arrow.DOFade(visible ? opacity : 0f, FadeDuration).SetLink(_root);
            }
        }

        /// <summary>Removes the arrows from the scene.</summary>
        public void Dispose()
        {
            if (_root != null)
                Object.Destroy(_root);
        }

        private void CreateArrow(int index, float side, Color categoryColor)
        {
            GameObject arrowObject = new(side < 0f ? "LeftArrow" : "RightArrow");
            Transform arrowTransform = arrowObject.transform;
            arrowTransform.SetParent(_root.transform, false);
            arrowTransform.localPosition = new Vector3(side * SideOffset, 0f, 0f);
            arrowTransform.localScale = Vector3.one * ArrowSize;

            // Half white keeps the arrow readable on top of a background of the same category color.
            Color tint = Color.Lerp(categoryColor, Color.white, 0.5f);
            _renderers[index] = CreateRenderer("Shadow", arrowTransform, ShadowOffset / ArrowSize, Color.black, SortingOrder - 1, side);
            _renderers[index + 1] = CreateRenderer("Arrow", arrowTransform, Vector3.zero, tint, SortingOrder, side);

            arrowTransform.DOLocalMoveX(side * (SideOffset + BobDistance), BobDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetLink(_root);
        }

        private static SpriteRenderer CreateRenderer(string name, Transform parent, Vector3 localPosition, Color color, int sortingOrder, float side)
        {
            GameObject rendererObject = new(name);
            rendererObject.transform.SetParent(parent, false);
            rendererObject.transform.localPosition = localPosition;

            SpriteRenderer spriteRenderer = rendererObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = ProceduralSprites.Chevron;
            spriteRenderer.flipX = side < 0f;
            spriteRenderer.sortingOrder = sortingOrder;
            color.a = 0f;
            spriteRenderer.color = color;
            return spriteRenderer;
        }
    }
}
