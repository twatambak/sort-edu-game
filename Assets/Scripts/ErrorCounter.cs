using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ErrorCounter : MonoBehaviour
{
    [SerializeField] private Transform _errorCounterParent;
    [SerializeField] private GameObject _errorCounterPrefab;
    [SerializeField] private Color _errorColor;

    private GameController _gameController;

    private List<GameObject> _errorCounters = new();
    private int _currentErrorIndex;

    private void Start()
    {
        _gameController = GameController.Instance;
        _gameController.OnGameStarted += InitializeCounters;
        _gameController.OnCorrectStorage += HandleCorrectStorage;
        _gameController.OnIncorrectStorage += HandleIncorrectStorage;
    }

    public void InitializeCounters(GameConfiguration config)
    {
        foreach (GameObject gb in _errorCounters)
            Destroy(gb);

        _errorCounters.Clear();

        _currentErrorIndex = 0;
        for (int i = 0; i < config.ErrorLimit; i++)
        {
            GameObject newCounter = Instantiate(_errorCounterPrefab, _errorCounterParent);
            if(newCounter.TryGetComponent(out Image img))
            {
                img.color = Color.black;
                _errorCounters.Add(newCounter);
            }
        }
    }

    private void HandleCorrectStorage()
    {
    }

    private void HandleIncorrectStorage()
    {
        GameObject counter = _errorCounters[_currentErrorIndex];
        if(counter == null)
            return;

        if (counter.TryGetComponent(out Image img))
            img.color = _errorColor;

        Tweenimation.Pop(counter, initialScale: 0.5f, overshoot: 1.8f, duration: 0.3f);
        _currentErrorIndex++;

    }

    private void OnDestroy()
    {
        if (_gameController == null)
            return;

        _gameController.OnCorrectStorage -= HandleCorrectStorage;
        _gameController.OnIncorrectStorage -= HandleIncorrectStorage;
    }
}