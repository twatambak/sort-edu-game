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
    /// <summary>Selected storage, or null when the answer is a swipe-selected category.</summary>
    public readonly SortingSource Storage;
    public readonly bool Correct;
    public readonly bool DuringTutorial;
    public readonly Vector3 Position;

    public ItemSortResult(Item item, SortingSource storage, bool correct, bool duringTutorial, Vector3 position)
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
    [SerializeField] private SortingSource _sortingSourceA;
    [Tooltip("Optional. Receives group B of the selected scenario.")]
    [SerializeField] private SortingSource _sortingSourceB;

    [Header("Swipe Mode")]
    [SerializeField] private Transform _questionPoint;

    public Action<GameConfiguration> OnGameStarted { get; set; }
    public Action OnCorrectSort { get; set; }
    public Action OnIncorrectSort { get; set; }
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
    private readonly List<SortingSource> _knownStorages = new();

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

        if (selectedConfig.Mode != GameMode.DragAndDrop && selectedConfig.Mode != GameMode.SwipeTwoDirections)
            throw new ArgumentOutOfRangeException(nameof(selectedConfig), selectedConfig.Mode, "Unsupported game mode.");

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

    public bool SubmitItem(Item item, SortingSource storage)
    {
        if (storage == null)
            return false;

        return SubmitItem(item, storage.GroupTarget, storage);
    }

    public bool SubmitItem(Item item, ItemGroup chosenGroup)
    {
        return SubmitItem(item, chosenGroup, null);
    }

    private bool SubmitItem(Item item, ItemGroup chosenGroup, SortingSource storage)
    {
        if (item == null || !CanAcceptInput || item.IsResolving)
            return false;

        bool correct = item.ItemType == chosenGroup;
        bool duringTutorial = TutorialItemsRemaining > 0;
        Vector3 position = item.transform.position;
        RecordAttempt(item, chosenGroup, correct, duringTutorial);

        if (correct)
            ResolveCorrect(item, storage, duringTutorial, position);
        else
            ResolveIncorrect(item, storage, duringTutorial, position);

        return correct;
    }

    public void SelectSortingSource(SortingSource storage)
    {
        if (CanAcceptInput)
            _runner?.HandleStorageSelected(storage);
    }

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
    public bool TryGetStorageFor(ItemGroup group, out SortingSource storage)
    {
        if (_sortingSourceA != null && _sortingSourceA.GroupTarget == group)
        {
            storage = _sortingSourceA;
            return true;
        }

        if (_sortingSourceB != null && _sortingSourceB.GroupTarget == group)
        {
            storage = _sortingSourceB;
            return true;
        }

        foreach (SortingSource candidate in SortingSource.Active)
        {
            if (candidate.GroupTarget == group)
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
        OnCorrectSort?.Invoke();
        CheckWinCondition();
    }

    /// <summary>Legacy entry point kept for existing callers. Prefer <see cref="SubmitItem"/>.</summary>
    public void HandleIncorrectStorage()
    {
        if (!CanAcceptInput)
            return;

        ErrorCount++;
        OnIncorrectSort?.Invoke();
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

    private void ResolveCorrect(Item item, SortingSource storage, bool duringTutorial, Vector3 position)
    {
        _spawner.Remove(item);
        if (storage != null)
        {
            storage.AcceptItem(item);
        }
        else
        {
            item.BeginResolve();
            item.SetKinematic(true);
        }
        ScoreCount++;

        bool tutorialFinished = false;
        if (duringTutorial)
        {
            TutorialItemsRemaining--;
            tutorialFinished = TutorialItemsRemaining == 0;
        }

        OnCorrectSort?.Invoke();
        OnItemSorted?.Invoke(new ItemSortResult(item, storage, true, duringTutorial, position));

        if (tutorialFinished)
            OnTutorialCompleted?.Invoke();

        if (CheckWinCondition())
            return;

        _runner.HandleItemResolved(item, true);
    }

    private void ResolveIncorrect(Item item, SortingSource storage, bool duringTutorial, Vector3 position)
    {
        if (storage != null)
            storage.RejectItem(item, _runner.EjectOnError);
        if (!duringTutorial)
            ErrorCount++;

        OnIncorrectSort?.Invoke();
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

    private void RecordAttempt(Item item, ItemGroup chosenGroup, bool correct, bool duringTutorial)
    {
        _attempts.Add(new ItemAttemptRecord
        {
            ItemGroup = (int)item.ItemType,
            ChosenGroup = (int)chosenGroup,
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
        foreach (SortingSource storage in SortingSource.Active)
        {
            if (storage != null && !_knownStorages.Contains(storage))
                _knownStorages.Add(storage);
        }

        RegisterStorage(_sortingSourceA);
        RegisterStorage(_sortingSourceB);

        if (_sortingSourceA == null && _sortingSourceB == null)
            return;

        SortingScenarioDefinition scenario = _gameDefinition != null
            ? _gameDefinition.GetScenarioOrDefault(Config.ScenarioId)
            : SortingScenarioDefinition.Default;

        bool scenarioMatches = scenario.IsPlayable &&
            scenario.GroupA == Config.ItemGroupA && scenario.GroupB == Config.ItemGroupB;
        if (_sortingSourceA != null)
            _sortingSourceA.Configure(Config, scenario.TypeA);

        if (_sortingSourceB != null)
            _sortingSourceB.Configure(Config, scenario.TypeB);

        void RegisterStorage(SortingSource storage)
        {
            if (storage != null && !_knownStorages.Contains(storage))
                _knownStorages.Add(storage);
        }
    }

    private IGameModeRunner CreateRunner(GameMode mode)
    {
        GameModeContext context = new()
        {
            Spawner = _spawner,
            QuestionPoint = _questionPoint,
            NotifySpawned = item => OnItemSpawned?.Invoke(item),
            Submit = (item, storage) => SubmitItem(item, storage),
            SubmitGroup = (item, group) => SubmitItem(item, group),
            NotifyPicked = NotifyItemPicked,
            NotifyReleased = NotifyItemReleased,
            CanAcceptInput = () => CanAcceptInput
        };

        return mode switch
        {
            GameMode.DragAndDrop => new DragAndDropModeRunner(context),
            GameMode.SwipeTwoDirections => new SwipeCardModeRunner(context),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported game mode.")
        };
    }

    private void StopCurrentMatch()
    {
        _runner?.End();
        _runner = null;
        _spawner?.Clear();
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
