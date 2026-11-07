using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// End of match screen. Shows the result, score, errors and up to three stars based on accuracy.
/// </summary>
public class GameStatusScreen : MonoBehaviour
{
    [SerializeField] private GameObject _screenRoot;
    [SerializeField] private TMP_Text _resultTitle;
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _errorText;

    [Tooltip("Optional star images, filled from left to right according to accuracy.")]
    [SerializeField] private Image[] _stars;
    [SerializeField] private Color _starOnColor = new(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color _starOffColor = new(1f, 1f, 1f, 0.25f);

    private GameController _gameController;

    /// <summary>Raised when the player asks to go back to the title screen.</summary>
    public event Action HomeRequested;

    private void Start()
    {
        _gameController = GameController.Instance;
        if (_gameController == null)
        {
            Debug.LogError("GameStatusScreen could not find the GameController.", this);
            return;
        }

        _gameController.OnGameWon += ShowWin;
        _gameController.OnGameLost += ShowLoss;
        _gameController.OnGameStarted += HandleGameStarted;
        _screenRoot.SetActive(false);
    }

    public void PlayAgain()
    {
        Hide();
        GameController.Instance.ResetGame();
    }

    /// <summary>Hides the screen and asks the flow to return to the title screen.</summary>
    public void GoHome()
    {
        Hide();
        HomeRequested?.Invoke();
    }

    public void Hide()
    {
        if (_screenRoot != null)
            _screenRoot.SetActive(false);
    }

    private void HandleGameStarted(GameConfiguration config)
    {
        Hide();
    }

    private void ShowWin()
    {
        ShowResult("PARABÉNS!", new Color(0.35f, 0.9f, 0.55f, 1f));
    }

    private void ShowLoss()
    {
        ShowResult("FIM DE JOGO", new Color(1f, 0.4f, 0.35f, 1f));
    }

    private void ShowResult(string title, Color titleColor)
    {
        _screenRoot.SetActive(true);
        Tweenimation.Pop(_screenRoot);
        _resultTitle.text = title;
        _resultTitle.color = titleColor;
        _scoreText.text = _gameController.ScoreCount.ToString();
        _errorText.text = _gameController.ErrorCount.ToString();
        ShowStars(CalculateStars(_gameController.ScoreCount, _gameController.ErrorCount));
    }

    /// <summary>Three stars for no mistakes, fewer as the error ratio grows. At least one star when anything was sorted.</summary>
    public static int CalculateStars(int score, int errors)
    {
        if (score <= 0)
            return 0;

        float accuracy = (float)score / (score + errors);
        if (accuracy >= 0.9f)
            return 3;

        return accuracy >= 0.6f ? 2 : 1;
    }

    private void ShowStars(int count)
    {
        if (_stars == null)
            return;

        for (int i = 0; i < _stars.Length; i++)
        {
            Image star = _stars[i];
            if (star == null)
                continue;

            bool on = i < count;
            star.color = on ? _starOnColor : _starOffColor;
            star.transform.DOKill();
            star.transform.localScale = Vector3.one;
            if (on)
            {
                star.transform.DOPunchScale(Vector3.one * 0.5f, 0.4f, 6)
                    .SetDelay(0.25f + i * 0.2f)
                    .SetLink(star.gameObject);
            }
        }
    }

    private void OnDestroy()
    {
        if (_gameController == null)
            return;

        _gameController.OnGameWon -= ShowWin;
        _gameController.OnGameLost -= ShowLoss;
        _gameController.OnGameStarted -= HandleGameStarted;
    }
}
