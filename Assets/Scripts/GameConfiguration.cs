using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using NaughtyAttributes;

public class GameConfiguration
{
    public int MatchTime {get; set;}
    public int ErrorLimit {get; set;}
    public int MaxItemsOnScreen {get; set;}
    public int ItemsToWin {get; set;}
    public int DelayBetweenSpawns {get; set;}
    public ItemGroup ItemGroupA {get; set;}
    public ItemGroup ItemGroupB {get; set;}

    public GameConfiguration(int matchTime, int errorLimit, int maxItemsOnScreen, int itemsToWin, int delayBetweenSpawns, ItemGroup itemGroupA, ItemGroup itemGroupB)
    {
        MatchTime = matchTime;
        ErrorLimit = errorLimit;
        MaxItemsOnScreen = maxItemsOnScreen;
        ItemsToWin = itemsToWin;
        DelayBetweenSpawns = delayBetweenSpawns;
        ItemGroupA = itemGroupA;
        ItemGroupB = itemGroupB;
    }

    public static GameConfiguration ClassicMode => new GameConfiguration(30, 3, 5, 99, 5, ItemGroup.Toy, ItemGroup.Tool);
}
