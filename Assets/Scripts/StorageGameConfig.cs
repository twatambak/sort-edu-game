using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using NaughtyAttributes;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Scriptable Objects/GameConfig")]
public class GameConfiguration : ScriptableObject
{
    public GameMode Mode;
    
    [ShowIf("IsTimedMode")] public int MatchTime;
    [ShowIf("IsTimedMode")] public int IncreaseOnRight;
    [ShowIf("IsTimedMode")] public int DecreaseOnWrong;

    [ShowIf("IsErrorLimitMode")] public int ErrorLimit;

    [FormerlySerializedAs("MaxItems")] public int MaxItemsOnScreen;
    public int ItemsToWin = 10;
    public int DelayBetweenItemSpawn;

    public List<ItemGroupDefinition> Items;

    private bool IsTimedMode => Mode == GameMode.Timed;
    private bool IsErrorLimitMode => Mode == GameMode.ErrorLimit;
}

public enum GroupType
{
    Toy,
    Food,
    Tool,
    Animal
}

public enum GameMode
{
    Timed,
    ErrorLimit
}

[Serializable]
public struct ItemGroupDefinition
{
    public GroupType ItemType;  
    [FormerlySerializedAs("Prefab")] 
    public GameObject[] Prefabs;
    public Storage StoragePrefab;

    public GameObject GetRandomGameObject()
    {
        if (Prefabs == null || Prefabs.Length == 0)
            return null;
        int randomIndex = UnityEngine.Random.Range(0, Prefabs.Length);
        return Prefabs[randomIndex];
    }
}