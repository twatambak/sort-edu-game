using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows one marker per allowed error and fills them as the child makes mistakes.
/// Mistakes made while the tutorial is guiding are not counted.
/// </summary>
public class ErrorCounter : MonoBehaviour
{
    [SerializeField] private Transform _errorCounterParent;
    [SerializeField] private GameObject _errorCounterPrefab;
    [SerializeField] private Color _errorColor;
    [SerializeField] private Color _emptyColor = Color.black;

    private GameController _gameController;

    private readonly List<GameObject> _errorCounters = new();
    private int _currentErrorIndex;

    private void Start()
    {
        _gameController = GameController.Instance;
        if (_gameController == null)
        {
            Debug.LogError("ErrorCounter could not find the GameController.", this);
            return;
        }

        _gameController.OnGameStarted += InitializeCounters;
        _gameController.OnItemSorted += HandleItemSorted;

        if (_gameController.IsGameStarted && _gameController.Config != null)
            InitializeCounters(_gameController.Config);
    }

    public void InitializeCounters(GameConfiguration config)
    {
        foreach (GameObject counter in _errorCounters)
            Destroy(counter);

        _errorCounters.Clear();
        _currentErrorIndex = 0;

        for (int i = 0; i < config.ErrorLimit; i++)
        {
            GameObject newCounter = Instantiate(_errorCounterPrefab, _errorCounterParent);
            if (newCounter.TryGetComponent(out Image img))
                img.color = _emptyColor;

            _errorCounters.Add(newCounter);
        }
    }

    private void HandleItemSorted(ItemSortResult result)
    {
        if (result.Correct || result.DuringTutorial || _currentErrorIndex >= _errorCounters.Count)
            return;

        GameObject counter = _errorCounters[_currentErrorIndex];
        _currentErrorIndex++;
        if (counter == null)
            return;

        if (counter.TryGetComponent(out Image img))
            img.color = _errorColor;

        Tweenimation.Pop(counter, initialScale: 0.5f, overshoot: 1.8f, duration: 0.3f);
    }

    private void OnDestroy()
    {
        if (_gameController == null)
            return;

        _gameController.OnGameStarted -= InitializeCounters;
        _gameController.OnItemSorted -= HandleItemSorted;
    }
}
