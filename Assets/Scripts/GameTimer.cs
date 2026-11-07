using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Countdown slider of the match. Holds still while paused or while the tutorial is guiding,
/// and pulses during the final seconds.
/// </summary>
public class GameTimer : MonoBehaviour
{
    [SerializeField] private Slider _sliderTimer;
    [Tooltip("Seconds left when the low time warning starts.")]
    [SerializeField, Min(0f)] private float _warningThreshold = 5f;
    [SerializeField] private Image _fillImage;
    [SerializeField] private Color _warningColor = new(1f, 0.4f, 0.35f, 1f);

    private float _remainingTime;
    private GameController _gameController;
    private Color _fillRestColor;
    private Tween _warningTween;

    private void Start()
    {
        _gameController = GameController.Instance;
        if (_gameController == null)
        {
            Debug.LogError("GameTimer could not find the GameController.", this);
            enabled = false;
            return;
        }

        if (_fillImage != null)
            _fillRestColor = _fillImage.color;

        _gameController.OnGameStarted += InitializeCountdown;
        if (_gameController.IsGameStarted && _gameController.Config != null)
            InitializeCountdown(_gameController.Config);
    }

    private void Update()
    {
        if (_gameController == null || !_gameController.IsTimerRunning || _remainingTime <= 0f)
            return;

        _remainingTime = Mathf.Max(_remainingTime - Time.deltaTime, 0f);
        _sliderTimer.value = _remainingTime;

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
        _remainingTime = config.MatchTime;

        _sliderTimer.minValue = 0f;
        _sliderTimer.maxValue = _remainingTime;
        _sliderTimer.value = _remainingTime;
    }

    private void StartWarning()
    {
        Transform target = _sliderTimer.transform;
        _warningTween = target.DOScale(target.localScale * 1.06f, 0.25f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetLink(gameObject);

        if (_fillImage != null)
            _fillImage.color = _warningColor;
    }

    private void StopWarning()
    {
        if (_warningTween != null)
        {
            _warningTween.Rewind();
            _warningTween.Kill();
            _warningTween = null;
        }

        if (_fillImage != null)
            _fillImage.color = _fillRestColor;
    }

    private void OnDestroy()
    {
        if (_gameController != null)
            _gameController.OnGameStarted -= InitializeCountdown;
    }
}
