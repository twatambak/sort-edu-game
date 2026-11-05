using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameController : SingletonBase<GameController>
{
    [SerializeField] private Transform[] _spawnPoints;
/*     [SerializeField] private Canvas _uiCanvas;
    [SerializeField] private GameSetupScreen _setupScreenPrefab;
    [SerializeField] private GameStatusScreen _statusScreenPrefab; */
    [SerializeField] private GameDefinition _gameDefinition;

    public Action<GameConfiguration> OnGameStarted { get; set; }
    public Action OnCorrectStorage { get; set; }
    public Action OnIncorrectStorage { get; set; }
    public Action OnGameWon{ get; set; }
    public Action OnGameLost{ get; set; }

    public GameConfiguration Config { get; private set; }
    public int ErrorCount { get; private set; }
    public int ScoreCount { get; private set; }
    public bool IsGameOver { get; private set; }

    private int _activeItemCount;
    private bool _gameStarted;
    private DateTime _sessionStartedAtUtc;
    private GameHistoryDatabase _historyDatabase;

    private GameStatusScreen _statusScreen;

    private void Start()
    {
        try
        {
            _historyDatabase = new GameHistoryDatabase();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }

        Config = GameConfiguration.ClassicMode;
        BeginGame(Config);
    }

    private void SpawnItems()
    {
        _activeItemCount = 0;
        List<Transform> availableSpawnPoints = new();
        if (_spawnPoints != null)
        {
            foreach (Transform spawnPoint in _spawnPoints)
            {
                if (spawnPoint != null)
                    availableSpawnPoints.Add(spawnPoint);
            }
        }

        List<Collider2D> activeItemColliders = new();
        Item[] activeItems = FindObjectsByType<Item>(FindObjectsSortMode.None);
        foreach (Item item in activeItems)
            activeItemColliders.AddRange(item.GetComponentsInChildren<Collider2D>());

        for (int i = 0; i < Config.MaxItemsOnScreen; i++)
        {
            Vector3 spawnPosition = GetRandomSpawnPosition(availableSpawnPoints);
            if (SpawnRandomItem(spawnPosition, activeItemColliders))
                _activeItemCount++;
        }
    }

    private bool SpawnRandomItem(Vector3 spawnPosition, List<Collider2D> activeItemColliders)
    {
        ItemGroup randomGroup = UnityEngine.Random.value < 0.5f ? Config.ItemGroupA : Config.ItemGroupB;
        GameObject randomItemPrefab = _gameDefinition.GetRandomGameObject(randomGroup);
        GameObject newItem = Instantiate(randomItemPrefab, spawnPosition, Quaternion.identity);

        Collider2D[] newItemColliders = newItem.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D activeCollider in activeItemColliders)
        {
            if (activeCollider == null)
                continue;

            foreach (Collider2D newCollider in newItemColliders)
                Physics2D.IgnoreCollision(activeCollider, newCollider);
        }

        activeItemColliders.AddRange(newItemColliders);
        Tweenimation.Pop(newItem, initialScale: 0.3f, duration: 0.3f);
        return true;
    }

    private Vector3 GetRandomSpawnPosition(List<Transform> availableSpawnPoints)
    {
        if (availableSpawnPoints.Count == 0 && _spawnPoints != null)
        {
            foreach (Transform spawnPoint in _spawnPoints)
            {
                if (spawnPoint != null)
                    availableSpawnPoints.Add(spawnPoint);
            }
        }

        if (availableSpawnPoints.Count == 0)
            return Vector3.zero;

        int randomIndex = UnityEngine.Random.Range(0, availableSpawnPoints.Count);
        Transform selectedSpawnPoint = availableSpawnPoints[randomIndex];
        availableSpawnPoints.RemoveAt(randomIndex);
        return selectedSpawnPoint.position;
    }

    public void HandleCorrectStorage()
    {
        if (IsGameOver)
            return;

        _activeItemCount--;
        ScoreCount++;

        OnCorrectStorage?.Invoke();

        if (Config.ItemsToWin > 0 && ScoreCount >= Config.ItemsToWin)
        {
            EndGame(true, "ItemsCollected");
            return;
        }

        if (_activeItemCount <= 0)
            SpawnItems();
    }

    public void HandleIncorrectStorage()
    {
        if (IsGameOver)
            return;

        ErrorCount++;

        OnIncorrectStorage?.Invoke();

        if (ErrorCount >= Config.ErrorLimit)
            EndGame(false, "ErrorLimitReached");
    }

    public void HandleTimeExpired()
    {
        if (IsGameOver)
            return;

        EndGame(true, "TimeExpired");
    }

    private void EndGame(bool won, string finishReason)
    {
        if (IsGameOver)
            return;

        IsGameOver = true;

        if (_historyDatabase == null)
        {
            Debug.LogError("Game history was not saved because the SQLite database is unavailable.", this);
        }
        else
        {
            DateTime finishedAtUtc = DateTime.UtcNow;
            try
            {
                _historyDatabase.SaveCompletedGame(new GameSessionRecord
                {
                    StartedAtUtcTicks = _sessionStartedAtUtc.Ticks,
                    FinishedAtUtcTicks = finishedAtUtc.Ticks,
                    DurationSeconds = Math.Max(0, (finishedAtUtc - _sessionStartedAtUtc).TotalSeconds),
                    MatchTimeSeconds = Config.MatchTime,
                    ErrorLimit = Config.ErrorLimit,
                    MaxItemsOnScreen = Config.MaxItemsOnScreen,
                    ItemsToWin = Config.ItemsToWin,
                    DelayBetweenSpawnsSeconds = Config.DelayBetweenSpawns,
                    ItemGroupA = (int)Config.ItemGroupA,
                    ItemGroupB = (int)Config.ItemGroupB,
                    Score = ScoreCount,
                    Errors = ErrorCount,
                    Won = won,
                    FinishReason = finishReason
                });
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        if (won)
            OnGameWon?.Invoke();
        else
            OnGameLost?.Invoke();
    }

    public void BeginGame(GameConfiguration selectedConfig)
    {
        Config = selectedConfig;
        _gameStarted = true;
        IsGameOver = false;
        ErrorCount = 0;
        ScoreCount = 0;
        _sessionStartedAtUtc = DateTime.UtcNow;

        SpawnItems();

        OnGameStarted?.Invoke(Config);

    }

    public void ResetGame()
    {
        RemoveActiveItems();

        Config = GameConfiguration.ClassicMode;
        _gameStarted = false;
        IsGameOver = false;
        ErrorCount = 0;
        ScoreCount = 0;

        BeginGame(Config);
    }

    public bool IsGameStarted => _gameStarted;

    public List<GameSessionRecord> GetRecentGameSessions(int limit = 50)
    {
        return GetHistoryDatabase().GetRecentSessions(limit);
    }

    public GameSessionRecord GetGameSessionById(int id)
    {
        return GetHistoryDatabase().GetSessionById(id);
    }

    public int GetCompletedGameCount()
    {
        return GetHistoryDatabase().GetCompletedSessionCount();
    }

    private GameHistoryDatabase GetHistoryDatabase()
    {
        if (_historyDatabase == null)
            throw new InvalidOperationException("The SQLite game history database is unavailable.");

        return _historyDatabase;
    }

    private void RemoveActiveItems()
    {
        Item[] activeItems = FindObjectsByType<Item>(FindObjectsSortMode.None);
        foreach (Item item in activeItems)
            Destroy(item.gameObject);
    }

    protected override void OnDestroy()
    {
        _historyDatabase?.Dispose();
        base.OnDestroy();
    }
}