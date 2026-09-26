using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameStatusScreen : MonoBehaviour
{
    [SerializeField] private GameObject _hudRoot;
    [SerializeField] private GameObject _resultRoot;
    [SerializeField] private GameObject _errorCounterRoot;
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _errorText;
    [SerializeField] private TMP_Text _resultTitle;
    [SerializeField] private TMP_Text _resultScore;
    [SerializeField] private TMP_Text _resultErrors;
    [SerializeField] private Button _resetButton;

    private GameController _controller;

    public void Initialize(GameController controller)
    {
        _controller = controller;
        _resetButton.onClick.AddListener(_controller.ResetGame);
        _controller.OnCorrectStorage += RefreshHud;
        _controller.OnIncorrectStorage += RefreshHud;
        _controller.OnGameWon += ShowWin;
        _controller.OnGameLost += ShowLoss;
        ShowGameView();
    }

    private void RefreshHud()
    {
        if (_controller == null || _scoreText == null)
            return;

        /*
        _scoreText.text = $"PLACAR  {_controller.CorrectItemCount} / {_controller.Config.ItemsToWin}";
        bool errorMode = _controller.Config.Mode == GameMode.ErrorLimit;
        _errorCounterRoot.SetActive(errorMode);
        _errorText.text = errorMode
            ? $"ERROS  {_controller.ErrorCount} / {_controller.Config.ErrorLimit}   RESTANTES  {Mathf.Max(0, _controller.Config.ErrorLimit - _controller.ErrorCount)}"
            : string.Empty;
            */
    }

    private void ShowWin()
    {
        ShowResult("VITORIA", new Color(0.35f, 0.9f, 0.55f, 1f));
    }

    private void ShowLoss()
    {
        ShowResult("DERROTA", new Color(1f, 0.4f, 0.35f, 1f));
    }

    private void ShowResult(string title, Color titleColor)
    {
        RefreshHud();
        _hudRoot.SetActive(false);
        _resultRoot.SetActive(true);
        _resultTitle.text = title;
        _resultTitle.color = titleColor;
        _resultScore.text = $"Pontuacao  {_controller.CorrectItemCount} / {_controller.Config.ItemsToWin}";
        _resultErrors.text = $"Erros  {_controller.ErrorCount}";
    }

    public void ShowGameView()
    {
        if (_hudRoot == null)
            return;

        _hudRoot.SetActive(true);
        _resultRoot.SetActive(false);
        RefreshHud();
    }

    public void Hide()
    {
        _hudRoot?.SetActive(false);
        _resultRoot?.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_controller == null)
            return;

        _controller.OnCorrectStorage -= RefreshHud;
        _controller.OnIncorrectStorage -= RefreshHud;
        _controller.OnGameWon -= ShowWin;
        _controller.OnGameLost -= ShowLoss;
        _resetButton.onClick.RemoveListener(_controller.ResetGame);
    }
}