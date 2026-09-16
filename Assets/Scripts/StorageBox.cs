using UnityEngine;
using UnityEngine.EventSystems;

public class StorageBox : MonoBehaviour, IDropTarget
{
    public void OnDrop(GameObject droppedObject)
    {
        if (droppedObject != null)
        {
            Debug.Log($"Item {droppedObject.name} dropped on storage box");
            Destroy(droppedObject);
            Tweenimation.Impact(this.gameObject);
        }
    }
}
