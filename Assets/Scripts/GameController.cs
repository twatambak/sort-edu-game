using System;
using UnityEngine;
using UnityEngine.UI;

public class GameController : SingletonBase<GameController>
{
    [SerializeField] private Transform[] _spawnPoints;
/*     [SerializeField] private Canvas _uiCanvas;
    [SerializeField] private GameSetupScreen _setupScreenPrefab;
    [SerializeField] private GameStatusScreen _statusScreenPrefab; */
    [SerializeField] private GameDefinition _gameDefinition;

    public Action OnCorrectStorage;
    public Action OnIncorrectStorage;
    public Action OnGameWon;
    public Action OnGameLost;

    public GameConfiguration Config { get; private set; }
    public int ErrorCount { get; private set; }
    public int CorrectItemCount { get; private set; }
    public bool IsGameOver { get; private set; }

    private int _activeItemCount;
    private bool _gameStarted;

    private GameStatusScreen _statusScreen;
    private GameTimer _gameTimer;

    private void Start()
    {
        Config = GameConfiguration.ClassicMode;
        BeginGame(Config);
    }

    private void SpawnItems()
    {
        _activeItemCount = 0;

        for (int i = 0; i < Config.MaxItemsOnScreen; i++)
        {
            if (SpawnRandomItem())
                _activeItemCount++;
        }
    }

    private bool SpawnRandomItem()
    {
        ItemGroup randomGroup = UnityEngine.Random.value < 0.5f ? Config.ItemGroupA : Config.ItemGroupB;
        GameObject randomItemPrefab = _gameDefinition.GetRandomGameObject(randomGroup);
        Instantiate(randomItemPrefab, GetRandomSpawnPosition(), Quaternion.identity);
        return true;
    }

    private Vector3 GetRandomSpawnPosition()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
            return Vector3.zero;

        int randomIndex = UnityEngine.Random.Range(0, _spawnPoints.Length);
        return _spawnPoints[randomIndex].position;
    }

    public void HandleCorrectStorage()
    {
        if (IsGameOver)
            return;

        _activeItemCount--;
        CorrectItemCount++;

        OnCorrectStorage?.Invoke();

        if (Config.ItemsToWin > 0 && CorrectItemCount >= Config.ItemsToWin)
        {
            EndGame(true);
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
            EndGame(false);
    }

    public void HandleTimeExpired()
    {
        if (IsGameOver)
            return;

        EndGame(false);
    }

    private void EndGame(bool won)
    {
        IsGameOver = true;

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
        CorrectItemCount = 0;

        if (_gameTimer != null)
            _gameTimer.BeginGame(Config);

/*         if (_statusScreen == null)
        {
            _statusScreen = Instantiate(_statusScreenPrefab, _uiCanvas.transform);
            _statusScreen.Initialize(this);
        }
        else
        {
            _statusScreen.ShowGameView();
        } */

        SpawnItems();
    }

    public void ResetGame()
    {
        RemoveActiveItems();

        Config = GameConfiguration.ClassicMode;
        _gameStarted = false;
        IsGameOver = false;
        ErrorCount = 0;
        CorrectItemCount = 0;

        _gameTimer?.ResetTimer();
        _statusScreen?.Hide();
    }

    public bool IsGameStarted => _gameStarted;

    private void RemoveActiveItems()
    {
        Item[] activeItems = FindObjectsByType<Item>(FindObjectsSortMode.None);
        foreach (Item item in activeItems)
            Destroy(item.gameObject);
    }
}