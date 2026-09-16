using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class DragAndDrop : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    [SerializeField] private Canvas _canvas;
    [SerializeField] private UnityEvent _onBeginDrag;
    [SerializeField] private UnityEvent _onDrag;
    [SerializeField] private UnityEvent _onEndDrag;
    [SerializeField] private UnityEvent _onPointerDown;

    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _onBeginDrag?.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
        _onDrag?.Invoke();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _onEndDrag?.Invoke();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _onPointerDown?.Invoke();
    }
}
