using UnityEngine;

public class StorageBox : MonoBehaviour, IDropTarget
{
    [SerializeField] private ItemType _storageType;
    [SerializeField] private float _repulseForce;

    private Collider2D _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
    }

    public void OnDrop(GameObject droppedObject)
    {
        if (!droppedObject)
            return;

        if (!droppedObject.TryGetComponent(out StorageItem storageItem))
            return;

        if (storageItem.ItemType != _storageType)
        {
            EjectItem(droppedObject);
            Tweenimation.Spring(gameObject);
            StorageGameController.Instance.HandleIncorrectStorage();
            return;
        }
        StorageGameController.Instance.HandleCorrectStorage();
        Destroy(droppedObject);
        Tweenimation.Nod(gameObject);
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

    private Vector2 GetValidEjectionDirection(Vector2 direction)
    {
        Vector2 normalizedDirection = direction.normalized;

        Vector2[] validDirections =
        {
            Vector2.left,
            Vector2.right,
            Vector2.up
        };

        Vector2 bestDirection = Vector2.up;
        float bestDot = float.NegativeInfinity;

        foreach (Vector2 validDirection in validDirections)
        {
            float dot = Vector2.Dot(normalizedDirection, validDirection);

            if (dot > bestDot)
            {
                bestDot = dot;
                bestDirection = validDirection;
            }
        }

        return bestDirection;
    }
}