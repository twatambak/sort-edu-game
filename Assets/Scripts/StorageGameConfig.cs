using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StorageGameConfig", menuName = "Scriptable Objects/StorageGameConfig")]
public class StorageGameConfig : ScriptableObject
{
    public int MatchTime = 30;
    public int MaxItems = 10;
    public List<ItemSpritePack> ItemSprites;
}

[Serializable]
public struct ItemSpritePack
{
    public ItemType ItemType;  
    public Sprite[] Sprites;
    public Color BackgroundColor;

    public Sprite GetRandomSprite()
    {
        if (Sprites == null || Sprites.Length == 0)
            return null;
        int randomIndex = UnityEngine.Random.Range(0, Sprites.Length);
        return Sprites[randomIndex];
    }
}