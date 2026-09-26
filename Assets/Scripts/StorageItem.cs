using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private ItemGroup _itemType;
    [SerializeField] private bool _shouldScaleWithYPosition = true;

    public ItemGroup ItemType => _itemType;

    private void Update()
    {
        if (_shouldScaleWithYPosition)
        {
            float normalizedY = Mathf.InverseLerp(-5f, 5f, transform.position.y);
            float scaleMultiplier = Mathf.Lerp(1f, 0.6f, normalizedY);

            transform.localScale = Vector3.one * scaleMultiplier;            
        }
    }
}

