using UnityEngine;
using UnityEngine.EventSystems;

public class StorageBox : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if(eventData.pointerDrag != null)
        {
            Debug.Log($"Item {eventData.pointerDrag.name} dropped on storage box");
            Destroy(eventData.pointerDrag);
            Tweenimation.Impact(this.gameObject);
        }
    }

}
