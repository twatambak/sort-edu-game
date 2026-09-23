using System;
using UnityEngine;

public class StorageGameController : SingletonBase<StorageGameController>
{
    [SerializeField] private StorageGameConfig _config;
    [SerializeField] private Transform[] _spawnPoints;

    public Action OnCorrectStorage;
    public Action OnIncorrectStorage;

    private void Start()
    {
        for (int i = 0; i < _config.MaxItems; i++)
        {
            SpawnRandomItem();
        }
    }

    private void SpawnRandomItem()
    {
        if (_config.Items == null || _config.Items.Count == 0)
            return;
        int randomIndex = UnityEngine.Random.Range(0, _config.Items.Count);
        ItemPrefabs itemPrefab = _config.Items[randomIndex];
        GameObject randomItemPrefab = itemPrefab.GetRandomGameObject();
        if (randomItemPrefab != null)
        {
            Instantiate(randomItemPrefab, GetRandomSpawnPosition(), Quaternion.identity);
        }
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
        OnCorrectStorage?.Invoke();
    }

    public void HandleIncorrectStorage()
    {
        OnIncorrectStorage?.Invoke();
    }
}
