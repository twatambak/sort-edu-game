using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "GameDefinition", menuName = "Scriptable Objects/GameDefinition")]
public class GameDefinition : ScriptableObject
{
    public List<ItemGroupDefinition> Items;

    public GameObject GetRandomGameObject(ItemGroup itemGroup)
    {
        if (Items == null || Items.Count == 0)
            return null;

        ItemGroupDefinition group = Items.FirstOrDefault(i => i.ItemType == itemGroup);
        if (group.Prefabs == null || group.Prefabs.Length == 0)
            return null;

        int randomIndex = UnityEngine.Random.Range(0, group.Prefabs.Length);
        return group.Prefabs[randomIndex];
    }
}

[Serializable]
public struct ItemGroupDefinition
{
    public ItemGroup ItemType;  
    public GameObject[] Prefabs;
    //public Storage StoragePrefab;
}

public enum ItemGroup
{
    Toy,
    Food,
    Tool,
    Animal
}
