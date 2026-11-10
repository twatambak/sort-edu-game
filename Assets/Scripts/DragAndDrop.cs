using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace OndeVai.Gameplay
{
    /// <summary>
    /// Lets the child drag an object with a pointer and drop it on an <see cref="IDropTarget"/>.
    /// Targets that implement <see cref="IDropHoverTarget"/> are told while the object is held over them.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DragAndDrop : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const float ScreenEdgePadding = 0.3f;

        [SerializeField] private UnityEvent _onBeginDrag;
        [SerializeField] private UnityEvent _onDrag;
        [SerializeField] private UnityEvent _onEndDrag;

        [Tooltip("World units the item is raised above the finger on touch screens, so the child can see it while dragging.")]
        [SerializeField, Min(0f)] private float _touchLiftOffset = 0.6f;

        // Only one item can be dragged at a time, so a palm or several fingers cannot grab many items.
        private static DragAndDrop _activeDrag;

        private Camera _camera;
        private Collider2D _collider;
        private Item _item;
        private Vector3 _dragOffset;
        private float _zDistance;
        private Rigidbody2D _rigidbody;
        private RigidbodyType2D _originalBodyType;
        private bool _isDragging;
        private ContactFilter2D _dropFilter;
        private IDropHoverTarget _hoveredTarget;

        private readonly List<Collider2D> _overlapBuffer = new(16);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _activeDrag = null;
        }

        private void Awake()
        {
            _camera = Camera.main;
            _collider = GetComponent<Collider2D>();
            _dropFilter = new ContactFilter2D { useTriggers = true };
            TryGetComponent(out _item);

            if (TryGetComponent(out Rigidbody2D rigidbody))
            {
                _rigidbody = rigidbody;
                _originalBodyType = _rigidbody.bodyType;
            }
        }

        private void OnDisable()
        {
            if (_isDragging)
                CancelDrag();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_camera == null)
                return;

            _zDistance = Mathf.Abs(transform.position.z - _camera.transform.position.z);
            _dragOffset = transform.position - PointerWorldPosition(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_camera == null || !CanInteract() || (_activeDrag != null && _activeDrag != this))
            {
                eventData.pointerDrag = null;
                return;
            }

            _isDragging = true;
            _activeDrag = this;
            if (IsTouch(eventData))
                _dragOffset += Vector3.up * _touchLiftOffset;

            if (_rigidbody)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.angularVelocity = 0f;
                _rigidbody.bodyType = RigidbodyType2D.Kinematic;
            }

            SetLifted(true);
            GameController.Instance?.NotifyItemPicked(_item);
            _onBeginDrag?.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging)
                return;

            transform.position = ClampToView(PointerWorldPosition(eventData) + _dragOffset);
            SetHoveredTarget(FindDropTarget() as IDropHoverTarget);

            _onDrag?.Invoke();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging)
                return;

            StopDragging();
            GameController.Instance?.NotifyItemReleased(_item);

            if (CanInteract())
                FindDropTarget()?.OnDrop(gameObject);

            _onEndDrag?.Invoke();
        }

        private void CancelDrag()
        {
            StopDragging();
            GameController.Instance?.NotifyItemReleased(_item);
        }

        private void StopDragging()
        {
            _isDragging = false;
            if (_activeDrag == this)
                _activeDrag = null;

            if (_rigidbody)
                _rigidbody.bodyType = _originalBodyType;

            SetHoveredTarget(null);
            SetLifted(false);
        }

        private Vector3 PointerWorldPosition(PointerEventData eventData)
        {
            return _camera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, _zDistance));
        }

        private static bool IsTouch(PointerEventData eventData)
        {
            return eventData is ExtendedPointerEventData extended && extended.pointerType == UIPointerType.Touch;
        }

        private bool CanInteract()
        {
            GameController controller = GameController.Instance;
            if (controller != null && !controller.CanAcceptInput)
                return false;

            return _item == null || !_item.IsResolving;
        }

        private void SetLifted(bool lifted)
        {
            if (_item != null)
                _item.SetLifted(lifted);
        }

        private void SetHoveredTarget(IDropHoverTarget target)
        {
            if (ReferenceEquals(target, _hoveredTarget))
                return;

            if (IsAlive(_hoveredTarget))
                _hoveredTarget.SetHovered(false);

            _hoveredTarget = target;
            _hoveredTarget?.SetHovered(true);
        }

        // A hovered target may be a component that was destroyed in the meantime.
        private static bool IsAlive(IDropHoverTarget target)
        {
            return target is Object unityObject ? unityObject != null : target != null;
        }

        private Vector3 ClampToView(Vector3 position)
        {
            if (_camera == null || !_camera.orthographic)
                return position;

            float halfHeight = _camera.orthographicSize - ScreenEdgePadding;
            float halfWidth = _camera.orthographicSize * _camera.aspect - ScreenEdgePadding;
            Vector3 center = _camera.transform.position;
            position.x = Mathf.Clamp(position.x, center.x - halfWidth, center.x + halfWidth);
            position.y = Mathf.Clamp(position.y, center.y - halfHeight, center.y + halfHeight);
            return position;
        }

        /// <summary>
        /// The drop target under the object, or null. When several overlap, the one closest to the
        /// object's center wins, so the highlighted target is always the one that receives the drop.
        /// </summary>
        private IDropTarget FindDropTarget()
        {
            if (_collider == null)
                return null;

            // The object is moved through its Transform, so the physics pose must catch up before querying.
            Physics2D.SyncTransforms();
            Vector2 center = transform.position;
            int count = Physics2D.OverlapCircle(center, _collider.bounds.extents.magnitude, _dropFilter, _overlapBuffer);

            IDropTarget best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider2D candidate = _overlapBuffer[i];
                if (candidate == _collider || !candidate.TryGetComponent(out IDropTarget dropTarget))
                    continue;

                if (!_collider.Distance(candidate).isOverlapped)
                    continue;

                float distance = (candidate.ClosestPoint(center) - center).sqrMagnitude;
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                best = dropTarget;
            }

            return best;
        }
    }
}
