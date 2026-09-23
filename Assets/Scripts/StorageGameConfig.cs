using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StorageGameConfig", menuName = "Scriptable Objects/StorageGameConfig")]
public class StorageGameConfig : ScriptableObject
{
    public int MatchTime = 30;
    public int MaxItems = 10;
    public List<ItemPrefabs> Items;
}

[Serializable]
public struct ItemPrefabs
{
    public ItemType ItemType;  
    public GameObject[] Prefab;

    public GameObject GetRandomGameObject()
    {
        if (Prefab == null || Prefab.Length == 0)
            return null;
        int randomIndex = UnityEngine.Random.Range(0, Prefab.Length);
        return Prefab[randomIndex];
    }
}