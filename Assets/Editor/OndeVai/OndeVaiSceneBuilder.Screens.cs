using OndeVai.Gameplay;
using OndeVai.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Kit = OndeVai.EditorTools.UIBuilderKit;

namespace OndeVai.EditorTools
{
    public static partial class OndeVaiSceneBuilder
    {
        private const float HeaderTop = 60f;
        private const float HeaderHeight = 120f;
        private const float FooterHeight = 160f;
        private const float SidePadding = 60f;
        private const float RoundButton = 150f;

        private sealed class BuiltScreens
        {
            public DisclaimerScreen Disclaimer;
            public TitleScreen Title;
            public ChildProfileScreen Profiles;
            public OptionsScreen Options;
            public ProgressScreen Progress;
            public PauseScreen Pause;
            public GameObject Hud;
            public Button PauseButton;
        }

        private static BuiltScreens BuildScreens(Kit kit, Transform canvas, GameDefinition definition)
        {
            BuiltScreens screens = new();
            screens.Hud = BuildHud(kit, canvas, out screens.PauseButton);
            screens.Title = BuildTitle(kit, canvas, definition);
            screens.Profiles = BuildProfiles(kit, canvas);
            screens.Options = BuildOptions(kit, canvas);
            screens.Progress = BuildProgress(kit, canvas);
            screens.Pause = BuildPause(kit, canvas);
            screens.Disclaimer = BuildDisclaimer(kit, canvas);
            return screens;
        }

        private static T CreateScreen<T>(string name, Transform canvas, Color background, out RectTransform content) where T : UIScreen
        {
            RectTransform root = Kit.Stretch(Kit.Rect(name, canvas));
            root.gameObject.AddComponent<CanvasGroup>();
            Image backgroundImage = root.gameObject.AddComponent<Image>();
            backgroundImage.color = background;

            content = Kit.Stretch(Kit.Rect("Content", root));
            content.gameObject.AddComponent<SafeAreaFitter>();

            T screen = root.gameObject.AddComponent<T>();
            Kit.Wire(screen, "_content", content);
            root.gameObject.SetActive(false);
            return screen;
        }

        private static void Header(Kit kit, RectTransform content, string title)
        {
            TextMeshProUGUI header = kit.Text("Header", content, title, 84f, Kit.Ink);
            Kit.TopBand(header.rectTransform, HeaderTop, HeaderHeight, SidePadding);
        }

        private static RectTransform Footer(RectTransform content)
        {
            RectTransform footer = Kit.BottomBand(Kit.Rect("Footer", content), 60f, FooterHeight, SidePadding);
            Kit.Row(footer, 30f);
            return footer;
        }

        private static RectTransform Scroll(Kit kit, RectTransform content, float top, float bottom, float spacing)
        {
            RectTransform list = kit.ScrollView("Scroll", content, spacing, 10, out ScrollRect scrollRect);
            RectTransform scrollRoot = (RectTransform)scrollRect.transform;
            Kit.Stretch(scrollRoot);
            scrollRoot.offsetMin = new Vector2(SidePadding, bottom);
            scrollRoot.offsetMax = new Vector2(-SidePadding, -top);
            return list;
        }

        private static AvatarBadge Badge(Kit kit, Transform parent, float size)
        {
            Image circle = kit.Image("Avatar", parent, Color.white, kit.Circle);
            circle.rectTransform.sizeDelta = new Vector2(size, size);
            circle.raycastTarget = false;
            Kit.AddLayout(circle.gameObject, new Vector2(size, size));

            TextMeshProUGUI initial = kit.Text("Initial", circle.transform, "?", size * 0.55f, Color.white);
            Kit.Stretch(initial.rectTransform);

            AvatarBadge badge = circle.gameObject.AddComponent<AvatarBadge>();
            Kit.Wire(badge, "_background", circle);
            Kit.Wire(badge, "_initial", initial);
            return badge;
        }

        private static GameObject BuildHud(Kit kit, Transform canvas, out Button pauseButton)
        {
            RectTransform hud = Kit.Stretch(Kit.Rect("GameplayHUD", canvas));
            RectTransform safe = Kit.Stretch(Kit.Rect("SafeArea", hud));
            safe.gameObject.AddComponent<SafeAreaFitter>();

            SwipeDirectionHints hints = hud.gameObject.AddComponent<SwipeDirectionHints>();
            BuildSwipeHint(kit, safe, "LeftHint", "←", new Vector2(0f, 0.5f), new Vector2(125f, 0f), out TextMeshProUGUI leftHint);
            BuildSwipeHint(kit, safe, "RightHint", "→", new Vector2(1f, 0.5f), new Vector2(-125f, 0f), out TextMeshProUGUI rightHint);
            BuildSwipeHint(kit, safe, "UpHint", "↑", new Vector2(0.5f, 1f), new Vector2(0f, -260f), out TextMeshProUGUI upHint);
            BuildSwipeHint(kit, safe, "DownHint", "↓", new Vector2(0.5f, 0f), new Vector2(0f, 220f), out TextMeshProUGUI downHint);
            Kit.Wire(hints, "_leftRoot", leftHint.transform.parent.gameObject);
            Kit.Wire(hints, "_rightRoot", rightHint.transform.parent.gameObject);
            Kit.Wire(hints, "_upRoot", upHint.transform.parent.gameObject);
            Kit.Wire(hints, "_downRoot", downHint.transform.parent.gameObject);
            Kit.Wire(hints, "_leftLabel", leftHint);
            Kit.Wire(hints, "_rightLabel", rightHint);
            Kit.Wire(hints, "_upLabel", upHint);
            Kit.Wire(hints, "_downLabel", downHint);

            pauseButton = kit.Button("PauseButton", safe, new Vector2(130f, 130f), new Color(1f, 1f, 1f, 0.85f), IconLibrary.Get("icon_pause"), circle: true);
            Kit.Place((RectTransform)pauseButton.transform, new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(130f, 130f));
            Transform pauseIcon = pauseButton.transform.Find("Icon");
            if (pauseIcon != null)
                pauseIcon.GetComponent<Image>().color = Kit.Ink;

            hud.gameObject.SetActive(false);
            return hud.gameObject;
        }

        private static void BuildSwipeHint(
            Kit kit,
            Transform parent,
            string name,
            string arrow,
            Vector2 anchor,
            Vector2 position,
            out TextMeshProUGUI label)
        {
            Image panel = kit.Panel(name, parent, new Color(1f, 1f, 1f, 0.84f));
            Kit.Place(panel.rectTransform, anchor, position, new Vector2(220f, 130f));
            panel.raycastTarget = false;
            label = kit.Text("Label", panel.transform, arrow, 32f, Kit.Ink);
            Kit.Stretch(label.rectTransform, 8f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 22f;
            label.fontSizeMax = 32f;
        }

        private static DisclaimerScreen BuildDisclaimer(Kit kit, Transform canvas)
        {
            DisclaimerScreen screen = CreateScreen<DisclaimerScreen>("DisclaimerScreen", canvas, Kit.Cream, out RectTransform content);

            Image panel = kit.Panel("Panel", content, Kit.PanelColor);
            Kit.Stretch(panel.rectTransform, 50f);
            Kit.Column(panel.rectTransform, 30f, 50);

            Image icon = kit.Image("Icon", panel.transform, Kit.Blue, IconLibrary.Get("icon_lock"));
            icon.preserveAspect = true;
            Kit.AddLayout(icon.gameObject, new Vector2(130f, 130f));

            TextMeshProUGUI title = kit.Text("Title", panel.transform, DisclaimerScreen.Title, 72f, Kit.Ink);
            Kit.AddLayout(title.gameObject, new Vector2(-1f, 100f));

            TextMeshProUGUI body = kit.Text("Body", panel.transform, DisclaimerScreen.Body, 40f, Kit.Ink, TextAlignmentOptions.TopLeft);
            body.enableAutoSizing = true;
            body.fontSizeMin = 22f;
            body.fontSizeMax = 42f;
            body.lineSpacing = 8f;
            Kit.AddLayout(body.gameObject, new Vector2(-1f, 900f)).flexibleHeight = 1f;

            Button accept = kit.Button("AcceptButton", panel.transform, new Vector2(760f, 150f), Kit.Green, IconLibrary.Get("icon_check"), "Entendi e concordo", 52f);

            Kit.Wire(screen, "_titleText", title);
            Kit.Wire(screen, "_bodyText", body);
            Kit.Wire(screen, "_acceptButton", accept);
            return SaveScreenPrefab(screen, "DisclaimerScreen");
        }

        private static TitleScreen BuildTitle(Kit kit, Transform canvas, GameDefinition definition)
        {
            TitleScreen screen = CreateScreen<TitleScreen>("TitleScreen", canvas, Kit.Sky, out RectTransform content);

            TextMeshProUGUI logo = kit.Text("Logo", content, "Onde Vai?", 190f, Kit.Orange);
            Kit.Place(logo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(1000f, 260f));
            logo.textWrappingMode = TextWrappingModes.NoWrap;

            RectTransform pulse = Kit.Rect("PlayPulse", content);
            Kit.Place(pulse, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(440f, 440f));
            pulse.pivot = new Vector2(0.5f, 0.5f);
            Button play = kit.Button("PlayButton", pulse, new Vector2(440f, 440f), Kit.Green, IconLibrary.Get("icon_play"), circle: true);
            Kit.Stretch((RectTransform)play.transform);

            RectTransform scenarios = Kit.Rect("Scenarios", content);
            Kit.Place(scenarios, new Vector2(0.5f, 0.5f), new Vector2(0f, 355f), new Vector2(980f, 220f));
            scenarios.pivot = new Vector2(0.5f, 0.5f);
            Kit.Row(scenarios, 22f);
            SortingScenarioDefinition animals = FindScenario(definition, "farm-jungle-animals");
            SortingScenarioDefinition lettersNumbers = FindScenario(definition, "letters-numbers");
            SortingScenarioDefinition toysTools = FindScenario(definition, "toys-tools");
            Button animalsScenario = ScenarioButton(kit, scenarios, "AnimalsScenario", "Animais", Kit.Orange, animals, out Image animalsHighlight);
            Button lettersNumbersScenario = ScenarioButton(kit, scenarios, "LettersNumbersScenario", "Letras e números", Kit.Purple, lettersNumbers, out Image lettersNumbersHighlight);
            Button toysToolsScenario = ScenarioButton(kit, scenarios, "ToysToolsScenario", "Brinquedos e ferramentas", Kit.Blue, toysTools, out Image toysToolsHighlight);

            RectTransform modes = Kit.Rect("Modes", content);
            Kit.Place(modes, new Vector2(0.5f, 0.5f), new Vector2(0f, -380f), new Vector2(980f, 210f));
            modes.pivot = new Vector2(0.5f, 0.5f);
            Kit.Row(modes, 20f);
            Button dragMode = ModeButton(kit, modes, "DragMode", Kit.Blue, "icon_drag", "Arrastar", 190f, out Image dragHighlight);
            Button questionMode = ModeButton(kit, modes, "QuestionMode", Kit.Purple, "icon_question", "Perguntas", 190f, out Image questionHighlight);
            Button swipeMode = ModeButton(kit, modes, "SwipeMode", Kit.Green, "icon_right", "Cartas", 190f, out Image swipeHighlight);
            Button fourWaySwipeMode = ModeButton(kit, modes, "FourWaySwipeMode", Kit.Orange, "icon_left", "4 direções", 190f, out Image fourWaySwipeHighlight);

            Button profile = kit.Button("ProfileButton", content, new Vector2(170f, 170f), Color.white, null, circle: true);
            Kit.Place((RectTransform)profile.transform, new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(170f, 170f));
            AvatarBadge badge = Badge(kit, profile.transform, 140f);
            Kit.Place(badge.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140f, 140f));
            badge.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);

            HoldToConfirmButton options = kit.HoldButton("OptionsButton", content, new Vector2(RoundButton, RoundButton), Kit.Orange, IconLibrary.Get("icon_gear"), circle: true);
            Kit.Place((RectTransform)options.transform, new Vector2(1f, 1f), new Vector2(-40f, -50f), new Vector2(RoundButton, RoundButton));

            HoldToConfirmButton progress = kit.HoldButton("ProgressButton", content, new Vector2(RoundButton, RoundButton), Kit.Purple, IconLibrary.Get("icon_chart"), circle: true);
            Kit.Place((RectTransform)progress.transform, new Vector2(1f, 1f), new Vector2(-220f, -50f), new Vector2(RoundButton, RoundButton));

            TextMeshProUGUI hint = kit.Text("AdultHint", content, "Área dos responsáveis: segure", 28f, new Color(1f, 1f, 1f, 0.9f), TextAlignmentOptions.Right);
            Kit.Place(hint.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -215f), new Vector2(420f, 44f));

            Kit.Wire(screen, "_playButton", play);
            Kit.Wire(screen, "_playPulseTarget", pulse);
            Kit.Wire(screen, "_dragModeButton", dragMode);
            Kit.Wire(screen, "_questionModeButton", questionMode);
            Kit.Wire(screen, "_swipeModeButton", swipeMode);
            Kit.Wire(screen, "_fourWaySwipeModeButton", fourWaySwipeMode);
            Kit.Wire(screen, "_animalsScenarioButton", animalsScenario);
            Kit.Wire(screen, "_lettersNumbersScenarioButton", lettersNumbersScenario);
            Kit.Wire(screen, "_toysToolsScenarioButton", toysToolsScenario);
            Kit.Wire(screen, "_dragModeHighlight", dragHighlight);
            Kit.Wire(screen, "_questionModeHighlight", questionHighlight);
            Kit.Wire(screen, "_swipeModeHighlight", swipeHighlight);
            Kit.Wire(screen, "_fourWaySwipeModeHighlight", fourWaySwipeHighlight);
            Kit.Wire(screen, "_animalsScenarioHighlight", animalsHighlight);
            Kit.Wire(screen, "_lettersNumbersScenarioHighlight", lettersNumbersHighlight);
            Kit.Wire(screen, "_toysToolsScenarioHighlight", toysToolsHighlight);
            Kit.Wire(screen, "_profileButton", profile);
            Kit.Wire(screen, "_profileBadge", badge);
            Kit.Wire(screen, "_optionsButton", options);
            Kit.Wire(screen, "_progressButton", progress);
            Kit.Wire(screen, "_logo", logo.rectTransform);
            return screen;
        }

        private static SortingScenarioDefinition FindScenario(GameDefinition definition, string scenarioId)
        {
            if (definition == null)
                return null;

            foreach (SortingScenarioDefinition scenario in definition.Scenarios)
            {
                if (scenario != null && scenario.Id == scenarioId && scenario.IsPlayable)
                    return scenario;
            }

            Debug.LogError($"The title screen cannot display scenario '{scenarioId}' because it is not playable.", definition);
            return null;
        }

        private static Button ScenarioButton(
            Kit kit,
            RectTransform parent,
            string name,
            string label,
            Color color,
            SortingScenarioDefinition scenario,
            out Image highlight)
        {
            RectTransform slot = Kit.Rect(name, parent);
            Kit.AddLayout(slot.gameObject, new Vector2(312f, 210f));

            highlight = kit.Image("Highlight", slot, Kit.Yellow, kit.Rounded);
            Kit.Stretch(highlight.rectTransform, -12f);
            highlight.raycastTarget = false;
            highlight.gameObject.SetActive(false);

            Button button = kit.Button("Button", slot, new Vector2(312f, 210f), color, null);
            Kit.Stretch((RectTransform)button.transform);

            if (scenario != null)
            {
                ScenarioIcon(kit, button.transform, scenario.IconA, new Vector2(0.33f, 0.69f));
                ScenarioIcon(kit, button.transform, scenario.IconB, new Vector2(0.67f, 0.69f));
            }

            TextMeshProUGUI labelText = kit.Text("Label", button.transform, label, 30f, Color.white);
            Kit.Place(labelText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(300f, 74f));
            labelText.enableAutoSizing = true;
            labelText.fontSizeMin = 20f;
            labelText.fontSizeMax = 30f;
            labelText.textWrappingMode = TextWrappingModes.Normal;
            return button;
        }

        private static void ScenarioIcon(Kit kit, Transform parent, Sprite sprite, Vector2 anchor)
        {
            if (sprite == null)
                return;

            Image image = kit.Image("CategoryIcon", parent, Color.white, sprite);
            image.preserveAspect = true;
            image.raycastTarget = false;
            Kit.Place(image.rectTransform, anchor, Vector2.zero, new Vector2(82f, 82f));
        }

        private static Button ModeButton(
            Kit kit,
            RectTransform parent,
            string name,
            Color color,
            string icon,
            string label,
            float size,
            out Image highlight)
        {
            RectTransform slot = Kit.Rect(name, parent);
            Kit.AddLayout(slot.gameObject, new Vector2(size, size));

            highlight = kit.Image("Highlight", slot, Kit.Yellow, kit.Rounded);
            Kit.Stretch(highlight.rectTransform, -12f);
            highlight.raycastTarget = false;

            Button button = kit.Button("Button", slot, new Vector2(size, size), color, null);
            Kit.Stretch((RectTransform)button.transform);
            Image modeIcon = kit.Image("ModeIcon", button.transform, Color.white, IconLibrary.Get(icon));
            modeIcon.preserveAspect = true;
            modeIcon.raycastTarget = false;
            Kit.Place(modeIcon.rectTransform, new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(size * 0.5f, size * 0.5f));

            TextMeshProUGUI modeLabel = kit.Text("ModeLabel", button.transform, label, 30f, Color.white);
            Kit.Place(modeLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(size - 16f, 64f));
            modeLabel.enableAutoSizing = true;
            modeLabel.fontSizeMin = 20f;
            modeLabel.fontSizeMax = 30f;
            return button;
        }

        private static ChildProfileScreen BuildProfiles(Kit kit, Transform canvas)
        {
            ChildProfileScreen screen = CreateScreen<ChildProfileScreen>("ProfileScreen", canvas, Kit.Cream, out RectTransform content);
            Header(kit, content, "Quem vai jogar?");

            RectTransform list = Scroll(kit, content, HeaderTop + HeaderHeight + 30f, FooterHeight + 90f, 24f);
            ChildProfileRow rowTemplate = ProfileRow(kit, list);

            TextMeshProUGUI empty = kit.Text("EmptyMessage", content, "Nenhuma criança cadastrada ainda.\nToque em + para adicionar.", 46f, new Color(Kit.Ink.r, Kit.Ink.g, Kit.Ink.b, 0.6f));
            Kit.Place(empty.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 300f));
            empty.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            RectTransform footer = Footer(content);
            Button back = kit.Button("BackButton", footer, new Vector2(RoundButton, RoundButton), Kit.Blue, IconLibrary.Get("icon_left"), circle: true);
            Button guest = kit.Button("GuestButton", footer, new Vector2(480f, RoundButton), Kit.Orange, IconLibrary.Get("icon_person"), "Jogar sem cadastro", 40f);
            Button add = kit.Button("AddButton", footer, new Vector2(RoundButton, RoundButton), Kit.Green, IconLibrary.Get("icon_plus"), circle: true);

            ProfileForm form = BuildProfileForm(kit, content);

            Kit.Wire(screen, "_listContent", list);
            Kit.Wire(screen, "_rowTemplate", rowTemplate);
            Kit.Wire(screen, "_emptyListMessage", empty.gameObject);
            Kit.Wire(screen, "_addButton", add);
            Kit.Wire(screen, "_guestButton", guest);
            Kit.Wire(screen, "_backButton", back);
            Kit.Wire(screen, "_formRoot", form.Root);
            Kit.Wire(screen, "_formTitle", form.Title);
            Kit.Wire(screen, "_nameInput", form.Name);
            Kit.Wire(screen, "_ageSelector", form.Age);
            Kit.Wire(screen, "_avatarOptionsParent", form.AvatarParent);
            Kit.Wire(screen, "_avatarOptionTemplate", form.AvatarTemplate);
            Kit.Wire(screen, "_formPreview", form.Preview);
            Kit.Wire(screen, "_formError", form.Error);
            Kit.Wire(screen, "_saveButton", form.Save);
            Kit.Wire(screen, "_cancelButton", form.Cancel);
            Kit.Wire(screen, "_deleteButton", form.Delete);
            return screen;
        }

        private static ChildProfileRow ProfileRow(Kit kit, RectTransform list)
        {
            Image background = kit.Panel("RowTemplate", list, Color.white);
            Kit.AddLayout(background.gameObject, new Vector2(-1f, 190f));
            Button select = background.gameObject.AddComponent<Button>();
            select.targetGraphic = background;
            background.gameObject.AddComponent<UIButtonFeedback>();

            AvatarBadge badge = Badge(kit, background.transform, 150f);
            Kit.Place(badge.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(25f, 0f), new Vector2(150f, 150f));

            TextMeshProUGUI nameText = kit.Text("Name", background.transform, "Nome", 56f, Kit.Ink, TextAlignmentOptions.BottomLeft);
            Kit.Stretch(nameText.rectTransform);
            nameText.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            nameText.rectTransform.offsetMin = new Vector2(205f, 0f);
            nameText.rectTransform.offsetMax = new Vector2(-300f, -10f);
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.overflowMode = TextOverflowModes.Ellipsis;

            TextMeshProUGUI detail = kit.Text("Detail", background.transform, "4 anos", 38f, new Color(Kit.Ink.r, Kit.Ink.g, Kit.Ink.b, 0.6f), TextAlignmentOptions.TopLeft);
            Kit.Stretch(detail.rectTransform);
            detail.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            detail.rectTransform.offsetMin = new Vector2(205f, 10f);
            detail.rectTransform.offsetMax = new Vector2(-300f, 0f);

            Image marker = kit.Image("ActiveMarker", background.transform, Kit.Green, kit.Circle);
            Kit.Place(marker.rectTransform, new Vector2(1f, 0.5f), new Vector2(-165f, 0f), new Vector2(90f, 90f));
            marker.raycastTarget = false;
            Image check = kit.Image("Check", marker.transform, Color.white, IconLibrary.Get("icon_check"));
            Kit.Stretch(check.rectTransform, 16f);
            check.raycastTarget = false;

            Button edit = kit.Button("EditButton", background.transform, new Vector2(120f, 120f), Kit.Orange, IconLibrary.Get("icon_gear"), circle: true);
            Kit.Place((RectTransform)edit.transform, new Vector2(1f, 0.5f), new Vector2(-25f, 0f), new Vector2(120f, 120f));

            ChildProfileRow row = background.gameObject.AddComponent<ChildProfileRow>();
            Kit.Wire(row, "_selectButton", select);
            Kit.Wire(row, "_editButton", edit);
            Kit.Wire(row, "_badge", badge);
            Kit.Wire(row, "_nameText", nameText);
            Kit.Wire(row, "_detailText", detail);
            Kit.Wire(row, "_activeMarker", marker.gameObject);
            return row;
        }

        private sealed class ProfileForm
        {
            public GameObject Root;
            public TMP_Text Title;
            public TMP_InputField Name;
            public OptionSelector Age;
            public RectTransform AvatarParent;
            public Button AvatarTemplate;
            public AvatarBadge Preview;
            public TMP_Text Error;
            public Button Save;
            public Button Cancel;
            public HoldToConfirmButton Delete;
        }

        private static ProfileForm BuildProfileForm(Kit kit, RectTransform content)
        {
            ProfileForm form = new();
            Image overlay = kit.Image("Form", content, Kit.Dim);
            Kit.Stretch(overlay.rectTransform);
            form.Root = overlay.gameObject;

            Image panel = kit.Panel("Panel", overlay.transform, Kit.PanelColor);
            Kit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 1420f));
            panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Kit.Column(panel.rectTransform, 22f, 50);

            form.Title = kit.Text("Title", panel.transform, "Nova criança", 68f, Kit.Ink);
            Kit.AddLayout(form.Title.gameObject, new Vector2(-1f, 90f));

            RectTransform previewRow = Kit.Rect("PreviewRow", panel.transform);
            Kit.AddLayout(previewRow.gameObject, new Vector2(-1f, 190f));
            form.Preview = Badge(kit, previewRow, 180f);
            RectTransform previewRect = form.Preview.GetComponent<RectTransform>();
            Kit.Place(previewRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180f, 180f));
            previewRect.pivot = new Vector2(0.5f, 0.5f);

            FormLabel(kit, panel.transform, "Nome");
            form.Name = kit.InputField("NameInput", panel.transform, new Vector2(860f, 130f), "Digite o nome");

            FormLabel(kit, panel.transform, "Idade");
            form.Age = kit.Selector("AgeSelector", panel.transform, new Vector2(860f, 110f), Kit.Blue);

            FormLabel(kit, panel.transform, "Cor");
            RectTransform avatars = Kit.Rect("AvatarOptions", panel.transform);
            Kit.AddLayout(avatars.gameObject, new Vector2(-1f, 110f));
            Kit.Row(avatars, 14f);
            form.AvatarParent = avatars;
            form.AvatarTemplate = kit.Button("AvatarOption", avatars, new Vector2(88f, 88f), Color.white, null, circle: true);

            form.Error = kit.Text("Error", panel.transform, string.Empty, 36f, Kit.Red);
            Kit.AddLayout(form.Error.gameObject, new Vector2(-1f, 50f));

            RectTransform buttons = Kit.Rect("Buttons", panel.transform);
            Kit.AddLayout(buttons.gameObject, new Vector2(-1f, 150f));
            Kit.Row(buttons, 30f);
            form.Cancel = kit.Button("CancelButton", buttons, new Vector2(140f, 140f), Kit.Blue, IconLibrary.Get("icon_left"), circle: true);
            form.Save = kit.Button("SaveButton", buttons, new Vector2(400f, 140f), Kit.Green, IconLibrary.Get("icon_check"), "Salvar", 52f);
            form.Delete = kit.HoldButton("DeleteButton", buttons, new Vector2(140f, 140f), Kit.Red, IconLibrary.Get("icon_trash"), circle: true);

            TextMeshProUGUI deleteHint = kit.Text("DeleteHint", panel.transform, "Segure a lixeira para apagar a criança e todo o seu histórico.", 28f, new Color(Kit.Ink.r, Kit.Ink.g, Kit.Ink.b, 0.6f));
            Kit.AddLayout(deleteHint.gameObject, new Vector2(-1f, 70f));

            overlay.gameObject.SetActive(false);
            return form;
        }

        private static void FormLabel(Kit kit, Transform parent, string text)
        {
            TextMeshProUGUI label = kit.Text(text + "Label", parent, text, 42f, Kit.Ink, TextAlignmentOptions.BottomLeft);
            Kit.AddLayout(label.gameObject, new Vector2(-1f, 56f));
        }

        private static OptionsScreen BuildOptions(Kit kit, Transform canvas)
        {
            OptionsScreen screen = CreateScreen<OptionsScreen>("OptionsScreen", canvas, Kit.Cream, out RectTransform content);
            Header(kit, content, "Opções");

            RectTransform list = Scroll(kit, content, HeaderTop + HeaderHeight + 30f, FooterHeight + 90f, 18f);
            Vector2 selectorSize = new(540f, 100f);

            Slider music = kit.Slider("MusicSlider", OptionRow(kit, list, "Música", new Vector2(500f, 70f)), new Vector2(500f, 70f), Kit.Orange);
            Slider sfx = kit.Slider("SfxSlider", OptionRow(kit, list, "Efeitos sonoros", new Vector2(500f, 70f)), new Vector2(500f, 70f), Kit.Orange);
            Toggle vibration = kit.Toggle("VibrationToggle", OptionRow(kit, list, "Vibração", new Vector2(100f, 100f)), 100f, Kit.Green);
            Toggle tutorial = kit.Toggle("TutorialToggle", OptionRow(kit, list, "Tutorial no início", new Vector2(100f, 100f)), 100f, Kit.Green);
            OptionSelector tutorialItems = kit.Selector("TutorialItems", OptionRow(kit, list, "Objetos guiados", selectorSize), selectorSize, Kit.Blue);
            OptionSelector mode = kit.Selector("Mode", OptionRow(kit, list, "Modo de jogo", selectorSize), selectorSize, Kit.Blue);
            OptionSelector scenario = kit.Selector("Scenario", OptionRow(kit, list, "Cenário", selectorSize), selectorSize, Kit.Blue);
            OptionSelector matchTime = kit.Selector("MatchTime", OptionRow(kit, list, "Tempo da partida", selectorSize), selectorSize, Kit.Blue);
            OptionSelector errorLimit = kit.Selector("ErrorLimit", OptionRow(kit, list, "Erros permitidos", selectorSize), selectorSize, Kit.Blue);
            OptionSelector itemsOnScreen = kit.Selector("ItemsOnScreen", OptionRow(kit, list, "Objetos na tela", selectorSize), selectorSize, Kit.Blue);

            SectionTitle(kit, list, "Dados do perfil ativo");
            TextMeshProUGUI profileDataStatus = kit.Text(
                "ProfileDataStatus",
                list,
                "As ações abaixo afetam apenas o perfil ativo.",
                34f,
                new Color(Kit.Ink.r, Kit.Ink.g, Kit.Ink.b, 0.7f),
                TextAlignmentOptions.MidlineLeft);
            Kit.AddLayout(profileDataStatus.gameObject, new Vector2(-1f, 100f));
            HoldToConfirmButton clearProgress = kit.HoldButton(
                "ClearProgressButton",
                list,
                new Vector2(860f, 120f),
                Kit.Orange,
                IconLibrary.Get("icon_replay"),
                "Segure: apagar progresso");
            HoldToConfirmButton deleteProfile = kit.HoldButton(
                "DeleteProfileButton",
                list,
                new Vector2(860f, 120f),
                Kit.Red,
                IconLibrary.Get("icon_trash"),
                "Segure: excluir perfil");

            foreach (Component control in new Component[] { music, sfx, vibration, tutorial, tutorialItems, mode, scenario, matchTime, errorLimit, itemsOnScreen })
                Kit.Stretch((RectTransform)control.transform);

            RectTransform footer = Footer(content);
            Button back = kit.Button("BackButton", footer, new Vector2(RoundButton, RoundButton), Kit.Blue, IconLibrary.Get("icon_left"), circle: true);
            Button privacy = kit.Button("PrivacyButton", footer, new Vector2(320f, RoundButton), Kit.Purple, IconLibrary.Get("icon_info"), "Privacidade", 40f);
            HoldToConfirmButton reset = kit.HoldButton("ResetButton", footer, new Vector2(420f, RoundButton), Kit.Red, IconLibrary.Get("icon_replay"), "Segure: restaurar padrões");

            Kit.Wire(screen, "_musicSlider", music);
            Kit.Wire(screen, "_sfxSlider", sfx);
            Kit.Wire(screen, "_vibrationToggle", vibration);
            Kit.Wire(screen, "_tutorialToggle", tutorial);
            Kit.Wire(screen, "_tutorialItemsSelector", tutorialItems);
            Kit.Wire(screen, "_modeSelector", mode);
            Kit.Wire(screen, "_scenarioSelector", scenario);
            Kit.Wire(screen, "_matchTimeSelector", matchTime);
            Kit.Wire(screen, "_errorLimitSelector", errorLimit);
            Kit.Wire(screen, "_itemsOnScreenSelector", itemsOnScreen);
            Kit.Wire(screen, "_backButton", back);
            Kit.Wire(screen, "_privacyButton", privacy);
            Kit.Wire(screen, "_resetButton", reset);
            Kit.Wire(screen, "_profileDataStatus", profileDataStatus);
            Kit.Wire(screen, "_clearProgressButton", clearProgress);
            Kit.Wire(screen, "_deleteProfileButton", deleteProfile);
            return SaveScreenPrefab(screen, "OptionsScreen");
        }

        /// <summary>White row with a label on the left. Returns the area on the right that holds the control.</summary>
        private static RectTransform OptionRow(Kit kit, RectTransform list, string label, Vector2 controlSize)
        {
            Image row = kit.Panel(label, list, Color.white);
            Kit.AddLayout(row.gameObject, new Vector2(-1f, 140f));

            TextMeshProUGUI text = kit.Text("Label", row.transform, label, 44f, Kit.Ink, TextAlignmentOptions.MidlineLeft);
            Kit.Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(40f, 0f);
            text.rectTransform.offsetMax = new Vector2(-controlSize.x - 60f, 0f);
            text.enableAutoSizing = true;
            text.fontSizeMin = 24f;
            text.fontSizeMax = 44f;

            RectTransform area = Kit.Rect("Control", row.transform);
            Kit.Place(area, new Vector2(1f, 0.5f), new Vector2(-30f, 0f), controlSize);
            return area;
        }

        private static ProgressScreen BuildProgress(Kit kit, Transform canvas)
        {
            ProgressScreen screen = CreateScreen<ProgressScreen>("ProgressScreen", canvas, Kit.Cream, out RectTransform content);
            Header(kit, content, "Progresso");

            RectTransform childRow = Kit.TopBand(Kit.Rect("ChildRow", content), HeaderTop + HeaderHeight + 20f, 150f, SidePadding);
            AvatarBadge badge = Badge(kit, childRow, 130f);
            Kit.Place(badge.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(130f, 130f));
            OptionSelector selector = kit.Selector("ChildSelector", childRow, new Vector2(800f, 120f), Kit.Blue);
            RectTransform selectorRect = (RectTransform)selector.transform;
            Kit.Stretch(selectorRect);
            selectorRect.offsetMin = new Vector2(160f, 15f);
            selectorRect.offsetMax = new Vector2(0f, -15f);

            RectTransform list = Scroll(kit, content, HeaderTop + HeaderHeight + 190f, FooterHeight + 90f, 22f);

            TextMeshProUGUI summary = kit.Text("Summary", list, string.Empty, 40f, Kit.Ink, TextAlignmentOptions.TopLeft);
            summary.lineSpacing = 12f;

            TextMeshProUGUI noData = kit.Text("NoData", list, "Ainda não há partidas registradas.", 42f, new Color(Kit.Ink.r, Kit.Ink.g, Kit.Ink.b, 0.6f));

            SectionTitle(kit, list, "Acertos por categoria");
            RectTransform bars = Kit.Rect("GroupBars", list);
            Kit.Column(bars, 14f);
            StatBarRow barTemplate = StatBar(kit, bars);

            SectionTitle(kit, list, "Últimas partidas (acertos)");
            Image chartPanel = kit.Panel("Chart", list, Color.white);
            Kit.AddLayout(chartPanel.gameObject, new Vector2(-1f, 280f));
            HorizontalLayoutGroup chartLayout = Kit.Row(chartPanel.rectTransform, 18f, TextAnchor.LowerCenter);
            chartLayout.childControlHeight = false;
            chartLayout.padding = new RectOffset(20, 20, 30, 30);
            Image chartBar = kit.Image("BarTemplate", chartPanel.transform, Kit.Green, kit.Rounded, 0.5f);
            chartBar.rectTransform.sizeDelta = new Vector2(56f, 100f);
            Kit.AddLayout(chartBar.gameObject, new Vector2(56f, -1f));

            RectTransform sessions = Kit.Rect("Sessions", list);
            Kit.Column(sessions, 10f);
            TextMeshProUGUI sessionTemplate = kit.Text("SessionTemplate", sessions, "Partida", 34f, Kit.Ink, TextAlignmentOptions.TopLeft);

            RectTransform footer = Footer(content);
            Button back = kit.Button("BackButton", footer, new Vector2(RoundButton, RoundButton), Kit.Blue, IconLibrary.Get("icon_left"), circle: true);
            HoldToConfirmButton delete = kit.HoldButton("DeleteHistoryButton", footer, new Vector2(560f, RoundButton), Kit.Red, IconLibrary.Get("icon_trash"), "Segure: apagar histórico");

            Kit.Wire(screen, "_childSelector", selector);
            Kit.Wire(screen, "_childBadge", badge);
            Kit.Wire(screen, "_summaryText", summary);
            Kit.Wire(screen, "_noDataMessage", noData.gameObject);
            Kit.Wire(screen, "_groupBarsParent", bars);
            Kit.Wire(screen, "_groupBarTemplate", barTemplate);
            Kit.Wire(screen, "_chartParent", chartPanel.rectTransform);
            Kit.Wire(screen, "_chartBarTemplate", chartBar);
            Kit.Wire(screen, "_sessionListParent", sessions);
            Kit.Wire(screen, "_sessionRowTemplate", sessionTemplate);
            Kit.Wire(screen, "_deleteHistoryButton", delete);
            Kit.Wire(screen, "_backButton", back);
            return screen;
        }

        private static void SectionTitle(Kit kit, RectTransform parent, string text)
        {
            TextMeshProUGUI title = kit.Text(text, parent, text, 52f, Kit.Orange, TextAlignmentOptions.BottomLeft);
            Kit.AddLayout(title.gameObject, new Vector2(-1f, 90f));
        }

        private static StatBarRow StatBar(Kit kit, RectTransform parent)
        {
            RectTransform row = Kit.Rect("BarTemplate", parent);
            Kit.AddLayout(row.gameObject, new Vector2(-1f, 80f));

            TextMeshProUGUI label = kit.Text("Label", row, "Categoria", 36f, Kit.Ink, TextAlignmentOptions.MidlineLeft);
            Kit.Stretch(label.rectTransform);
            label.rectTransform.anchorMax = new Vector2(0.42f, 1f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 20f;
            label.fontSizeMax = 36f;

            Image track = kit.Image("Track", row, Kit.Track, kit.Rounded, 0.4f);
            Kit.Stretch(track.rectTransform);
            track.rectTransform.anchorMin = new Vector2(0.44f, 0.2f);
            track.rectTransform.anchorMax = new Vector2(0.84f, 0.8f);

            Image fill = kit.Image("Fill", track.transform, Kit.Green, kit.Rounded, 0.4f);
            Kit.Stretch(fill.rectTransform);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0.5f;

            TextMeshProUGUI value = kit.Text("Value", row, "0/0", 34f, Kit.Ink, TextAlignmentOptions.MidlineRight);
            Kit.Stretch(value.rectTransform);
            value.rectTransform.anchorMin = new Vector2(0.85f, 0f);

            StatBarRow bar = row.gameObject.AddComponent<StatBarRow>();
            Kit.Wire(bar, "_label", label);
            Kit.Wire(bar, "_fill", fill);
            Kit.Wire(bar, "_value", value);
            return bar;
        }

        private static PauseScreen BuildPause(Kit kit, Transform canvas)
        {
            PauseScreen screen = CreateScreen<PauseScreen>("PauseScreen", canvas, Kit.Dim, out RectTransform content);

            Image panel = kit.Panel("Panel", content, Kit.PanelColor);
            Kit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 520f));
            panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Kit.Row(panel.rectTransform, 50f);

            Button home = kit.Button("HomeButton", panel.transform, new Vector2(220f, 220f), Kit.Blue, IconLibrary.Get("icon_home"), circle: true);
            Button resume = kit.Button("ResumeButton", panel.transform, new Vector2(260f, 260f), Kit.Green, IconLibrary.Get("icon_play"), circle: true);
            Button restart = kit.Button("RestartButton", panel.transform, new Vector2(220f, 220f), Kit.Orange, IconLibrary.Get("icon_replay"), circle: true);

            Kit.Wire(screen, "_resumeButton", resume);
            Kit.Wire(screen, "_restartButton", restart);
            Kit.Wire(screen, "_homeButton", home);
            return screen;
        }
    }
}
