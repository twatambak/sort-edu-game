using System;
using UnityEngine;

public class StorageItem : MonoBehaviour
{
    [SerializeField] private ItemType _itemType;
    public ItemType ItemType => _itemType;
}

public enum ItemType
{
    Toy,
    Food,
    Tool
}