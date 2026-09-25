using System;
using UnityEngine;

public class GameController : SingletonBase<GameController>
{
    [SerializeField] private GameConfiguration _config;
    [SerializeField] private Transform[] _spawnPoints;

    public Action OnCorrectStorage;
    public Action OnIncorrectStorage;
    public Action OnGameWon;
    public Action OnGameLost;

    public GameConfiguration Config => _config;
    public int ErrorCount { get; private set; }
    public int CorrectItemCount { get; private set; }
    public bool IsGameOver { get; private set; }

    private int _activeItemCount;
    private bool _gameStarted;
    private GameConfiguration _baseConfig;
    private GameStatusScreen _statusScreen;
    private GameTimer _gameTimer;

    private void Start()
    {
        _baseConfig = _config;
        _gameTimer = FindFirstObjectByType<GameTimer>();
        GameSetupScreen setupScreen = gameObject.AddComponent<GameSetupScreen>();
        setupScreen.Initialize(_config, BeginGame);
    }

    private void SpawnItems()
    {
        _activeItemCount = 0;

        for (int i = 0; i < _config.MaxItemsOnScreen; i++)
        {
            if (SpawnRandomItem())
                _activeItemCount++;
        }
    }

    private bool SpawnRandomItem()
    {
        if (_config.Items == null || _config.Items.Count == 0)
            return false;

        int randomIndex = UnityEngine.Random.Range(0, _config.Items.Count);
        ItemGroupDefinition itemPrefab = _config.Items[randomIndex];

        GameObject randomItemPrefab = itemPrefab.GetRandomGameObject();

        if (randomItemPrefab == null)
            return false;

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

        if (_config.ItemsToWin > 0 && CorrectItemCount >= _config.ItemsToWin)
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

        if (_config.Mode == GameMode.ErrorLimit && _config.ErrorLimit > 0 && ErrorCount >= _config.ErrorLimit)
            EndGame(false);
    }

    public void HandleTimeExpired()
    {
        if (IsGameOver || _config.Mode != GameMode.Timed)
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

    private void BeginGame(GameConfiguration selectedConfig)
    {
        if (_gameStarted || selectedConfig == null)
            return;

        _config = selectedConfig;
        _gameStarted = true;
        IsGameOver = false;
        ErrorCount = 0;
        CorrectItemCount = 0;

        if (_gameTimer != null)
            _gameTimer.BeginGame(_config);

        if (_statusScreen == null)
        {
            _statusScreen = gameObject.AddComponent<GameStatusScreen>();
            _statusScreen.Initialize(this);
        }
        else
        {
            _statusScreen.ShowGameView();
        }

        SpawnItems();
    }

    public void ResetGame()
    {
        RemoveActiveItems();

        if (_config != _baseConfig && _config != null)
            Destroy(_config);

        _config = _baseConfig;
        _gameStarted = false;
        IsGameOver = false;
        ErrorCount = 0;
        CorrectItemCount = 0;

        _gameTimer?.ResetTimer();
        _statusScreen?.Hide();

        GameSetupScreen setupScreen = gameObject.AddComponent<GameSetupScreen>();
        setupScreen.Initialize(_baseConfig, BeginGame);
    }

    public bool IsGameStarted => _gameStarted;

    private void RemoveActiveItems()
    {
        Item[] activeItems = FindObjectsByType<Item>(FindObjectsSortMode.None);
        foreach (Item item in activeItems)
            Destroy(item.gameObject);
    }
}