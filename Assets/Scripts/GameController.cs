using System;
using System.Collections.Generic;
using OndeVai.Data;
using OndeVai.Gameplay;
using OndeVai.Platform;
using OndeVai.Settings;
using UnityEngine;

/// <summary>Outcome of one sorting attempt, broadcast by <see cref="GameController.OnItemSorted"/>.</summary>
public readonly struct ItemSortResult
{
    public readonly Item Item;
    public readonly Storage Storage;
    public readonly bool Correct;
    public readonly bool DuringTutorial;
    public readonly Vector3 Position;

    public ItemSortResult(Item item, Storage storage, bool correct, bool duringTutorial, Vector3 position)
    {
        Item = item;
        Storage = storage;
        Correct = correct;
        DuringTutorial = duringTutorial;
        Position = position;
    }
}

/// <summary>
/// Owns the match: score, errors, tutorial progress, pause, end conditions and persistence.
/// Mode-specific rules live in <see cref="IGameModeRunner"/> implementations.
/// </summary>
public class GameController : SingletonBase<GameController>
{
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private GameDefinition _gameDefinition;

    [Header("Flow")]
    [Tooltip("Starts a match as soon as the scene loads. Disable when a title screen drives the flow.")]
    [SerializeField] private bool _beginOnStart = true;
    [Tooltip("Pauses the match when the app loses focus. Only enable when a pause screen exists to resume it.")]
    [SerializeField] private bool _autoPauseOnFocusLoss;

    [Header("Storages")]
    [Tooltip("Optional. Receives group A of the selected scenario. When empty, storages keep their authored group.")]
    [SerializeField] private Storage _storageA;
    [Tooltip("Optional. Receives group B of the selected scenario.")]
    [SerializeField] private Storage _storageB;

    [Header("Question Mode")]
    [SerializeField] private Transform _questionPoint;
    [SerializeField, Min(0.1f)] private float _questionItemScale = 1.6f;

    public Action<GameConfiguration> OnGameStarted { get; set; }
    public Action OnCorrectStorage { get; set; }
    public Action OnIncorrectStorage { get; set; }
    public Action OnGameWon { get; set; }
    public Action OnGameLost { get; set; }

    /// <summary>Raised whenever a new item appears.</summary>
    public Action<Item> OnItemSpawned { get; set; }

    /// <summary>Raised when the child picks up an item.</summary>
    public Action<Item> OnItemPicked { get; set; }

    /// <summary>Raised when the child lets go of a dragged item, before any drop is evaluated.</summary>
    public Action<Item> OnItemReleased { get; set; }

    /// <summary>Raised after every sorting attempt.</summary>
    public Action<ItemSortResult> OnItemSorted { get; set; }

    /// <summary>Raised when the match is paused (true) or resumed (false).</summary>
    public Action<bool> OnPauseChanged { get; set; }

    /// <summary>Raised when the last tutorial item has been sorted.</summary>
    public Action OnTutorialCompleted { get; set; }

    /// <summary>Raised when the match is left before it finished.</summary>
    public Action OnGameAborted { get; set; }

    /// <summary>Raised after a finished match has been saved. The record is null if saving failed.</summary>
    public Action<GameSessionRecord> OnGameEnded { get; set; }

    public GameConfiguration Config { get; private set; }
    public int ErrorCount { get; private set; }
    public int ScoreCount { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsPaused { get; private set; }
    public bool LastGameWon { get; private set; }
    public int TutorialItemsRemaining { get; private set; }

    public GameSettingsService Settings { get; private set; }
    public GameDataService Data { get; private set; }
    public GameDefinition Definition => _gameDefinition;

    public bool IsGameStarted => _gameStarted;
    public bool IsTutorialActive => _gameStarted && !IsGameOver && TutorialItemsRemaining > 0;
    public bool IsTimerRunning => _gameStarted && !IsGameOver && !IsPaused && !IsTutorialActive;
    public bool CanAcceptInput => _gameStarted && !IsGameOver && !IsPaused;

    /// <summary>The item the tutorial should guide, or null.</summary>
    public Item FocusItem => _runner?.FocusItem;

    /// <summary>Items currently in play.</summary>
    public IReadOnlyList<Item> ActiveItems => _spawner != null ? _spawner.ActiveItems : Array.Empty<Item>();

    private bool _gameStarted;
    private bool _ownsServices;
    private bool _hasTutorialPresenter;
    private DateTime _sessionStartedAtUtc;
    private ItemSpawner _spawner;
    private IGameModeRunner _runner;
    private readonly List<ItemAttemptRecord> _attempts = new();

    protected override void Awake()
    {
        base.Awake();
        if (GetInstance() != this)
            return;

        _ownsServices = true;
        Settings = new GameSettingsService();
        Data = new GameDataService(OpenDatabase());
        _spawner = new ItemSpawner(_gameDefinition, _spawnPoints, new GameObject("Items").transform);
    }

    private void Start()
    {
        if (_beginOnStart)
            BeginGameFromSettings();
    }

    /// <summary>Starts a match using the saved settings.</summary>
    public void BeginGameFromSettings()
    {
        GameSettings settings = Settings.Current;
        SortingScenarioDefinition scenario = _gameDefinition != null
            ? _gameDefinition.GetScenarioOrDefault(settings.ScenarioId)
            : SortingScenarioDefinition.Default;

        BeginGame(GameConfiguration.FromSettings(settings, scenario));
    }

    public void BeginGame(GameConfiguration selectedConfig)
    {
        if (selectedConfig == null)
            throw new ArgumentNullException(nameof(selectedConfig));

        StopCurrentMatch();

        Config = selectedConfig;
        _gameStarted = true;
        IsGameOver = false;
        LastGameWon = false;
        ErrorCount = 0;
        ScoreCount = 0;
        TutorialItemsRemaining = _hasTutorialPresenter ? Mathf.Max(0, selectedConfig.TutorialItemCount) : 0;
        _attempts.Clear();
        _sessionStartedAtUtc = DateTime.UtcNow;
        SetPausedInternal(false);

        ConfigureStorages();
        _runner = CreateRunner(selectedConfig.Mode);
        _runner.Begin(selectedConfig);

        MobilePlatformRuntime.SetKeepAwake(true);
        OnGameStarted?.Invoke(Config);
    }

    /// <summary>Restarts with the same configuration as the last match.</summary>
    public void ResetGame()
    {
        if (Config == null)
        {
            BeginGameFromSettings();
            return;
        }

        BeginGame(Config);
    }

    /// <summary>Leaves the current match. Partial progress is saved when the child made any attempt.</summary>
    public void AbortGame()
    {
        if (!_gameStarted)
            return;

        bool wasRunning = !IsGameOver;
        if (wasRunning && _attempts.Count > 0)
            SaveSession(false, GameSessionRecord.ReasonAbandoned);

        StopCurrentMatch();
        _gameStarted = false;
        IsGameOver = true;
        SetPausedInternal(false);
        MobilePlatformRuntime.SetKeepAwake(false);

        if (wasRunning)
            OnGameAborted?.Invoke();
    }

    public void SetPaused(bool paused)
    {
        if (paused && (!_gameStarted || IsGameOver))
            return;

        SetPausedInternal(paused);
    }

    /// <summary>
    /// Evaluates an item placed in (or chosen for) a storage. Plays storage feedback and updates the match.
    /// </summary>
    /// <returns>True when the item belongs to the storage.</returns>
    public bool SubmitItem(Item item, Storage storage)
    {
        if (item == null || storage == null || !CanAcceptInput || item.IsResolving)
            return false;

        bool correct = item.ItemType == storage.StorageType;
        bool duringTutorial = TutorialItemsRemaining > 0;
        Vector3 position = item.transform.position;
        RecordAttempt(item, storage, correct, duringTutorial);

        if (correct)
            ResolveCorrect(item, storage, duringTutorial, position);
        else
            ResolveIncorrect(item, storage, duringTutorial, position);

        return correct;
    }

    /// <summary>Forwards a storage tap to the current game mode.</summary>
    public void SelectStorage(Storage storage)
    {
        if (CanAcceptInput)
            _runner?.HandleStorageSelected(storage);
    }

    /// <summary>
    /// Called by the tutorial view when it is ready. Tutorial rules (paused timer, forgiven errors)
    /// only apply when something is actually showing the tutorial to the child.
    /// </summary>
    public void RegisterTutorialPresenter()
    {
        _hasTutorialPresenter = true;
    }

    /// <summary>Called by <see cref="DragAndDrop"/> when an item is picked up.</summary>
    public void NotifyItemPicked(Item item)
    {
        if (item != null)
            OnItemPicked?.Invoke(item);
    }

    /// <summary>Called by <see cref="DragAndDrop"/> when a dragged item is released.</summary>
    public void NotifyItemReleased(Item item)
    {
        if (item != null)
            OnItemReleased?.Invoke(item);
    }

    /// <summary>Finds the storage that accepts <paramref name="group"/>.</summary>
    public bool TryGetStorageFor(ItemGroup group, out Storage storage)
    {
        if (_storageA != null && _storageA.StorageType == group)
        {
            storage = _storageA;
            return true;
        }

        if (_storageB != null && _storageB.StorageType == group)
        {
            storage = _storageB;
            return true;
        }

        foreach (Storage candidate in Storage.Active)
        {
            if (candidate.StorageType == group)
            {
                storage = candidate;
                return true;
            }
        }

        storage = null;
        return false;
    }

    /// <summary>Legacy entry point kept for existing callers. Prefer <see cref="SubmitItem"/>.</summary>
    public void HandleCorrectStorage()
    {
        if (!CanAcceptInput)
            return;

        ScoreCount++;
        OnCorrectStorage?.Invoke();
        CheckWinCondition();
    }

    /// <summary>Legacy entry point kept for existing callers. Prefer <see cref="SubmitItem"/>.</summary>
    public void HandleIncorrectStorage()
    {
        if (!CanAcceptInput)
            return;

        ErrorCount++;
        OnIncorrectStorage?.Invoke();
        CheckLossCondition();
    }

    public void HandleTimeExpired()
    {
        if (IsGameOver || !_gameStarted)
            return;

        EndGame(true, GameSessionRecord.ReasonTimeExpired);
    }

    public List<GameSessionRecord> GetRecentGameSessions(int limit = 50)
    {
        return Data.Database.GetRecentSessions(limit);
    }

    public GameSessionRecord GetGameSessionById(int id)
    {
        return Data.Database.GetSessionById(id);
    }

    public int GetCompletedGameCount()
    {
        return Data.Database.GetCompletedSessionCount();
    }

    private void ResolveCorrect(Item item, Storage storage, bool duringTutorial, Vector3 position)
    {
        _spawner.Remove(item);
        storage.AcceptItem(item);
        ScoreCount++;

        bool tutorialFinished = false;
        if (duringTutorial)
        {
            TutorialItemsRemaining--;
            tutorialFinished = TutorialItemsRemaining == 0;
        }

        OnCorrectStorage?.Invoke();
        OnItemSorted?.Invoke(new ItemSortResult(item, storage, true, duringTutorial, position));

        if (tutorialFinished)
            OnTutorialCompleted?.Invoke();

        if (CheckWinCondition())
            return;

        _runner.HandleItemResolved(item, true);
    }

    private void ResolveIncorrect(Item item, Storage storage, bool duringTutorial, Vector3 position)
    {
        storage.RejectItem(item, _runner.EjectOnError);
        if (!duringTutorial)
            ErrorCount++;

        OnIncorrectStorage?.Invoke();
        OnItemSorted?.Invoke(new ItemSortResult(item, storage, false, duringTutorial, position));

        if (!duringTutorial && CheckLossCondition())
            return;

        _runner.HandleItemResolved(item, false);
    }

    private bool CheckWinCondition()
    {
        if (Config.ItemsToWin <= 0 || ScoreCount < Config.ItemsToWin)
            return false;

        EndGame(true, GameSessionRecord.ReasonItemsCollected);
        return true;
    }

    private bool CheckLossCondition()
    {
        if (ErrorCount < Config.ErrorLimit)
            return false;

        EndGame(false, GameSessionRecord.ReasonErrorLimit);
        return true;
    }

    private void EndGame(bool won, string finishReason)
    {
        if (IsGameOver)
            return;

        IsGameOver = true;
        LastGameWon = won;
        MobilePlatformRuntime.SetKeepAwake(false);
        _runner?.End();
        GameSessionRecord record = SaveSession(won, finishReason);

        if (won)
            OnGameWon?.Invoke();
        else
            OnGameLost?.Invoke();

        OnGameEnded?.Invoke(record);
    }

    private GameSessionRecord SaveSession(bool won, string finishReason)
    {
        DateTime finishedAtUtc = DateTime.UtcNow;
        GameSessionRecord record = new()
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
            FinishReason = finishReason,
            GameMode = (int)Config.Mode,
            ScenarioId = Config.ScenarioId,
            TutorialItemCount = Config.TutorialItemCount
        };

        return Data.SaveSession(record, _attempts) > 0 ? record : null;
    }

    private void RecordAttempt(Item item, Storage storage, bool correct, bool duringTutorial)
    {
        _attempts.Add(new ItemAttemptRecord
        {
            ItemGroup = (int)item.ItemType,
            ChosenGroup = (int)storage.StorageType,
            ItemName = item.name,
            Correct = correct,
            ResponseSeconds = Mathf.Max(0f, Time.time - item.AvailableSince),
            DuringTutorial = duringTutorial
        });

        if (!correct)
            item.RestartResponseTimer();
    }

    private void ConfigureStorages()
    {
        if (_storageA == null && _storageB == null)
            return;

        SortingScenarioDefinition scenario = _gameDefinition != null
            ? _gameDefinition.GetScenarioOrDefault(Config.ScenarioId)
            : SortingScenarioDefinition.Default;

        bool scenarioMatches = scenario.GroupA == Config.ItemGroupA && scenario.GroupB == Config.ItemGroupB;
        if (_storageA != null)
            _storageA.Configure(Config.ItemGroupA, scenarioMatches ? scenario.IconA : null);

        if (_storageB != null)
            _storageB.Configure(Config.ItemGroupB, scenarioMatches ? scenario.IconB : null);
    }

    private IGameModeRunner CreateRunner(GameMode mode)
    {
        GameModeContext context = new()
        {
            Spawner = _spawner,
            QuestionPoint = _questionPoint,
            QuestionItemScale = _questionItemScale,
            NotifySpawned = item => OnItemSpawned?.Invoke(item),
            Submit = SubmitItem,
            CanAcceptInput = () => CanAcceptInput
        };

        return mode switch
        {
            GameMode.Question => new QuestionModeRunner(context),
            _ => new DragAndDropModeRunner(context)
        };
    }

    private void StopCurrentMatch()
    {
        _runner?.End();
        _runner = null;
        _spawner?.Clear();

        foreach (Storage storage in Storage.Active)
            storage.SetHighlighted(false);
    }

    private void SetPausedInternal(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;
        if (IsPaused == paused)
            return;

        IsPaused = paused;
        OnPauseChanged?.Invoke(paused);
    }

    private GameHistoryDatabase OpenDatabase()
    {
        try
        {
            return new GameHistoryDatabase();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            return null;
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // Mobile apps can be killed while in the background without further callbacks.
        if (pauseStatus)
            Settings?.Flush();

        if (pauseStatus && _autoPauseOnFocusLoss)
            SetPaused(true);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && _autoPauseOnFocusLoss)
            SetPaused(true);
    }

    protected override void OnDestroy()
    {
        if (_ownsServices)
        {
            _ownsServices = false;
            Time.timeScale = 1f;
            Data?.Dispose();
        }

        base.OnDestroy();
    }
}
