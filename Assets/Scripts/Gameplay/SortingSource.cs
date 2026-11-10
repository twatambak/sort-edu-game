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
    public class SortingSource : MonoBehaviour, IDropTarget, IDropHoverTarget, IPointerClickHandler
    {
        private const float AbsorbDuration = 0.28f;
        private const float AbsorbSpin = 200f;
        private const float EjectSpeed = 9f;
        private const float EjectDownwardBias = 0.75f;
        private const float EjectSpin = 180f;

        private static readonly List<SortingSource> ActiveSortingSources = new();

        [SerializeField] private ItemGroup _groupTarget;
        [SerializeField, Required] private SortingSourcePresentation _presentation;
        [SerializeField] private Collider2D _dragAndDropPortalCollider;

        private GameController _gameController;
        private Vector3 _restPosition;
        private Quaternion _restRotation = Quaternion.identity;
        private Vector3 _restScale = Vector3.one;

        public static IReadOnlyList<SortingSource> Active => ActiveSortingSources;

        public ItemGroup GroupTarget => _groupTarget;

        public Vector3 DropPoint => _dragAndDropPortalCollider != null ? _dragAndDropPortalCollider.bounds.center : transform.position;

        public SortingSourcePresentation Presentation => _presentation;

        private void OnEnable()
        {
            ActiveSortingSources.Add(this);
            _restPosition = transform.localPosition;
            _restRotation = transform.localRotation;
            _restScale = transform.localScale;
        }

        private void Start()
        {
            _gameController = GameController.Instance;
            if (_gameController == null)
            {
                Debug.LogError("SortingSource could not find the GameController.", this);
                return;
            }

            _gameController.OnItemPicked += HandleItemPicked;
            _gameController.OnItemReleased += HandleItemReleased;
        }

        private void OnDisable()
        {
            ActiveSortingSources.Remove(this);
        }

        private void OnDestroy()
        {
            if (_gameController == null)
                return;

            _gameController.OnItemPicked -= HandleItemPicked;
            _gameController.OnItemReleased -= HandleItemReleased;
        }

        public void Configure(GameConfiguration gameConfig, ItemTypeDefinition type)
        {
            if (type == null)
                return;

            _groupTarget = type.Group;
            _presentation.Initialize(gameConfig.Mode, type.Background, type.Color);
        }

        public void OnDrop(GameObject droppedObject)
        {
            if (!droppedObject || !droppedObject.TryGetComponent(out Item item))
                return;

            GameController.Instance?.SubmitItem(item, this);
        }

        public void SetHovered(bool hovered)
        {
            _presentation.SetHovered(hovered);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging)
                return;

            GameController.Instance?.SelectSortingSource(this);
        }

        /// <summary>Animates the item into the storage and returns it to its pool.</summary>
        public void AcceptItem(Item item)
        {
            PlayBoxAnimation(true);
            _presentation.FlashResult(true);
            if (item == null)
                return;

            item.BeginResolve();
            item.SetKinematic(true);

            Transform itemTransform = item.transform;
            Sequence absorb = DOTween.Sequence();
            absorb.Join(itemTransform.DOMove(DropPoint, AbsorbDuration).SetEase(Ease.InBack));
            absorb.Join(itemTransform.DORotate(new Vector3(0f, 0f, AbsorbSpin), AbsorbDuration, RotateMode.FastBeyond360).SetEase(Ease.InQuad));
            absorb.Join(DOTween.To(() => item.PresentationScale, value => item.PresentationScale = value, 0f, AbsorbDuration).SetEase(Ease.InQuad));
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
            _presentation.FlashResult(false);
            if (eject && item != null)
                EjectItem(item.gameObject);
        }

        private void HandleItemPicked(Item item)
        {
            GameConfiguration config = _gameController.Config;
            if (config != null && config.Mode == GameMode.DragAndDrop)
                _presentation.SetInviting(true);
        }

        private void HandleItemReleased(Item item)
        {
            _presentation.SetInviting(false);
        }

        private void PlayBoxAnimation(bool correct)
        {
            Tweenimation.Reset(gameObject, _restPosition, _restRotation, _restScale);
            if (correct)
                Tweenimation.Jelly(gameObject);
            else
                Tweenimation.Spring(gameObject);
        }

        private void EjectItem(GameObject droppedObject)
        {
            if (_dragAndDropPortalCollider == null || !droppedObject.TryGetComponent(out Rigidbody2D rigidbody2D))
                return;

            rigidbody2D.bodyType = RigidbodyType2D.Dynamic;

            // A fixed speed, always with a downward component, sends the item back toward the table
            // no matter how deep inside the target it was dropped.
            Vector2 itemPosition = droppedObject.transform.position;
            Vector2 direction = itemPosition - (Vector2)_dragAndDropPortalCollider.bounds.center;
            direction.y = -Mathf.Abs(direction.y) - EjectDownwardBias;
            rigidbody2D.linearVelocity = direction.normalized * EjectSpeed;
            rigidbody2D.angularVelocity = Random.Range(-EjectSpin, EjectSpin);
        }
    }
}
