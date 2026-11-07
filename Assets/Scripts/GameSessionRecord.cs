using SQLite;

/// <summary>
/// One finished (or abandoned) match. Columns added later are migrated automatically by
/// SQLite-net's CreateTable and default to 0/null for older rows.
/// </summary>
public sealed class GameSessionRecord
{
    public const string ReasonItemsCollected = "ItemsCollected";
    public const string ReasonErrorLimit = "ErrorLimitReached";
    public const string ReasonTimeExpired = "TimeExpired";
    public const string ReasonAbandoned = "Abandoned";

    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public long StartedAtUtcTicks { get; set; }
    public long FinishedAtUtcTicks { get; set; }
    public double DurationSeconds { get; set; }
    public int MatchTimeSeconds { get; set; }
    public int ErrorLimit { get; set; }
    public int MaxItemsOnScreen { get; set; }
    public int ItemsToWin { get; set; }
    public int DelayBetweenSpawnsSeconds { get; set; }
    public int ItemGroupA { get; set; }
    public int ItemGroupB { get; set; }
    public int Score { get; set; }
    public int Errors { get; set; }
    public bool Won { get; set; }
    public string FinishReason { get; set; }

    /// <summary>Id of the <see cref="OndeVai.Data.ChildProfileRecord"/> that played. 0 when no child was selected.</summary>
    [Indexed]
    public int ChildProfileId { get; set; }

    /// <summary><see cref="OndeVai.Gameplay.GameMode"/> stored as an integer.</summary>
    public int GameMode { get; set; }

    public string ScenarioId { get; set; }

    public int TutorialItemCount { get; set; }
}
