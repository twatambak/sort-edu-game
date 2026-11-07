using System.Collections.Generic;
using OndeVai.Audio;
using OndeVai.Effects;
using OndeVai.Settings;
using OndeVai.Tutorial;
using OndeVai.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace OndeVai.EditorTools
{
    /// <summary>
    /// One click setup of the app flow in the open scene: generates every menu screen, the gameplay
    /// systems (audio, effects, tutorial) and wires them to the existing GameController. Fully undoable.
    /// </summary>
    public static partial class OndeVaiSceneBuilder
    {
        private const string MenuRoot = "Tools/Onde Vai/";
        private const string UIRootName = "AppUI";
        private const string SystemsRootName = "OndeVai Systems";
        private const string HomeButtonName = "Home_Button";
        private const int CanvasSortingOrder = 1000;

        [MenuItem(MenuRoot + "Build App UI", priority = 0)]
        public static void BuildAppUI()
        {
            GameController controller = Object.FindFirstObjectByType<GameController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                EditorUtility.DisplayDialog("Onde Vai", "No GameController found in the open scene.", "OK");
                return;
            }

            GameObject existingUI = GameObject.Find(UIRootName);
            GameObject existingSystems = GameObject.Find(SystemsRootName);
            if ((existingUI != null || existingSystems != null) &&
                !EditorUtility.DisplayDialog("Onde Vai", "The app UI already exists. Rebuild it? Manual changes to the generated objects will be lost.", "Rebuild", "Cancel"))
                return;

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Build Onde Vai App UI");

            if (existingUI != null)
                Undo.DestroyObjectImmediate(existingUI);

            if (existingSystems != null)
                Undo.DestroyObjectImmediate(existingSystems);

            IconLibrary.ClearCache();
            UIBuilderKit kit = new();

            Canvas canvas = CreateCanvas();
            BuiltScreens screens = BuildScreens(kit, canvas.transform);
            GameStatusScreen statusScreen = Object.FindFirstObjectByType<GameStatusScreen>(FindObjectsInactive.Include);
            BuildFlow(canvas, screens, statusScreen);
            Undo.RegisterCreatedObjectUndo(canvas.gameObject, "Create App UI");

            Transform questionPoint = BuildSystems(controller, out GameObject systemsRoot);
            Undo.RegisterCreatedObjectUndo(systemsRoot, "Create Onde Vai Systems");

            ConfigureController(controller, questionPoint);
            if (statusScreen != null)
                AddHomeButton(statusScreen);
            else
                Debug.LogWarning("No GameStatusScreen found. The results screen will not have a home button.");

            EnsureEventSystem();

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            Selection.activeGameObject = canvas.gameObject;
            Debug.Log("Onde Vai app UI built. Review the generated 'AppUI' and 'OndeVai Systems' objects and save the scene.", canvas);
        }

        [MenuItem(MenuRoot + "Debug/Reset Privacy Disclaimer", priority = 100)]
        public static void ResetDisclaimer()
        {
            PlayerPrefs.DeleteKey(GameSettingsService.DisclaimerPrefsKey);
            PlayerPrefs.Save();
            Debug.Log("Privacy disclaimer acceptance cleared. It will show on the next play.");
        }

        [MenuItem(MenuRoot + "Debug/Reset Settings", priority = 101)]
        public static void ResetSettings()
        {
            PlayerPrefs.DeleteKey(GameSettingsService.SettingsPrefsKey);
            PlayerPrefs.Save();
            Debug.Log("Saved settings cleared.");
        }

        [MenuItem(MenuRoot + "Debug/Open Local Data Folder", priority = 102)]
        public static void OpenDataFolder()
        {
            EditorUtility.RevealInFinder(System.IO.Path.Combine(Application.persistentDataPath, GameHistoryDatabase.FileName));
        }

        private static Canvas CreateCanvas()
        {
            GameObject root = new(UIRootName, typeof(RectTransform));
            root.layer = LayerMask.NameToLayer("UI");
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void BuildFlow(Canvas canvas, BuiltScreens screens, GameStatusScreen statusScreen)
        {
            GameFlowController flow = canvas.gameObject.AddComponent<GameFlowController>();
            UIBuilderKit.Wire(flow, "_disclaimerScreen", screens.Disclaimer);
            UIBuilderKit.Wire(flow, "_titleScreen", screens.Title);
            UIBuilderKit.Wire(flow, "_profileScreen", screens.Profiles);
            UIBuilderKit.Wire(flow, "_optionsScreen", screens.Options);
            UIBuilderKit.Wire(flow, "_progressScreen", screens.Progress);
            UIBuilderKit.Wire(flow, "_pauseScreen", screens.Pause);
            UIBuilderKit.Wire(flow, "_statusScreen", statusScreen);
            UIBuilderKit.Wire(flow, "_gameplayHud", screens.Hud);
            UIBuilderKit.Wire(flow, "_pauseButton", screens.PauseButton);
        }

        private static Transform BuildSystems(GameController controller, out GameObject root)
        {
            root = new GameObject(SystemsRootName);

            GameObject audioObject = new("Audio");
            audioObject.transform.SetParent(root.transform, false);
            AudioSource music = audioObject.AddComponent<AudioSource>();
            AudioSource sfx = audioObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            sfx.playOnAwake = false;
            GameAudio gameAudio = audioObject.AddComponent<GameAudio>();
            UIBuilderKit.Wire(gameAudio, "_musicSource", music);
            UIBuilderKit.Wire(gameAudio, "_sfxSource", sfx);
            SerializedObject audioSerialized = new(gameAudio);
            audioSerialized.FindProperty("_dontDestroyOnLoad").boolValue = false;
            audioSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject effectsObject = new("Effects");
            effectsObject.transform.SetParent(root.transform, false);
            SpriteBurstPool pool = effectsObject.AddComponent<SpriteBurstPool>();
            GameFeedbackEffects effects = effectsObject.AddComponent<GameFeedbackEffects>();
            UIBuilderKit.Wire(effects, "_burstPool", pool);
            UIBuilderKit.Wire(effects, "_camera", Camera.main);
            EnsureCameraFitter(Camera.main);

            GameObject tutorialObject = new("Tutorial");
            tutorialObject.transform.SetParent(root.transform, false);
            tutorialObject.AddComponent<TutorialGuide>();

            GameObject questionPoint = new("QuestionPoint");
            questionPoint.transform.SetParent(root.transform, false);
            questionPoint.transform.position = EstimateQuestionPoint(controller);
            return questionPoint.transform;
        }

        /// <summary>Center of the spawn points, which sits above the storages on the shelves.</summary>
        private static Vector3 EstimateQuestionPoint(GameController controller)
        {
            SerializedProperty spawnPoints = new SerializedObject(controller).FindProperty("_spawnPoints");
            Vector3 sum = Vector3.zero;
            int count = 0;
            for (int i = 0; spawnPoints != null && i < spawnPoints.arraySize; i++)
            {
                if (spawnPoints.GetArrayElementAtIndex(i).objectReferenceValue is Transform point)
                {
                    sum += point.position;
                    count++;
                }
            }

            Vector3 result = count > 0 ? sum / count : new Vector3(0f, 1.5f, 0f);
            result.z = 0f;
            return result;
        }

        private static void ConfigureController(GameController controller, Transform questionPoint)
        {
            List<Storage> storages = new(Object.FindObjectsByType<Storage>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
            storages.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

            SerializedObject serialized = new(controller);
            serialized.FindProperty("_beginOnStart").boolValue = false;
            serialized.FindProperty("_autoPauseOnFocusLoss").boolValue = true;
            serialized.FindProperty("_questionPoint").objectReferenceValue = questionPoint;
            if (storages.Count >= 2)
            {
                serialized.FindProperty("_storageA").objectReferenceValue = storages[0];
                serialized.FindProperty("_storageB").objectReferenceValue = storages[storages.Count - 1];
            }
            else
            {
                Debug.LogWarning("Expected two storages in the scene. Scenario switching is disabled until _storageA and _storageB are assigned on the GameController.", controller);
            }

            serialized.ApplyModifiedProperties();
        }

        /// <summary>Duplicates the play again button of the results screen into a home button.</summary>
        private static void AddHomeButton(GameStatusScreen statusScreen)
        {
            Button playAgain = null;
            foreach (Button button in statusScreen.GetComponentsInChildren<Button>(true))
            {
                if (button.name == HomeButtonName)
                    return;

                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                {
                    if (button.onClick.GetPersistentMethodName(i) == nameof(GameStatusScreen.PlayAgain))
                        playAgain = button;
                }
            }

            if (playAgain == null)
            {
                Debug.LogWarning("Could not find the play again button on the results screen. Add a button that calls GameStatusScreen.GoHome manually.", statusScreen);
                return;
            }

            GameObject copy = Object.Instantiate(playAgain.gameObject, playAgain.transform.parent);
            copy.name = HomeButtonName;
            Undo.RegisterCreatedObjectUndo(copy, "Create Home Button");
            copy.transform.SetSiblingIndex(playAgain.transform.GetSiblingIndex() + 1);

            Button homeButton = copy.GetComponent<Button>();
            while (homeButton.onClick.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(homeButton.onClick, 0);

            UnityEventTools.AddPersistentListener(homeButton.onClick, new UnityAction(statusScreen.GoHome));

            foreach (TMP_Text label in copy.GetComponentsInChildren<TMP_Text>(true))
                label.gameObject.SetActive(false);

            Image icon = new UIBuilderKit().Image("HomeIcon", copy.transform, Color.white, IconLibrary.Get("icon_home"));
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            UIBuilderKit.Stretch(icon.rectTransform, 16f);

            if (copy.transform.parent.GetComponent<LayoutGroup>() == null)
            {
                RectTransform rect = (RectTransform)copy.transform;
                rect.anchoredPosition += new Vector2(0f, -(rect.rect.height + 30f));
            }
        }

        /// <summary>Adds the aspect ratio fitter to the gameplay camera so tall phones do not crop the storages.</summary>
        private static void EnsureCameraFitter(Camera camera)
        {
            if (camera == null)
            {
                Debug.LogWarning("No main camera found. Add a CameraFitter to the gameplay camera manually.");
                return;
            }

            if (camera.GetComponent<OndeVai.Platform.CameraFitter>() != null)
                return;

            OndeVai.Platform.CameraFitter fitter = Undo.AddComponent<OndeVai.Platform.CameraFitter>(camera.gameObject);
            SerializedObject serialized = new(fitter);
            serialized.FindProperty("_designOrthographicSize").floatValue = camera.orthographicSize;
            serialized.ApplyModifiedProperties();
        }

        [MenuItem(MenuRoot + "Mobile/Add Camera Fitter To Main Camera", priority = 40)]
        public static void AddCameraFitterMenu()
        {
            EnsureCameraFitter(Camera.main);
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include) == null)
                Debug.LogWarning("No EventSystem found in the scene. UI input will not work until one is added.");
        }
    }
}
