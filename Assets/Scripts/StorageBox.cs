using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OndeVai.Gameplay
{
/// <summary>
/// A destination that accepts items of one <see cref="ItemGroup"/>. Receives drops in drag mode and
/// taps in question mode, forwards them to <see cref="GameController"/> and plays its own feedback.
/// </summary>
public class Storage : MonoBehaviour, IDropTarget, IPointerClickHandler
{
    private const string ItemTag = "StorageItem";
    private const float AbsorbDuration = 0.28f;

    private static readonly List<Storage> ActiveStorages = new();

    [SerializeField] private ItemGroup _storageType;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Sprite _chestOpenSprite;
    [SerializeField] private Sprite _chestClosedSprite;

    [Tooltip("Optional renderer that shows the icon of the group this storage receives.")]
    [SerializeField] private SpriteRenderer _groupIconRenderer;

    [SerializeField] private Color _errorFlashColor = new(1f, 0.55f, 0.55f, 1f);
    [SerializeField] private float _repulseForce = 20f;

    private Collider2D _collider;
    private Vector3 _restScale;
    private Color _restColor = Color.white;
    private Tween _highlightTween;
    private int _itemsInside;

    /// <summary>Every enabled storage. Used to resolve targets without scene searches.</summary>
    public static IReadOnlyList<Storage> Active => ActiveStorages;

    public ItemGroup StorageType => _storageType;

    /// <summary>World position items fly towards when absorbed.</summary>
    public Vector3 DropPoint => _collider != null ? _collider.bounds.center : transform.position;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _restScale = transform.localScale;
        if (_spriteRenderer != null)
            _restColor = _spriteRenderer.color;
    }

    private void OnEnable()
    {
        ActiveStorages.Add(this);
    }

    private void OnDisable()
    {
        ActiveStorages.Remove(this);
        SetHighlighted(false);
    }

    /// <summary>Changes the group accepted by this storage and, optionally, its icon.</summary>
    public void Configure(ItemGroup group, Sprite icon)
    {
        _storageType = group;
        if (_groupIconRenderer == null)
            return;

        _groupIconRenderer.sprite = icon;
        _groupIconRenderer.enabled = icon != null;
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

        GameController.Instance?.SelectStorage(this);
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
        absorb.Join(itemTransform.DOMove(DropPoint, AbsorbDuration).SetEase(Ease.InBack));
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
        FlashError();
        if (eject && item != null)
            EjectItem(item.gameObject);
    }

    /// <summary>Loops a gentle pulse to call attention to this storage, used by the tutorial.</summary>
    public void SetHighlighted(bool highlighted)
    {
        _highlightTween?.Kill();
        _highlightTween = null;
        if (!highlighted)
            return;

        _highlightTween = DOTween.To(() => 0f, value =>
            {
                if (_spriteRenderer != null)
                    _spriteRenderer.color = Color.Lerp(_restColor, Color.white * 1.2f, value);
            }, 1f, 0.5f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetLink(gameObject)
            .OnKill(() =>
            {
                if (_spriteRenderer != null)
                    _spriteRenderer.color = _restColor;
            });
    }

    private void PlayBoxAnimation(bool correct)
    {
        Tweenimation.Reset(gameObject, transform.localPosition, transform.localRotation, _restScale);
        if (correct)
            Tweenimation.Jelly(gameObject);
        else
            Tweenimation.Spring(gameObject);
    }

    private void FlashError()
    {
        if (_spriteRenderer == null || _highlightTween != null)
            return;

        _spriteRenderer.DOKill();
        _spriteRenderer.color = _errorFlashColor;
        _spriteRenderer.DOColor(_restColor, 0.4f).SetLink(gameObject);
    }

    private void EjectItem(GameObject droppedObject)
    {
        if (!droppedObject.TryGetComponent(out Rigidbody2D rigidbody2D))
            return;

        rigidbody2D.bodyType = RigidbodyType2D.Dynamic;

        Vector2 itemPosition = droppedObject.transform.position;
        Vector2 closestPoint = _collider.ClosestPoint(itemPosition);
        Vector2 direction = itemPosition - closestPoint;

        if (direction == Vector2.zero)
            direction = itemPosition - (Vector2)_collider.bounds.center;

        rigidbody2D.AddForce(direction * _repulseForce, ForceMode2D.Impulse);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag(ItemTag))
            return;

        _itemsInside++;
        if (_spriteRenderer != null && _chestOpenSprite != null)
            _spriteRenderer.sprite = _chestOpenSprite;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag(ItemTag))
            return;

        _itemsInside = Mathf.Max(0, _itemsInside - 1);
        if (_itemsInside == 0 && _spriteRenderer != null && _chestClosedSprite != null)
            _spriteRenderer.sprite = _chestClosedSprite;
    }
}
    
}
