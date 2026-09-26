using UnityEngine;
using UnityEngine.UI;

public class GameTimer : MonoBehaviour
{
    [SerializeField] private Slider _sliderTimer;

    private float _remainingTime;
    private GameController _gameController;

    private void Start()
    {
        _gameController = GameController.Instance;
        _gameController.OnGameStarted += InitializeCountdown;
    }

    private void Update()
    {
        if (_gameController == null || _gameController.IsGameOver || _remainingTime <= 0f)
            return;

        _remainingTime -= Time.deltaTime;
        _remainingTime = Mathf.Max(_remainingTime, 0f);
        _sliderTimer.value = _remainingTime;

        if (_remainingTime <= 0f)
            _gameController.HandleTimeExpired();
    }

    public void InitializeCountdown(GameConfiguration config)
    {
        _remainingTime = config.MatchTime;

        _sliderTimer.minValue = 0f;
        _sliderTimer.maxValue = _remainingTime;
        _sliderTimer.value = _remainingTime;

/*         _gameController.OnCorrectStorage += HandleCorrectStorage;
        _gameController.OnIncorrectStorage += HandleIncorrectStorage; */
    }

    private void HandleCorrectStorage()
    {
        _sliderTimer.maxValue = Mathf.Max(_sliderTimer.maxValue, _remainingTime);
        _sliderTimer.value = _remainingTime;
    }

    private void HandleIncorrectStorage()
    {
        Tweenimation.Impact(_sliderTimer.gameObject);
        _remainingTime = Mathf.Max(_remainingTime, 0f);
        _sliderTimer.value = _remainingTime;

        if (_remainingTime <= 0f)
            _gameController.HandleTimeExpired();
    }

    private void OnDestroy()
    {
        if (_gameController == null)
            return;
            
        _gameController.OnGameStarted -= InitializeCountdown;

        //_gameController.OnCorrectStorage -= AddTime;
        //_gameController.OnIncorrectStorage -= ReduceTime;
    }
}