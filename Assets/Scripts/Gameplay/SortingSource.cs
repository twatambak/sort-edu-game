using System.Collections.Generic;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OndeVai.Gameplay
{
    /// <summary>
    /// A destination that accepts items of one <see cref="ItemGroup"/>. Receives drops in drag mode,
    /// forwards them to <see cref="GameController"/> and plays its own feedback.
    /// </summary>
    public class SortingSource : MonoBehaviour, IDropTarget, IPointerClickHandler
    {
        [SerializeField] private ItemGroup _groupTarget;
        [SerializeField, Required] private SortingSourcePresentation _presentation;
        [SerializeField] private Collider2D _dragAndDropPortalCollider;
        
        private Vector3 _restScale = Vector3.one;
        public static IReadOnlyList<SortingSource> Active => ActiveSortingSources;
        public ItemGroup GroupTarget => _groupTarget;
        private static readonly List<SortingSource> ActiveSortingSources = new();
        private const float ABSORB_DURATION = 0.28f;
        private const float REPULSE_FORCE = 20f;

        public Vector3 DropPoint => _dragAndDropPortalCollider != null ? _dragAndDropPortalCollider.bounds.center : transform.position;
        public SortingSourcePresentation Presentation => _presentation;

        private void OnEnable()
        {
            ActiveSortingSources.Add(this);
            _restScale = transform.localScale;
        }

        private void OnDisable()
        {
            ActiveSortingSources.Remove(this);
        }

        public void Configure(GameConfiguration gameConfig, ItemTypeDefinition type)
        {
            if (type == null)
                return;

            _groupTarget = type.Group;
            _presentation.Initialize(gameConfig.Mode, type.Background);
        }

        public void SetGameModeVisible(bool visible)
        {
            if (_dragAndDropPortalCollider != null)
                _dragAndDropPortalCollider.enabled = visible;
        }

        public void OnDrop(GameObject droppedObject)
        {
            if (!droppedObject || !droppedObject.TryGetComponent(out Item item))
                return;

            GameController.Instance?.SubmitItem(item, this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging)
                return;

            GameController.Instance?.SelectSortingSource(this);
        }

        /// <summary>Animates the item into the storage and destroys it.</summary>
        public void AcceptItem(Item item)
        {
            PlayBoxAnimation(true);
            if (item == null)
                return;

            item.BeginResolve();
            item.SetKinematic(true);

            Transform itemTransform = item.transform;
            Sequence absorb = DOTween.Sequence();
            absorb.Join(itemTransform.DOMove(DropPoint, ABSORB_DURATION).SetEase(Ease.InBack));
            absorb.Join(DOTween.To(() => item.PresentationScale, value => item.PresentationScale = value, 0f, ABSORB_DURATION).SetEase(Ease.InQuad));
            absorb.SetTarget(item).SetLink(item.gameObject);
            absorb.OnComplete(() =>
            {
                if (item != null)
                    item.Despawn();
            });
        }

        /// <summary>Plays the wrong answer feedback and optionally pushes the item away.</summary>
        public void RejectItem(Item item, bool eject)
        {
            PlayBoxAnimation(false);
            if (eject && item != null)
                EjectItem(item.gameObject);

            _presentation.FlashError();
        }


        private void PlayBoxAnimation(bool correct)
        {
            Tweenimation.Reset(gameObject, transform.localPosition, transform.localRotation, _restScale);
            if (correct)
                Tweenimation.Jelly(gameObject);
            else
                Tweenimation.Spring(gameObject);
        }


        private void EjectItem(GameObject droppedObject)
        {
            if (!droppedObject.TryGetComponent(out Rigidbody2D rigidbody2D))
                return;

            rigidbody2D.bodyType = RigidbodyType2D.Dynamic;

            Vector2 itemPosition = droppedObject.transform.position;
            Vector2 closestPoint = _dragAndDropPortalCollider.ClosestPoint(itemPosition);
            Vector2 direction = itemPosition - closestPoint;

            if (direction == Vector2.zero)
                direction = itemPosition - (Vector2)_dragAndDropPortalCollider.bounds.center;

            rigidbody2D.AddForce(direction * REPULSE_FORCE, ForceMode2D.Impulse);
        }

    }
}
