using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class DragAndDrop : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private UnityEvent _onBeginDrag;
    [SerializeField] private UnityEvent _onDrag;
    [SerializeField] private UnityEvent _onEndDrag;
    [SerializeField] private UnityEvent _onPointerDown;

    private Camera _camera;
    private Collider2D _collider;
    private Vector3 _dragOffset;
    private float _zDistance;

    private void Awake()
    {
        _camera = Camera.main;
        _collider = GetComponent<Collider2D>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _onPointerDown?.Invoke();

        _zDistance = Mathf.Abs(transform.position.z - _camera.transform.position.z);

        Vector3 pointerWorldPosition = _camera.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, _zDistance)
        );

        _dragOffset = transform.position - pointerWorldPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _onBeginDrag?.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 pointerWorldPosition = _camera.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, _zDistance)
        );

        transform.position = pointerWorldPosition + _dragOffset;

        _onDrag?.Invoke();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        TryDrop();

        _onEndDrag?.Invoke();
    }

    private void TryDrop()
    {
        if (_collider == null)
            return;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            _collider.bounds.center,
            _collider.bounds.extents.magnitude
        );

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