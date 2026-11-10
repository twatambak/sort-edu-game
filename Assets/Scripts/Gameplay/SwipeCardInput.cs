using UnityEngine;
using UnityEngine.EventSystems;

namespace OndeVai.Gameplay
{
    /// <summary>Receives the swipe gesture of a card. Deltas are in screen pixels from the press position.</summary>
    public interface ISwipeCardListener
    {
        void OnSwipeStarted(Item item);

        void OnSwipeMoved(Item item, Vector2 screenDelta);

        /// <summary><paramref name="direction"/> is null when the gesture was too short or mostly vertical.</summary>
        void OnSwipeEnded(Item item, SwipeDirection? direction);
    }

    /// <summary>Converts a pointer drag on an item into a horizontal swipe gesture.</summary>
    public sealed class SwipeCardInput : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private Item _item;
        private ISwipeCardListener _listener;
        private Vector2 _startPosition;
        private float _minimumDistance;
        private int _pointerId;
        private bool _isDragging;

        private void Awake()
        {
            TryGetComponent(out _item);
        }

        /// <summary>Enables the input. A swipe counts once it travels <paramref name="minimumDistance"/> pixels sideways.</summary>
        public void Configure(float minimumDistance, ISwipeCardListener listener)
        {
            CancelGesture();
            _minimumDistance = Mathf.Max(1f, minimumDistance);
            _listener = listener;
            enabled = true;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_item == null || _item.IsResolving || _isDragging)
                return;

            _startPosition = eventData.pressPosition;
            _pointerId = eventData.pointerId;
            _isDragging = true;
            _listener?.OnSwipeStarted(_item);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_isDragging && eventData.pointerId == _pointerId)
                _listener?.OnSwipeMoved(_item, eventData.position - _startPosition);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging || eventData.pointerId != _pointerId)
                return;

            _isDragging = false;
            _pointerId = int.MinValue;

            Vector2 delta = eventData.position - _startPosition;
            bool isSwipe = Mathf.Abs(delta.x) >= _minimumDistance && Mathf.Abs(delta.x) >= Mathf.Abs(delta.y);
            SwipeDirection? direction = null;
            if (isSwipe)
                direction = delta.x < 0f ? SwipeDirection.Left : SwipeDirection.Right;

            _listener?.OnSwipeEnded(_item, direction);
        }

        private void OnDisable()
        {
            CancelGesture();
        }

        /// <summary>Ends a gesture in progress without an answer, so the card can settle back.</summary>
        private void CancelGesture()
        {
            bool wasDragging = _isDragging;
            _isDragging = false;
            _pointerId = int.MinValue;
            if (wasDragging)
                _listener?.OnSwipeEnded(_item, null);
        }
    }
}
