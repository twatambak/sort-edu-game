using UnityEngine;
using UnityEngine.UI;

public class GameStatusScreen : MonoBehaviour
{
    private GameController _controller;
    private GameObject _hudRoot;
    private GameObject _resultRoot;
    private Text _scoreText;
    private Text _errorText;
    private Text _resultTitle;
    private Text _resultScore;
    private Text _resultErrors;

    public void Initialize(GameController controller)
    {
        _controller = controller;
        _controller.OnCorrectStorage += RefreshHud;
        _controller.OnIncorrectStorage += RefreshHud;
        _controller.OnGameWon += ShowWin;
        _controller.OnGameLost += ShowLoss;
        BuildScreen();
        ShowGameView();
    }

    private void BuildScreen()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        _hudRoot = CreateUiObject("GameHud", canvas.transform);
        RectTransform hudRect = _hudRoot.GetComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(0f, 1f);
        hudRect.anchorMax = new Vector2(1f, 1f);
        hudRect.pivot = new Vector2(0.5f, 1f);
        hudRect.offsetMin = new Vector2(36f, -150f);
        hudRect.offsetMax = new Vector2(-36f, -36f);

        HorizontalLayoutGroup hudLayout = _hudRoot.AddComponent<HorizontalLayoutGroup>();
        hudLayout.spacing = 24f;
        hudLayout.childControlWidth = true;
        hudLayout.childControlHeight = true;
        hudLayout.childForceExpandWidth = true;
        _scoreText = AddText(_hudRoot.transform, string.Empty, 28, TextAnchor.MiddleLeft);
        _errorText = AddText(_hudRoot.transform, string.Empty, 28, TextAnchor.MiddleRight);

        _resultRoot = CreateUiObject("GameResult", canvas.transform);
        Stretch(_resultRoot.GetComponent<RectTransform>());
        _resultRoot.AddComponent<Image>().color = new Color(0.03f, 0.07f, 0.1f, 0.96f);

        GameObject resultPanel = CreateUiObject("ResultPanel", _resultRoot.transform);
        RectTransform resultPanelRect = resultPanel.GetComponent<RectTransform>();
        resultPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        resultPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        resultPanelRect.sizeDelta = new Vector2(760f, 650f);
        resultPanel.AddComponent<Image>().color = new Color(0.08f, 0.15f, 0.2f, 1f);
        VerticalLayoutGroup resultLayout = resultPanel.AddComponent<VerticalLayoutGroup>();
        resultLayout.padding = new RectOffset(48, 48, 48, 48);
        resultLayout.spacing = 18f;
        resultLayout.childControlWidth = true;
        resultLayout.childControlHeight = false;
        resultLayout.childForceExpandWidth = true;

        _resultTitle = AddText(resultPanel.transform, string.Empty, 48, TextAnchor.MiddleCenter);
        _resultScore = AddText(resultPanel.transform, string.Empty, 28, TextAnchor.MiddleCenter);
        _resultErrors = AddText(resultPanel.transform, string.Empty, 24, TextAnchor.MiddleCenter);
        AddButton(resultPanel.transform, "JOGAR NOVAMENTE", _controller.ResetGame);
    }

    private void RefreshHud()
    {
        if (_controller == null || _scoreText == null)
            return;

        _scoreText.text = $"PLACAR  {_controller.CorrectItemCount} / {_controller.Config.ItemsToWin}";
        bool errorMode = _controller.Config.Mode == GameMode.ErrorLimit;
        _errorText.text = errorMode
            ? $"ERROS  {_controller.ErrorCount} / {_controller.Config.ErrorLimit}   RESTANTES  {Mathf.Max(0, _controller.Config.ErrorLimit - _controller.ErrorCount)}"
            : string.Empty;
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
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static Text AddText(Transform parent, string content, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = CreateUiObject(content, parent);
        Text text = textObject.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        LayoutElement layout = textObject.AddComponent<LayoutElement>();
        layout.minHeight = 64f;
        return text;
    }

    private static Button AddButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = CreateUiObject(label, parent);
        buttonObject.AddComponent<Image>().color = new Color(0.2f, 0.65f, 0.7f, 1f);
        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(action);
        Text text = AddText(buttonObject.transform, label, 24, TextAnchor.MiddleCenter);
        text.color = Color.white;
        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.minHeight = 86f;
        return button;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}