using System;
using UnityEngine;

public class StorageGameController : SingletonBase<StorageGameController>
{
    [SerializeField] private StorageGameConfig _config;
    [SerializeField] private Transform[] _spawnPoints;

    public Action OnCorrectStorage;
    public Action OnIncorrectStorage;

    public int ErrorCount { get; private set; }

    private int _activeItemCount;

    private void Start()
    {
        SpawnItems();
    }

    private void SpawnItems()
    {
        _activeItemCount = 0;

        for (int i = 0; i < _config.MaxItems; i++)
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
        ItemPrefabs itemPrefab = _config.Items[randomIndex];

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
        _activeItemCount--;

        OnCorrectStorage?.Invoke();

        if (_activeItemCount <= 0)
            SpawnItems();
    }

    public void HandleIncorrectStorage()
    {
        ErrorCount++;

        OnIncorrectStorage?.Invoke();
    }
}