using UnityEngine;

[CreateAssetMenu(fileName = "StorageGameConfig", menuName = "Scriptable Objects/StorageGameConfig")]
public class StorageGameConfig : ScriptableObject
{
    public int MatchTime = 30;
    public int MaxItems = 10;
    public StorageItem[] ItemPrefabs;
}
