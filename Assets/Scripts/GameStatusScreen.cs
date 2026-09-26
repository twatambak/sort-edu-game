using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameStatusScreen : MonoBehaviour
{
    [SerializeField] private GameObject _screenRoot;
    [SerializeField] private TMP_Text _resultTitle;
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _errorText;

    private GameController _gameController;

    private void Start()
    {
        _gameController = GameController.Instance;
        _gameController.OnGameWon += ShowWin;
        _gameController.OnGameLost += ShowLoss;
        _screenRoot.SetActive(false);
    }

    public void PlayAgain()
    {
        Hide();
        GameController.Instance.ResetGame();
    }

    private void ShowWin()
    {
        ShowResult("PARABÉNS!", new Color(0.35f, 0.9f, 0.55f, 1f));
    }

    private void ShowLoss()
    {
        ShowResult("MUITOS ERROS", new Color(1f, 0.4f, 0.35f, 1f));
    }

    private void ShowResult(string title, Color titleColor)
    {
        _screenRoot.SetActive(true);
        Tweenimation.Pop(_screenRoot);
        _resultTitle.text = title;
        _resultTitle.color = titleColor;
        _scoreText.text = _gameController.ScoreCount.ToString();
    }

    public void Hide()
    {
        _screenRoot?.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_gameController == null)
            return;

        _gameController.OnGameWon -= ShowWin;
        _gameController.OnGameLost -= ShowLoss;
    }
}