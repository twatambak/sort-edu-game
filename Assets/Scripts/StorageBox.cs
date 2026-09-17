using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class StorageBox : MonoBehaviour, IDropTarget
{
    [SerializeField] private ItemType _storageType;

    public void OnDrop(GameObject droppedObject)
    {
        if (!droppedObject)
            return;

        if (droppedObject.TryGetComponent(out StorageItem storageItem))
        {
            if (storageItem.ItemType != _storageType)
            {
                if(droppedObject.TryGetComponent(out Rigidbody2D rigidbody2D))
                {
                    rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
                    rigidbody2D.AddForce(Vector2.up * 5f, ForceMode2D.Impulse);
                }
                return;
            }

            Destroy(droppedObject);
            Tweenimation.Impact(this.gameObject);
        }
    }
}
