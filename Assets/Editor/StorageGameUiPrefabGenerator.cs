using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class StorageGameUiPrefabGenerator
{
    private const string ResourceFolder = "Assets/Resources/StorageGameUI";
    private static bool _attemptedEssentialsImport;

    static StorageGameUiPrefabGenerator()
    {
        EditorApplication.delayCall += EnsurePrefabs;
    }

    [MenuItem("Tools/Storage Game/Create Missing UI Prefabs")]
    private static void EnsurePrefabs()
    {
        string setupPath = $"{ResourceFolder}/GameSetupScreen.prefab";
        string statusPath = $"{ResourceFolder}/GameStatusScreen.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(setupPath) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(statusPath) != null)
            return;

        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font == null)
        {
            if (!_attemptedEssentialsImport)
            {
                _attemptedEssentialsImport = true;
                EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
                AssetDatabase.Refresh();
                EditorApplication.delayCall += EnsurePrefabs;
                return;
            }

            Debug.LogError("TextMeshPro Essential Resources are required to create the Storage Game UI prefabs.");
            return;
        }

        EnsureFolder("Assets/Resources");
        EnsureFolder(ResourceFolder);

        if (AssetDatabase.LoadAssetAtPath<GameObject>(setupPath) == null)
            CreateSetupPrefab(setupPath, font);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(statusPath) == null)
            CreateStatusPrefab(statusPath, font);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Storage Game UI prefabs are ready under Assets/Resources/StorageGameUI.");
    }

    public static void GenerateMissingPrefabs()
    {
        EnsurePrefabs();
    }

    private static void CreateSetupPrefab(string path, TMP_FontAsset font)
    {
        GameObject root = CreateRect("GameSetupScreen", null);
        Image background = root.AddComponent<Image>();
        background.color = new Color(0.04f, 0.08f, 0.12f, 0.96f);
        GameSetupScreen screen = root.AddComponent<GameSetupScreen>();

        GameObject panel = CreateRect("SetupPanel", root.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(920f, 1480f);
        panel.AddComponent<Image>().color = new Color(0.08f, 0.15f, 0.2f, 1f);
        VerticalLayoutGroup panelLayout = panel.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(56, 56, 48, 48);
        panelLayout.spacing = 14f;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = true;
        panelLayout.childForceExpandWidth = true;

        CreateText("Title", panel.transform, "CONFIGURE SUA PARTIDA", font, 42, TextAlignmentOptions.Center, 76f);
        CreateText("Subtitle", panel.transform, "Escolha um modelo rapido ou ajuste as regras", font, 22, TextAlignmentOptions.Center, 42f);
        CreateText("PresetsLabel", panel.transform, "PARTIDAS RAPIDAS", font, 20, TextAlignmentOptions.Left, 34f);

        GameObject presetRow = CreateRect("QuickPresets", panel.transform);
        ConfigureHorizontalRow(presetRow, 76f, 14f);
        Button sprint = CreateButton(presetRow.transform, "Sprint", font, 22, 76f);
        Button classic = CreateButton(presetRow.transform, "Classico", font, 22, 76f);
        Button noErrors = CreateButton(presetRow.transform, "Sem Erros", font, 22, 76f);

        CreateText("ModeLabel", panel.transform, "MODO", font, 20, TextAlignmentOptions.Left, 30f);
        GameObject modeRow = CreateRect("ModeSelector", panel.transform);
        ConfigureHorizontalRow(modeRow, 68f, 14f);
        Button timedMode = CreateButton(modeRow.transform, "Contra o relogio", font, 20, 68f);
        Button errorMode = CreateButton(modeRow.transform, "Limite de erros", font, 20, 68f);

        TMP_InputField itemsToWin = CreateNumberField("ItemsToWin", panel.transform, "Itens para vencer", 15, font);
        TMP_InputField maxItems = CreateNumberField("MaxItemsOnScreen", panel.transform, "Itens na tela", 5, font);

        GameObject timedOptions = CreateRect("TimedOptions", panel.transform);
        VerticalLayoutGroup timedLayout = timedOptions.AddComponent<VerticalLayoutGroup>();
        ConfigureVerticalLayout(timedLayout, 10f);
        timedOptions.AddComponent<LayoutElement>().preferredHeight = 200f;
        TMP_InputField matchTime = CreateNumberField("MatchTime", timedOptions.transform, "Tempo inicial (segundos)", 45, font);
        TMP_InputField increaseOnRight = CreateNumberField("IncreaseOnRight", timedOptions.transform, "Segundos ganhos ao acertar", 5, font);
        TMP_InputField decreaseOnWrong = CreateNumberField("DecreaseOnWrong", timedOptions.transform, "Segundos perdidos ao errar", 5, font);

        GameObject errorOptions = CreateRect("ErrorOptions", panel.transform);
        VerticalLayoutGroup errorLayout = errorOptions.AddComponent<VerticalLayoutGroup>();
        ConfigureVerticalLayout(errorLayout, 10f);
        errorOptions.AddComponent<LayoutElement>().preferredHeight = 60f;
        TMP_InputField errorLimit = CreateNumberField("ErrorLimit", errorOptions.transform, "Erros permitidos", 5, font);

        TMP_Text modeHelp = CreateText("ModeHelp", panel.transform, string.Empty, font, 18, TextAlignmentOptions.Left, 52f);
        CreateButton(panel.transform, "COMEÇAR PARTIDA", font, 24, 84f);
        Button startButton = panel.transform.GetChild(panel.transform.childCount - 1).GetComponent<Button>();

        Assign(screen, "_timedOptionsRoot", timedOptions);
        Assign(screen, "_errorOptionsRoot", errorOptions);
        Assign(screen, "_itemsToWinInput", itemsToWin);
        Assign(screen, "_maxItemsInput", maxItems);
        Assign(screen, "_matchTimeInput", matchTime);
        Assign(screen, "_increaseOnRightInput", increaseOnRight);
        Assign(screen, "_decreaseOnWrongInput", decreaseOnWrong);
        Assign(screen, "_errorLimitInput", errorLimit);
        Assign(screen, "_modeHelp", modeHelp);
        Assign(screen, "_timedModeButton", timedMode);
        Assign(screen, "_errorLimitModeButton", errorMode);
        Assign(screen, "_sprintPresetButton", sprint);
        Assign(screen, "_classicPresetButton", classic);
        Assign(screen, "_noErrorsPresetButton", noErrors);
        Assign(screen, "_startButton", startButton);

        SavePrefab(root, path);
    }

    private static void CreateStatusPrefab(string path, TMP_FontAsset font)
    {
        GameObject root = CreateRect("GameStatusScreen", null);
        GameStatusScreen screen = root.AddComponent<GameStatusScreen>();

        GameObject hud = CreateRect("GameHud", root.transform);
        RectTransform hudRect = hud.GetComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(0f, 1f);
        hudRect.anchorMax = new Vector2(1f, 1f);
        hudRect.pivot = new Vector2(0.5f, 1f);
        hudRect.offsetMin = new Vector2(36f, -170f);
        hudRect.offsetMax = new Vector2(-36f, -32f);
        HorizontalLayoutGroup hudLayout = hud.AddComponent<HorizontalLayoutGroup>();
        hudLayout.spacing = 24f;
        hudLayout.childControlWidth = true;
        hudLayout.childControlHeight = true;
        hudLayout.childForceExpandWidth = true;
        TMP_Text scoreText = CreateText("Score", hud.transform, string.Empty, font, 28, TextAlignmentOptions.Left, 64f);

        GameObject errorCounter = CreateRect("ErrorCounter", hud.transform);
        LayoutElement errorLayout = errorCounter.AddComponent<LayoutElement>();
        errorLayout.minHeight = 64f;
        errorLayout.flexibleWidth = 1f;
        TMP_Text errorText = CreateText("Errors", errorCounter.transform, string.Empty, font, 24, TextAlignmentOptions.Right, 64f);

        GameObject result = CreateRect("ResultScreen", root.transform);
        Stretch(result.GetComponent<RectTransform>());
        result.AddComponent<Image>().color = new Color(0.03f, 0.07f, 0.1f, 0.96f);
        GameObject resultPanel = CreateRect("ResultPanel", result.transform);
        RectTransform resultRect = resultPanel.GetComponent<RectTransform>();
        resultRect.anchorMin = new Vector2(0.5f, 0.5f);
        resultRect.anchorMax = new Vector2(0.5f, 0.5f);
        resultRect.pivot = new Vector2(0.5f, 0.5f);
        resultRect.sizeDelta = new Vector2(800f, 650f);
        resultPanel.AddComponent<Image>().color = new Color(0.08f, 0.15f, 0.2f, 1f);
        VerticalLayoutGroup resultLayout = resultPanel.AddComponent<VerticalLayoutGroup>();
        resultLayout.padding = new RectOffset(48, 48, 48, 48);
        ConfigureVerticalLayout(resultLayout, 18f);
        TMP_Text resultTitle = CreateText("ResultTitle", resultPanel.transform, "VITORIA", font, 48, TextAlignmentOptions.Center, 80f);
        TMP_Text resultScore = CreateText("ResultScore", resultPanel.transform, string.Empty, font, 28, TextAlignmentOptions.Center, 64f);
        TMP_Text resultErrors = CreateText("ResultErrors", resultPanel.transform, string.Empty, font, 24, TextAlignmentOptions.Center, 64f);
        Button reset = CreateButton(resultPanel.transform, "JOGAR NOVAMENTE", font, 24, 86f);

        result.SetActive(false);
        Assign(screen, "_hudRoot", hud);
        Assign(screen, "_resultRoot", result);
        Assign(screen, "_errorCounterRoot", errorCounter);
        Assign(screen, "_scoreText", scoreText);
        Assign(screen, "_errorText", errorText);
        Assign(screen, "_resultTitle", resultTitle);
        Assign(screen, "_resultScore", resultScore);
        Assign(screen, "_resultErrors", resultErrors);
        Assign(screen, "_resetButton", reset);

        SavePrefab(root, path);
    }

    private static TMP_InputField CreateNumberField(string objectName, Transform parent, string label, int value, TMP_FontAsset font)
    {
        GameObject row = CreateRect(objectName, parent);
        ConfigureHorizontalRow(row, 60f, 16f);

        TMP_Text labelText = CreateText("Label", row.transform, label, font, 20, TextAlignmentOptions.Left, 60f);
        labelText.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1f;

        GameObject inputObject = CreateRect("Input", row.transform);
        Image background = inputObject.AddComponent<Image>();
        background.color = new Color(0.95f, 0.97f, 0.98f, 1f);
        TMP_InputField input = inputObject.AddComponent<TMP_InputField>();
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        input.text = value.ToString();
        input.targetGraphic = background;

        GameObject textArea = CreateRect("TextArea", inputObject.transform);
        Stretch(textArea.GetComponent<RectTransform>());
        RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
        textAreaRect.offsetMin = new Vector2(10f, 4f);
        textAreaRect.offsetMax = new Vector2(-10f, -4f);
        textArea.AddComponent<RectMask2D>();
        TMP_Text placeholder = CreateText("Placeholder", textArea.transform, "0", font, 22, TextAlignmentOptions.Center, 48f);
        placeholder.color = new Color(0.3f, 0.34f, 0.36f, 0.6f);
        TMP_Text text = CreateText("Text", textArea.transform, value.ToString(), font, 22, TextAlignmentOptions.Center, 48f);
        Stretch(text.GetComponent<RectTransform>());
        input.textViewport = textAreaRect;
        input.textComponent = text;
        input.placeholder = placeholder;
        LayoutElement inputSize = inputObject.AddComponent<LayoutElement>();
        inputSize.minWidth = 180f;
        inputSize.preferredWidth = 180f;
        inputSize.minHeight = 60f;
        return input;
    }

    private static Button CreateButton(Transform parent, string label, TMP_FontAsset font, int fontSize, float height)
    {
        GameObject buttonObject = CreateRect(label, parent);
        Image background = buttonObject.AddComponent<Image>();
        background.color = new Color(0.2f, 0.65f, 0.7f, 1f);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = background;
        TMP_Text buttonText = CreateText("Label", buttonObject.transform, label, font, fontSize, TextAlignmentOptions.Center, height);
        buttonText.raycastTarget = false;
        Stretch(buttonText.GetComponent<RectTransform>());
        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.minHeight = height;
        return button;
    }

    private static TMP_Text CreateText(string objectName, Transform parent, string value, TMP_FontAsset font, int size, TextAlignmentOptions alignment, float height)
    {
        GameObject textObject = CreateRect(objectName, parent);
        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        LayoutElement layout = textObject.AddComponent<LayoutElement>();
        layout.minHeight = height;
        return text;
    }

    private static void ConfigureHorizontalRow(GameObject row, float height, float spacing)
    {
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        LayoutElement element = row.AddComponent<LayoutElement>();
        element.minHeight = height;
    }

    private static void ConfigureVerticalLayout(VerticalLayoutGroup layout, float spacing)
    {
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
    }

    private static GameObject CreateRect(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        if (parent != null)
            gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void Assign(Object target, string propertyName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string folderName = System.IO.Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
