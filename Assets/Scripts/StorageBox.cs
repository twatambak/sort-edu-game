using UnityEngine;
using UnityEngine.EventSystems;

public class Storage : MonoBehaviour, IDropTarget
{
    [SerializeField] private GroupType _storageType;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Sprite _chestOpenSprite;
    [SerializeField] private Sprite _chestClosedSprite;

    private Collider2D _collider;
    private float _repulseForce = 20f;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
    }

    public void OnDrop(GameObject droppedObject)
    {
        if (!droppedObject)
            return;

        if (!droppedObject.TryGetComponent(out Item storageItem))
            return;

        if (storageItem.ItemType != _storageType)
        {
            EjectItem(droppedObject);
            Tweenimation.Spring(gameObject);
            GameController.Instance.HandleIncorrectStorage();
            return;
        }
        GameController.Instance.HandleCorrectStorage();
        Destroy(droppedObject);
        Tweenimation.Jelly(gameObject);
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

        //direction = GetValidEjectionDirection(direction);

        rigidbody2D.AddForce(direction * _repulseForce, ForceMode2D.Impulse);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.tag == "StorageItem")
            _spriteRenderer.sprite = _chestOpenSprite;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "StorageItem")
        {
            _spriteRenderer.sprite = _chestClosedSprite;
        }
    }

}