using System;
using UnityEngine;

public class StorageGameController : SingletonBase<StorageGameController>
{
    [SerializeField] private StorageGameConfig _config;

    public Action OnCorrectStorage;
    public Action OnIncorrectStorage;

    public void HandleCorrectStorage()
    {
        OnCorrectStorage?.Invoke();
    }

    public void HandleIncorrectStorage()
    {
        OnIncorrectStorage?.Invoke();
    }
}
