using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OndeVai.Gameplay
{
    /// <summary>Converts a pointer drag on an item into a cardinal swipe gesture.</summary>
    public sealed class SwipeCardInput : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private Item _item;
        private Action<Item, SwipeDirection> _swiped;
        private Action<Item> _picked;
        private Action<Item> _released;
        private Vector2 _startPosition;
        private float _minimumDistance;
        private int _pointerId;
        private bool _allowVertical;
        private bool _isDragging;

        private void Awake()
        {
            TryGetComponent(out _item);
        }

        /// <summary>Configures callbacks and the accepted swipe axes for the current card.</summary>
        public void Configure(
            bool allowVertical,
            float minimumDistance,
            Action<Item, SwipeDirection> swiped,
            Action<Item> picked,
            Action<Item> released)
        {
            _allowVertical = allowVertical;
            _minimumDistance = Mathf.Max(1f, minimumDistance);
            _swiped = swiped;
            _picked = picked;
            _released = released;
            enabled = true;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_item == null || _item.IsResolving || _isDragging)
                return;

            _startPosition = eventData.pressPosition;
            _pointerId = eventData.pointerId;
            _isDragging = true;
            _picked?.Invoke(_item);
        }

        public void OnDrag(PointerEventData eventData)
        {
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging || eventData.pointerId != _pointerId)
                return;

            _isDragging = false;
            _released?.Invoke(_item);

            Vector2 delta = eventData.position - _startPosition;
            if (delta.magnitude < _minimumDistance)
                return;

            bool horizontal = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y);
            if (!horizontal && !_allowVertical)
                return;

            SwipeDirection direction = horizontal
                ? (delta.x < 0f ? SwipeDirection.Left : SwipeDirection.Right)
                : (delta.y < 0f ? SwipeDirection.Down : SwipeDirection.Up);
            _swiped?.Invoke(_item, direction);
        }

        private void OnDisable()
        {
            _isDragging = false;
        }
    }
}
