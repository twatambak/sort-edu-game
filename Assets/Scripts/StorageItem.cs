using UnityEngine;

public class StorageItem : MonoBehaviour
{
    [SerializeField] private ItemType _itemType;

    public ItemType ItemType => _itemType;

    private void Update()
    {
        float normalizedY = Mathf.InverseLerp(-5f, 5f, transform.position.y);
        float scaleMultiplier = Mathf.Lerp(1f, 0.6f, normalizedY);

        transform.localScale = Vector3.one * scaleMultiplier;
    }
}

public enum ItemType
{
    Toy,
    Food,
    Tool
}