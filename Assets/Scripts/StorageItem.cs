using System;
using System.Linq;
using UnityEngine;

public class StorageItem : MonoBehaviour
{
    [SerializeField] private StorageGameConfig _config;
    [SerializeField] private SpriteRenderer _imageSprite;
    [SerializeField] private SpriteRenderer _backgroundSprite;

    [SerializeField] private ItemType _itemType;
    public ItemType ItemType => _itemType;

    private void Start()
    {
        _imageSprite.sprite = _config.ItemSprites.FirstOrDefault(x => x.ItemType == _itemType).GetRandomSprite();
        _backgroundSprite.color = _config.ItemSprites.FirstOrDefault(x => x.ItemType == _itemType).BackgroundColor;
    }
}

public enum ItemType
{
    Toy,
    Food,
    Tool
}