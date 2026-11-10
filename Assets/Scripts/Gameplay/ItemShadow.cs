using DG.Tweening;
using UnityEngine;

namespace OndeVai.Gameplay
{
    /// <summary>
    /// Soft drop shadow shown under an item while it is held, so it reads as lifted off the table.
    /// Created on demand as a child of the item and reused while the item is pooled.
    /// </summary>
    public sealed class ItemShadow : MonoBehaviour
    {
        private const float FadeDuration = 0.15f;
        private const float Opacity = 0.28f;

        private static readonly Vector2 WorldOffset = new(0.1f, -0.16f);

        private SpriteRenderer _renderer;

        /// <summary>Adds a hidden shadow under <paramref name="owner"/>.</summary>
        public static ItemShadow Create(Transform owner)
        {
            GameObject shadowObject = new("Shadow");
            shadowObject.transform.SetParent(owner, false);
            ItemShadow shadow = shadowObject.AddComponent<ItemShadow>();
            shadow._renderer = shadowObject.AddComponent<SpriteRenderer>();
            shadow._renderer.color = new Color(0f, 0f, 0f, 0f);
            shadowObject.SetActive(false);
            return shadow;
        }

        /// <summary>Copies the look of <paramref name="source"/> and fades the shadow in right behind it.</summary>
        public void Show(SpriteRenderer source)
        {
            if (source == null)
                return;

            _renderer.sprite = source.sprite;
            _renderer.flipX = source.flipX;
            _renderer.flipY = source.flipY;
            _renderer.sortingLayerID = source.sortingLayerID;
            _renderer.sortingOrder = source.sortingOrder - 1;

            float parentScale = Mathf.Max(0.0001f, transform.parent.lossyScale.x);
            transform.localPosition = WorldOffset / parentScale;
            gameObject.SetActive(true);

            _renderer.DOKill();
            _renderer.DOFade(Opacity, FadeDuration).SetLink(gameObject);
        }

        public void Hide()
        {
            if (!gameObject.activeSelf)
                return;

            _renderer.DOKill();
            _renderer.DOFade(0f, FadeDuration).SetLink(gameObject).OnComplete(Deactivate);
        }

        /// <summary>Hides without animation. Used when the item goes back to its pool.</summary>
        public void HideImmediate()
        {
            _renderer.DOKill();
            Color color = _renderer.color;
            color.a = 0f;
            _renderer.color = color;
            gameObject.SetActive(false);
        }

        private void Deactivate()
        {
            gameObject.SetActive(false);
        }
    }
}
