using System;
using OndeVai.Gameplay;
using OndeVai.Settings;

/// <summary>
/// Immutable-by-convention parameters of a single match.
/// </summary>
public class GameConfiguration
{
    public int MatchTime { get; set; }
    public int ErrorLimit { get; set; }
    public int MaxItemsOnScreen { get; set; }
    public int ItemsToWin { get; set; }
    public int DelayBetweenSpawns { get; set; }
    public ItemGroup ItemGroupA { get; set; }
    public ItemGroup ItemGroupB { get; set; }
    public ItemGroup ItemGroupC { get; set; }
    public ItemGroup ItemGroupD { get; set; }

    /// <summary>Sorting interaction used by the match.</summary>
    public GameMode Mode { get; set; } = GameMode.DragAndDrop;

    /// <summary>Id of the <see cref="SortingScenarioDefinition"/> the groups came from.</summary>
    public string ScenarioId { get; set; } = SortingScenarioDefinition.DefaultScenarioId;

    /// <summary>How many items at the start of the match are guided by the tutorial pointer.</summary>
    public int TutorialItemCount { get; set; }

    public GameConfiguration(int matchTime, int errorLimit, int maxItemsOnScreen, int itemsToWin, int delayBetweenSpawns, ItemGroup itemGroupA, ItemGroup itemGroupB, ItemGroup itemGroupC = ItemGroup.Toy, ItemGroup itemGroupD = ItemGroup.Tool)
    {
        MatchTime = matchTime;
        ErrorLimit = errorLimit;
        MaxItemsOnScreen = maxItemsOnScreen;
        ItemsToWin = itemsToWin;
        DelayBetweenSpawns = delayBetweenSpawns;
        ItemGroupA = itemGroupA;
        ItemGroupB = itemGroupB;
        ItemGroupC = itemGroupC;
        ItemGroupD = itemGroupD;
    }

    public static GameConfiguration ClassicMode => new GameConfiguration(30, 3, 5, 99, 5, ItemGroup.Toy, ItemGroup.Tool);

    /// <summary>Gets one configured category in scenario order.</summary>
    public ItemGroup GetGroup(int index)
    {
        return index switch
        {
            0 => ItemGroupA,
            1 => ItemGroupB,
            2 => ItemGroupC,
            3 => ItemGroupD,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    /// <summary>Number of categories required by the selected mode.</summary>
    public int GroupCount => Mode == GameMode.FourWaySwipe ? 4 : 2;

    /// <summary>
    /// Builds a configuration from the player's saved settings and the chosen scenario.
    /// </summary>
    public static GameConfiguration FromSettings(GameSettings settings, SortingScenarioDefinition scenario)
    {
        if (scenario == null)
            scenario = SortingScenarioDefinition.Default;
        return new GameConfiguration(
            settings.MatchTimeSeconds,
            settings.ErrorLimit,
            settings.ItemsOnScreen,
            settings.ItemsToWin,
            0,
            scenario.GroupA,
            scenario.GroupB,
            scenario.GroupC,
            scenario.GroupD)
        {
            Mode = settings.Mode,
            ScenarioId = scenario.Id,
            TutorialItemCount = settings.TutorialEnabled ? settings.TutorialItemCount : 0
        };
    }
}
