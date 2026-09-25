using System;
using UnityEngine;
using UnityEngine.UI;

public class GameSetupScreen : MonoBehaviour
{
    private const int PanelWidth = 860;

    private GameConfiguration _baseConfig;
    private Action<GameConfiguration> _onStart;
    private GameObject _screenRoot;
    private Button _timedModeButton;
    private Button _errorLimitModeButton;
    private InputField _itemsToWinInput;
    private InputField _matchTimeInput;
    private InputField _errorLimitInput;
    private InputField _maxItemsInput;
    private Text _modeHelp;
    private GameMode _selectedMode;

    public void Initialize(GameConfiguration baseConfig, Action<GameConfiguration> onStart)
    {
        _baseConfig = baseConfig;
        _onStart = onStart;
        BuildScreen();
    }

    private void BuildScreen()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        _screenRoot = CreateUiObject("GameSetupScreen", canvas.transform);
        RectTransform rootRect = _screenRoot.GetComponent<RectTransform>();
        Stretch(rootRect);

        Image background = _screenRoot.AddComponent<Image>();
        background.color = new Color(0.04f, 0.08f, 0.12f, 0.96f);

        GameObject panel = CreateUiObject("SetupPanel", _screenRoot.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(PanelWidth, 1320f);
        panel.AddComponent<Image>().color = new Color(0.08f, 0.15f, 0.2f, 1f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(64, 64, 52, 52);
        layout.spacing = 18f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;

        AddText(panel.transform, "CONFIGURE SUA PARTIDA", 42, TextAnchor.MiddleCenter, 76f);
        AddText(panel.transform, "Escolha um modelo rapido ou ajuste as regras", 22, TextAnchor.MiddleCenter, 44f);

        AddText(panel.transform, "PARTIDAS RAPIDAS", 20, TextAnchor.MiddleLeft, 42f);
        GameObject presets = CreateUiObject("Presets", panel.transform);
        HorizontalLayoutGroup presetLayout = presets.AddComponent<HorizontalLayoutGroup>();
        presetLayout.spacing = 12f;
        presetLayout.childControlWidth = true;
        presetLayout.childControlHeight = true;
        presetLayout.childForceExpandWidth = true;
        LayoutElement presetHeight = presets.AddComponent<LayoutElement>();
        presetHeight.minHeight = 88f;
        AddButton(presets.transform, "Sprint", () => ApplyPreset(0));
        AddButton(presets.transform, "Classico", () => ApplyPreset(1));
        AddButton(presets.transform, "Sem Erros", () => ApplyPreset(2));

        AddText(panel.transform, "MODO", 20, TextAnchor.MiddleLeft, 34f);
        GameObject modeButtons = CreateUiObject("ModeButtons", panel.transform);
        HorizontalLayoutGroup modeLayout = modeButtons.AddComponent<HorizontalLayoutGroup>();
        modeLayout.spacing = 12f;
        modeLayout.childControlWidth = true;
        modeLayout.childControlHeight = true;
        modeLayout.childForceExpandWidth = true;
        LayoutElement modeHeight = modeButtons.AddComponent<LayoutElement>();
        modeHeight.minHeight = 76f;
        _timedModeButton = AddButton(modeButtons.transform, "Contra o relogio", () => SetMode(GameMode.Timed));
        _errorLimitModeButton = AddButton(modeButtons.transform, "Limite de erros", () => SetMode(GameMode.ErrorLimit));

        _itemsToWinInput = AddNumberField(panel.transform, "Itens para vencer", 10);
        _maxItemsInput = AddNumberField(panel.transform, "Itens na tela", 5);
        _matchTimeInput = AddNumberField(panel.transform, "Tempo inicial (segundos)", 30);
        _errorLimitInput = AddNumberField(panel.transform, "Erros permitidos", 3);
        _modeHelp = AddText(panel.transform, string.Empty, 18, TextAnchor.MiddleLeft, 54f);

        AddButton(panel.transform, "COMEÇAR PARTIDA", StartGame, 92f);
        SetMode(GameMode.Timed);
        ApplyPreset(1);
    }

    private void ApplyPreset(int presetIndex)
    {
        switch (presetIndex)
        {
            case 0:
                SetFields(GameMode.Timed, 8, 5, 20, 3);
                break;
            case 1:
                SetFields(GameMode.Timed, 15, 5, 45, 5);
                break;
            default:
                SetFields(GameMode.ErrorLimit, 12, 5, 30, 1);
                break;
        }
    }

    private void SetFields(GameMode mode, int itemsToWin, int maxItems, int matchTime, int errorLimit)
    {
        SetMode(mode);
        _itemsToWinInput.text = itemsToWin.ToString();
        _maxItemsInput.text = maxItems.ToString();
        _matchTimeInput.text = matchTime.ToString();
        _errorLimitInput.text = errorLimit.ToString();
    }

    private void SetMode(GameMode mode)
    {
        _selectedMode = mode;
        bool timed = mode == GameMode.Timed;
        _matchTimeInput.transform.parent.gameObject.SetActive(timed);
        _errorLimitInput.transform.parent.gameObject.SetActive(!timed);
        _timedModeButton.GetComponent<Image>().color = timed
            ? new Color(0.2f, 0.65f, 0.7f, 1f)
            : new Color(0.14f, 0.25f, 0.3f, 1f);
        _errorLimitModeButton.GetComponent<Image>().color = timed
            ? new Color(0.14f, 0.25f, 0.3f, 1f)
            : new Color(0.2f, 0.65f, 0.7f, 1f);
        _modeHelp.text = timed
            ? "O tempo diminui a cada segundo. Acabe os itens antes de zerar."
            : "A partida termina quando o limite de erros for alcancado.";
    }

    private void StartGame()
    {
        if (_baseConfig == null || _onStart == null)
            return;

        GameConfiguration selectedConfig = Instantiate(_baseConfig);
        selectedConfig.name = "SelectedGameConfiguration";
        selectedConfig.Mode = _selectedMode;
        selectedConfig.ItemsToWin = ReadNumber(_itemsToWinInput, 10);
        selectedConfig.MaxItemsOnScreen = ReadNumber(_maxItemsInput, 5);
        selectedConfig.MatchTime = ReadNumber(_matchTimeInput, 30);
        selectedConfig.ErrorLimit = ReadNumber(_errorLimitInput, 3);
        selectedConfig.IncreaseOnRight = Mathf.Max(0, _baseConfig.IncreaseOnRight);
        selectedConfig.DecreaseOnWrong = Mathf.Max(0, _baseConfig.DecreaseOnWrong);

        Destroy(_screenRoot);
        Destroy(this);
        _onStart(selectedConfig);
    }

    private static int ReadNumber(InputField input, int fallback)
    {
        return int.TryParse(input.text, out int value) ? Mathf.Max(1, value) : fallback;
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static Text AddText(Transform parent, string content, int fontSize, TextAnchor alignment, float height)
    {
        GameObject textObject = CreateUiObject(content, parent);
        Text text = textObject.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        LayoutElement layout = textObject.AddComponent<LayoutElement>();
        layout.minHeight = height;
        return text;
    }

    private static InputField AddNumberField(Transform parent, string label, int value)
    {
        GameObject row = CreateUiObject(label, parent);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 16f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        LayoutElement rowHeight = row.AddComponent<LayoutElement>();
        rowHeight.minHeight = 58f;

        Text labelText = AddText(row.transform, label, 20, TextAnchor.MiddleLeft, 58f);
        labelText.GetComponent<LayoutElement>().flexibleWidth = 1f;

        GameObject inputObject = CreateUiObject("Input", row.transform);
        Image image = inputObject.AddComponent<Image>();
        image.color = new Color(0.95f, 0.97f, 0.98f, 1f);
        InputField input = inputObject.AddComponent<InputField>();
        input.contentType = InputField.ContentType.IntegerNumber;
        input.text = value.ToString();
        input.textComponent = AddText(inputObject.transform, value.ToString(), 22, TextAnchor.MiddleCenter, 52f);
        input.textComponent.color = Color.black;
        LayoutElement inputSize = inputObject.AddComponent<LayoutElement>();
        inputSize.minWidth = 150f;
        inputSize.preferredWidth = 150f;
        return input;
    }

    private static Button AddButton(Transform parent, string label, UnityEngine.Events.UnityAction action, float height = 76f)
    {
        GameObject buttonObject = CreateUiObject(label, parent);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.2f, 0.65f, 0.7f, 1f);
        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(action);
        Text text = AddText(buttonObject.transform, label, 22, TextAnchor.MiddleCenter, height);
        text.color = Color.white;
        LayoutElement buttonHeight = buttonObject.AddComponent<LayoutElement>();
        buttonHeight.minHeight = height;
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