using System;
using System.Collections.Generic;
using OndeVai.Data;
using OndeVai.Gameplay;
using OndeVai.Platform;
using OndeVai.Settings;
using UnityEngine;

/// <summary>
/// Owns the match: score, errors, tutorial progress, pause and end conditions.
/// Mode-specific rules live in <see cref="IGameModeRunner"/> implementations and everything that is
/// stored about a match is collected by <see cref="MatchSessionRecorder"/>.
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

    /// <summary>Raised when the child lets go of an item, before any answer is evaluated.</summary>
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

    /// <summary>Longest run of consecutive correct answers in the current match.</summary>
    public int BestStreak => _recorder.BestStreak;

    /// <summary>The item the tutorial should guide, or null.</summary>
    public Item FocusItem => _runner?.FocusItem;

    /// <summary>Items currently in play.</summary>
    public IReadOnlyList<Item> ActiveItems => _spawner != null ? _spawner.ActiveItems : Array.Empty<Item>();

    private readonly MatchSessionRecorder _recorder = new();
    private bool _gameStarted;
    private bool _ownsServices;
    private bool _hasTutorialPresenter;
    private int _checkpointSessionId;
    private ItemSpawner _spawner;
    private IGameModeRunner _runner;

    private bool IsMatchRunning => _gameStarted && !IsGameOver;

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
        BeginGame(GameConfiguration.FromSettings(settings, ResolveScenario(settings.ScenarioId)));
    }

    public void BeginGame(GameConfiguration selectedConfig)
    {
        if (selectedConfig == null)
            throw new ArgumentNullException(nameof(selectedConfig));

        if (!GameModes.IsSupported(selectedConfig.Mode))
            throw new ArgumentOutOfRangeException(nameof(selectedConfig), selectedConfig.Mode, "Unsupported game mode.");

        // Restarting in the middle of a match must not lose what the child already did.
        SaveAbandonedMatch();
        StopCurrentMatch();

        Config = selectedConfig;
        _gameStarted = true;
        IsGameOver = false;
        LastGameWon = false;
        ErrorCount = 0;
        ScoreCount = 0;
        TutorialItemsRemaining = _hasTutorialPresenter ? Mathf.Max(0, selectedConfig.TutorialItemCount) : 0;
        _checkpointSessionId = 0;
        _recorder.Begin(selectedConfig, DateTime.UtcNow, Time.time);
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
        SaveAbandonedMatch();
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
        if (paused && !IsMatchRunning)
            return;

        SetPausedInternal(paused);
    }

    /// <summary>Evaluates an item dropped on a storage. Returns true when it belongs there.</summary>
    public bool SubmitItem(Item item, SortingSource storage)
    {
        return storage != null && Submit(item, storage.GroupTarget, storage);
    }

    /// <summary>Evaluates an answer given without a physical drop (swipe). Returns true when correct.</summary>
    public bool SubmitItem(Item item, ItemGroup chosenGroup)
    {
        return Submit(item, chosenGroup, null);
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

    /// <summary>Called by the input components when an item is picked up.</summary>
    public void NotifyItemPicked(Item item)
    {
        if (item == null)
            return;

        item.RegisterPickup();
        OnItemPicked?.Invoke(item);
    }

    /// <summary>Called by the input components when a held item is released.</summary>
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

    public void HandleTimeExpired()
    {
        if (IsMatchRunning)
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

    private bool Submit(Item item, ItemGroup chosenGroup, SortingSource storage)
    {
        if (item == null || !CanAcceptInput || item.IsResolving)
            return false;

        bool correct = item.ItemType == chosenGroup;
        bool duringTutorial = TutorialItemsRemaining > 0;
        Vector3 position = item.transform.position;
        RecordAttempt(item, chosenGroup, correct, duringTutorial);

        // A swipe has no storage to animate, so the panel of the chosen side reacts instead.
        if (storage == null && TryGetStorageFor(chosenGroup, out SortingSource panel))
            panel.Presentation.FlashResult(correct);

        ItemSortResult result = new(item, storage, correct, duringTutorial, position, _recorder.CurrentStreak);
        if (correct)
            ResolveCorrect(result);
        else
            ResolveIncorrect(result);

        return correct;
    }

    private void ResolveCorrect(in ItemSortResult result)
    {
        Item item = result.Item;
        _spawner.Remove(item);
        if (result.Storage != null)
        {
            result.Storage.AcceptItem(item);
        }
        else
        {
            item.BeginResolve();
            item.SetKinematic(true);
        }

        ScoreCount++;

        bool tutorialFinished = false;
        if (result.DuringTutorial)
        {
            TutorialItemsRemaining--;
            tutorialFinished = TutorialItemsRemaining == 0;
        }

        OnCorrectSort?.Invoke();
        OnItemSorted?.Invoke(result);

        if (tutorialFinished)
            OnTutorialCompleted?.Invoke();

        if (CheckWinCondition())
            return;

        _runner.HandleItemResolved(item, true);
    }

    private void ResolveIncorrect(in ItemSortResult result)
    {
        if (result.Storage != null)
            result.Storage.RejectItem(result.Item, _runner.EjectOnError);

        if (!result.DuringTutorial)
            ErrorCount++;

        OnIncorrectSort?.Invoke();
        OnItemSorted?.Invoke(result);

        if (!result.DuringTutorial && CheckLossCondition())
            return;

        _runner.HandleItemResolved(result.Item, false);
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
        GameSessionRecord record = SaveSession(won, finishReason, false);

        if (won)
            OnGameWon?.Invoke();
        else
            OnGameLost?.Invoke();

        OnGameEnded?.Invoke(record);
    }

    private void RecordAttempt(Item item, ItemGroup chosenGroup, bool correct, bool duringTutorial)
    {
        SortAttempt attempt = new()
        {
            ItemGroup = item.ItemType,
            ChosenGroup = chosenGroup,
            ItemName = item.name,
            Correct = correct,
            DuringTutorial = duringTutorial,
            ResponseSeconds = Time.time - item.AvailableSince,
            RetryIndex = item.AttemptCount,
            PickupCount = item.PickupCount
        };

        _recorder.RecordAttempt(attempt, DateTime.UtcNow, Time.time);
        item.RegisterAttempt();
        if (!correct)
            item.RestartResponseTimer();
    }

    /// <summary>
    /// Stores the match in its current state. A checkpoint is replaced by the next save of the same
    /// match, so the history never holds the same match twice.
    /// </summary>
    private GameSessionRecord SaveSession(bool won, string finishReason, bool isCheckpoint)
    {
        GameSessionRecord record = _recorder.BuildRecord(
            won, finishReason, ScoreCount, ErrorCount, DateTime.UtcNow, Time.time, Application.version);

        int savedId = Data.SaveSession(record, _recorder.Attempts, _checkpointSessionId, isCheckpoint);
        if (savedId <= 0)
            return null;

        _checkpointSessionId = isCheckpoint ? savedId : 0;
        return record;
    }

    private void SaveAbandonedMatch()
    {
        if (IsMatchRunning && _recorder.HasAttempts)
            SaveSession(false, GameSessionRecord.ReasonAbandoned, false);
    }

    /// <summary>
    /// Mobile systems may kill a backgrounded app without notice, so the running match is stored as
    /// abandoned when the app goes to the background. It is replaced if the match continues.
    /// </summary>
    private void SaveCheckpoint()
    {
        if (IsMatchRunning && _recorder.HasAttempts)
            SaveSession(false, GameSessionRecord.ReasonAbandoned, true);
    }

    private SortingScenarioDefinition ResolveScenario(string scenarioId)
    {
        return _gameDefinition != null
            ? _gameDefinition.GetScenarioOrDefault(scenarioId)
            : SortingScenarioDefinition.Default;
    }

    private void ConfigureStorages()
    {
        if (_sortingSourceA == null && _sortingSourceB == null)
            return;

        SortingScenarioDefinition scenario = ResolveScenario(Config.ScenarioId);
        if (scenario.GroupA != Config.ItemGroupA || scenario.GroupB != Config.ItemGroupB)
            Debug.LogError($"Scenario '{scenario.Id}' does not match the groups of the match configuration. Storages may show the wrong category.", this);

        if (_sortingSourceA != null)
            _sortingSourceA.Configure(Config, scenario.TypeA);

        if (_sortingSourceB != null)
            _sortingSourceB.Configure(Config, scenario.TypeB);
    }

    private IGameModeRunner CreateRunner(GameMode mode)
    {
        GameModeContext context = new()
        {
            Spawner = _spawner,
            QuestionPoint = _questionPoint,
            NotifySpawned = item => OnItemSpawned?.Invoke(item),
            Submit = SubmitItem,
            SubmitGroup = SubmitItem,
            NotifyPicked = NotifyItemPicked,
            NotifyReleased = NotifyItemReleased,
            CanAcceptInput = () => CanAcceptInput,
            FindStorage = group => TryGetStorageFor(group, out SortingSource storage) ? storage : null
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
        if (paused)
            _recorder.NotifyPaused();

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
        if (!pauseStatus)
            return;

        Settings?.Flush();
        SaveCheckpoint();
        if (_autoPauseOnFocusLoss)
            SetPaused(true);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && _autoPauseOnFocusLoss)
            SetPaused(true);
    }

    protected override void OnApplicationQuit()
    {
        if (_ownsServices)
            SaveAbandonedMatch();

        base.OnApplicationQuit();
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
