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

    /// <summary>Wall clock seconds between start and finish, pauses included.</summary>
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

    /// <summary>Stars shown on the results screen (0 to 3). 0 for rows saved before stars were stored.</summary>
    public int Stars { get; set; }

    /// <summary>Longest run of consecutive correct answers.</summary>
    public int BestStreak { get; set; }

    /// <summary>Seconds actually played, pauses excluded. 0 for rows saved before it was stored.</summary>
    public double ActiveSeconds { get; set; }

    /// <summary>How many times the match was paused.</summary>
    public int PauseCount { get; set; }

    /// <summary>Mistakes made while the tutorial was guiding. They are not part of <see cref="Errors"/>.</summary>
    public int TutorialErrors { get; set; }

    /// <summary>Mean response time of the attempts outside the tutorial, in seconds.</summary>
    public float AverageResponseSeconds { get; set; }

    /// <summary>Age of the child when the match was played, so progress can be read against age later. 0 without a profile.</summary>
    public int ChildAge { get; set; }

    /// <summary>Application version that recorded the match.</summary>
    public string AppVersion { get; set; }
}
