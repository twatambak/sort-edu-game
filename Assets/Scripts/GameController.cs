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

    private GameStatusScreen _statusScreen;

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
        GameObject newItem = Instantiate(randomItemPrefab, GetRandomSpawnPosition(), Quaternion.identity);
        Tweenimation.Pop(newItem, initialScale: 0.3f, duration: 0.3f);
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
        ScoreCount++;

        OnCorrectStorage?.Invoke();

        if (Config.ItemsToWin > 0 && ScoreCount >= Config.ItemsToWin)
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

        EndGame(true);
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
        ScoreCount = 0;

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

    private void RemoveActiveItems()
    {
        Item[] activeItems = FindObjectsByType<Item>(FindObjectsSortMode.None);
        foreach (Item item in activeItems)
            Destroy(item.gameObject);
    }
}