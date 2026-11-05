using SQLite;

public sealed class GameSessionRecord
{
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
}
