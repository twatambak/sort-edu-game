using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public class DragAndDrop : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private UnityEvent _onBeginDrag;
    [SerializeField] private UnityEvent _onDrag;
    [SerializeField] private UnityEvent _onEndDrag;

    private Camera _camera;
    private Collider2D _collider;
    private Vector3 _dragOffset;
    private float _zDistance;
    private Rigidbody2D _rigidbody;
    private RigidbodyType2D _originalBodyType;

    private void Awake()
    {
        _camera = Camera.main;
        _collider = GetComponent<Collider2D>();

        if (TryGetComponent(out Rigidbody2D rigidbody))
        {
            _rigidbody = rigidbody;
            _originalBodyType = _rigidbody.bodyType;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _zDistance = Mathf.Abs(transform.position.z - _camera.transform.position.z);
        Vector3 pointerWorldPosition = _camera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, _zDistance));
        _dragOffset = transform.position - pointerWorldPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_rigidbody)
        {
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
            _rigidbody.bodyType = RigidbodyType2D.Kinematic;
        }

        _onBeginDrag?.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 pointerWorldPosition = _camera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, _zDistance)
        );

        transform.position = pointerWorldPosition + _dragOffset;

        _onDrag?.Invoke();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_rigidbody)
            _rigidbody.bodyType = _originalBodyType;

        TryDrop();

        _onEndDrag?.Invoke();
    }

    private void TryDrop()
    {
        if (_collider == null)
            return;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(_collider.bounds.center, _collider.bounds.extents.magnitude);

        foreach (Collider2D collider in colliders)
        {
            if (!collider.TryGetComponent<IDropTarget>(out var dropTarget))
                continue;

            if (!_collider.Distance(collider).isOverlapped)
                continue;

            dropTarget.OnDrop(gameObject);
            return;
        }
    }
}