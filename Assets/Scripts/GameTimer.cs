using UnityEngine;
using UnityEngine.UI;

public class GameTimer : MonoBehaviour
{
    [SerializeField] private Slider _slider;

    private float _remainingTime;
    private GameController _gameController;

    private void Start()
    {
        _gameController = GameController.Instance;
        enabled = false;
    }

    public void BeginGame(GameConfiguration config)
    {
        if (_gameController == null || config == null || _slider == null)
            return;

        //_gameController.OnCorrectStorage -= AddTime;
        //_gameController.OnIncorrectStorage -= ReduceTime;
        _remainingTime = config.MatchTime;

        _slider.minValue = 0f;
        _slider.maxValue = _remainingTime;
        _slider.value = _remainingTime;

        //_gameController.OnCorrectStorage += AddTime;
        //_gameController.OnIncorrectStorage += ReduceTime;
    }

    public void ResetTimer()
    {
        enabled = false;
        if (_slider != null)
            _slider.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (_gameController == null || _gameController.IsGameOver || _remainingTime <= 0f)
            return;

        _remainingTime -= Time.deltaTime;
        _remainingTime = Mathf.Max(_remainingTime, 0f);

        _slider.value = _remainingTime;

        if (_remainingTime <= 0f)
            _gameController.HandleTimeExpired();
    }

    private void AddTime()
    {
        Tweenimation.Jelly(_slider.gameObject);
        _slider.maxValue = Mathf.Max(_slider.maxValue, _remainingTime);
        _slider.value = _remainingTime;
    }

    private void ReduceTime()
    {
        Tweenimation.Impact(_slider.gameObject);
        _remainingTime = Mathf.Max(_remainingTime, 0f);
        _slider.value = _remainingTime;

        if (_remainingTime <= 0f)
            _gameController.HandleTimeExpired();
    }

    private void OnDestroy()
    {
        if (_gameController == null)
            return;

        //_gameController.OnCorrectStorage -= AddTime;
        //_gameController.OnIncorrectStorage -= ReduceTime;
    }
}