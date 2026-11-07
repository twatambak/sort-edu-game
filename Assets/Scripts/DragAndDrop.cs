using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public class DragAndDrop : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private const float LiftedScale = 1.15f;
    private const float ScreenEdgePadding = 0.3f;

    [SerializeField] private UnityEvent _onBeginDrag;
    [SerializeField] private UnityEvent _onDrag;
    [SerializeField] private UnityEvent _onEndDrag;

    private Camera _camera;
    private Collider2D _collider;
    private Item _item;
    private Vector3 _dragOffset;
    private float _zDistance;
    private Rigidbody2D _rigidbody;
    private RigidbodyType2D _originalBodyType;
    private bool _isDragging;
    private int _sortingOrderBoost;
    private SpriteRenderer[] _renderers;

    private readonly System.Collections.Generic.List<Collider2D> _overlapBuffer = new(16);

    private void Awake()
    {
        _camera = Camera.main;
        _collider = GetComponent<Collider2D>();
        TryGetComponent(out _item);
        _renderers = GetComponentsInChildren<SpriteRenderer>();

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
        _zDistance = Mathf.Abs(transform.position.z - _camera.transform.position.z);
        Vector3 pointerWorldPosition = _camera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, _zDistance));
        _dragOffset = transform.position - pointerWorldPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanInteract())
        {
            eventData.pointerDrag = null;
            return;
        }

        _isDragging = true;
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

        Vector3 pointerWorldPosition = _camera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, _zDistance));
        transform.position = ClampToView(pointerWorldPosition + _dragOffset);

        _onDrag?.Invoke();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging)
            return;

        _isDragging = false;
        if (_rigidbody)
            _rigidbody.bodyType = _originalBodyType;

        SetLifted(false);
        GameController.Instance?.NotifyItemReleased(_item);

        if (CanInteract())
            TryDrop();

        _onEndDrag?.Invoke();
    }

    private void CancelDrag()
    {
        _isDragging = false;
        if (_rigidbody)
            _rigidbody.bodyType = _originalBodyType;

        SetLifted(false);
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
            _item.PresentationScale = lifted ? LiftedScale : 1f;

        int delta = lifted ? 100 : -_sortingOrderBoost;
        _sortingOrderBoost = lifted ? 100 : 0;
        if (delta == 0)
            return;

        foreach (SpriteRenderer spriteRenderer in _renderers)
        {
            if (spriteRenderer != null)
                spriteRenderer.sortingOrder += delta;
        }
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

    private void TryDrop()
    {
        if (_collider == null)
            return;

        ContactFilter2D filter = new() { useTriggers = true };
        int count = Physics2D.OverlapCircle(_collider.bounds.center, _collider.bounds.extents.magnitude, filter, _overlapBuffer);

        for (int i = 0; i < count; i++)
        {
            Collider2D candidate = _overlapBuffer[i];
            if (!candidate.TryGetComponent<IDropTarget>(out var dropTarget))
                continue;

            if (!_collider.Distance(candidate).isOverlapped)
                continue;

            dropTarget.OnDrop(gameObject);
            return;
        }
    }
}
