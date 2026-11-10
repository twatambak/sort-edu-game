using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Countdown slider of the match. Holds still while paused or while the tutorial is guiding,
/// shifts its color as time runs out and pulses during the final seconds.
/// </summary>
public class GameTimer : MonoBehaviour
{
    private static readonly Color HalfTimeColor = new(1f, 0.8f, 0.3f, 1f);

    [SerializeField] private Slider _sliderTimer;
    [Tooltip("Seconds left when the low time warning starts.")]
    [SerializeField, Min(0f)] private float _warningThreshold = 5f;
    [Tooltip("Fill of the slider. The slider's own fill graphic is used when empty.")]
    [SerializeField] private Image _fillImage;
    [SerializeField] private Color _warningColor = new(1f, 0.4f, 0.35f, 1f);

    private float _remainingTime;
    private float _totalTime;
    private GameController _gameController;
    private Color _fillRestColor;
    private Tween _warningTween;
    private int _lastReportedSecond;

    /// <summary>Raised once for each elapsed whole second, with the remaining seconds.</summary>
    public event Action<int> SecondElapsed;

    private void Start()
    {
        _gameController = GameController.Instance;
        if (_gameController == null)
        {
            Debug.LogError("GameTimer could not find the GameController.", this);
            enabled = false;
            return;
        }

        if (_sliderTimer == null)
        {
            Debug.LogError("GameTimer has no slider assigned.", this);
            enabled = false;
            return;
        }

        if (_fillImage == null && _sliderTimer.fillRect != null)
            _fillImage = _sliderTimer.fillRect.GetComponent<Image>();

        if (_fillImage != null)
            _fillRestColor = _fillImage.color;

        _gameController.OnGameStarted += InitializeCountdown;
        _gameController.OnGameEnded += HandleGameEnded;
        _gameController.OnGameAborted += StopWarning;
        if (_gameController.IsGameStarted && _gameController.Config != null)
            InitializeCountdown(_gameController.Config);
    }

    private void Update()
    {
        if (!_gameController.IsTimerRunning || _remainingTime <= 0f)
            return;

        _remainingTime = Mathf.Max(_remainingTime - Time.deltaTime, 0f);
        _sliderTimer.value = _remainingTime;
        ApplyFillColor();

        int secondsRemaining = Mathf.CeilToInt(_remainingTime);
        if (secondsRemaining < _lastReportedSecond)
        {
            _lastReportedSecond = secondsRemaining;
            SecondElapsed?.Invoke(secondsRemaining);
        }

        if (_warningTween == null && _remainingTime <= _warningThreshold)
            StartWarning();

        if (_remainingTime <= 0f)
        {
            StopWarning();
            _gameController.HandleTimeExpired();
        }
    }

    public void InitializeCountdown(GameConfiguration config)
    {
        StopWarning();
        _totalTime = config.MatchTime;
        _remainingTime = _totalTime;
        _lastReportedSecond = Mathf.CeilToInt(_remainingTime);

        _sliderTimer.minValue = 0f;
        _sliderTimer.maxValue = _remainingTime;
        _sliderTimer.value = _remainingTime;
        ApplyFillColor();
    }

    /// <summary>Rest color while there is plenty of time, then through yellow to the warning color.</summary>
    private void ApplyFillColor()
    {
        if (_fillImage == null || _totalTime <= 0f)
            return;

        float remaining = Mathf.Clamp01(_remainingTime / _totalTime);
        _fillImage.color = remaining > 0.5f
            ? Color.Lerp(HalfTimeColor, _fillRestColor, (remaining - 0.5f) * 2f)
            : Color.Lerp(_warningColor, HalfTimeColor, remaining * 2f);
    }

    private void HandleGameEnded(GameSessionRecord record)
    {
        StopWarning();
    }

    private void StartWarning()
    {
        Transform target = _sliderTimer.transform;
        _warningTween = target.DOScale(target.localScale * 1.06f, 0.25f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetLink(gameObject);
    }

    private void StopWarning()
    {
        if (_warningTween == null)
            return;

        _warningTween.Rewind();
        _warningTween.Kill();
        _warningTween = null;
    }

    private void OnDestroy()
    {
        if (_gameController == null)
            return;

        _gameController.OnGameStarted -= InitializeCountdown;
        _gameController.OnGameEnded -= HandleGameEnded;
        _gameController.OnGameAborted -= StopWarning;
    }
}
