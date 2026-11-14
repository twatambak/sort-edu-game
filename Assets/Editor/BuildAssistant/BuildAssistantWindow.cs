using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BuildAssistant
{
    /// <summary>Editor window for configuring presets, versions, changelog and player builds.</summary>
    public sealed class BuildAssistantWindow : EditorWindow
    {
        private enum WindowTab
        {
            Builds,
            Versioning,
            Settings
        }

        private const string ProfileTypeName = "UnityEditor.Build.Profile.BuildProfile";

        private BuildAssistantSettings settings;
        private SerializedObject serializedSettings;
        private WindowTab selectedTab;
        private Vector2 scrollPosition;

        /// <summary>Opens the Build Assistant from the Unity Tools menu.</summary>
        [MenuItem("Tools/Build Assistant", priority = 1)]
        public static void Open()
        {
            BuildAssistantWindow window = GetWindow<BuildAssistantWindow>();
            window.titleContent = new GUIContent("Build Assistant");
            window.minSize = new Vector2(560f, 450f);
            window.Show();
        }

        private void OnEnable()
        {
            LoadSettings();
        }

        private void OnGUI()
        {
            if (settings == null || serializedSettings == null)
                LoadSettings();

            serializedSettings.Update();
            DrawHeader();
            DrawToolbar();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            using (new EditorGUI.DisabledScope(BuildAssistantBuildRunner.IsBuildPending))
            {
                switch (selectedTab)
                {
                    case WindowTab.Builds:
                        DrawBuilds();
                        break;
                    case WindowTab.Versioning:
                        DrawVersioning();
                        break;
                    case WindowTab.Settings:
                        DrawSettings();
                        break;
                }
            }
            EditorGUILayout.EndScrollView();

            bool changed = serializedSettings.ApplyModifiedProperties();
            if (changed)
                SaveSettings();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Build Assistant", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"Versão {settings.Version}  |  Build {settings.BuildNumber}",
                EditorStyles.miniLabel, GUILayout.Width(190f));
            EditorGUILayout.EndHorizontal();

            if (BuildAssistantBuildRunner.IsBuildPending)
            {
                EditorGUILayout.HelpBox("Preparando a plataforma ou o Build Profile para iniciar a build.",
                    MessageType.Info);
                if (GUILayout.Button("Cancelar build pendente", GUILayout.Width(180f)))
                    BuildAssistantBuildRunner.CancelPendingBuild();
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            DrawTabButton(WindowTab.Builds, "Builds");
            DrawTabButton(WindowTab.Versioning, "Versão e changelog");
            DrawTabButton(WindowTab.Settings, "Configurações");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8f);
        }

        private void DrawTabButton(WindowTab tab, string label)
        {
            bool selected = selectedTab == tab;
            if (GUILayout.Toggle(selected, label, EditorStyles.toolbarButton) && !selected)
                selectedTab = tab;
        }

        private void DrawBuilds()
        {
            EditorGUILayout.LabelField("Builds rápidas", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Cada preset guarda a plataforma, as cenas, o Build Profile e as opções da build.",
                MessageType.None);

            SerializedProperty presets = serializedSettings.FindProperty("Presets");
            for (int i = 0; i < presets.arraySize; i++)
                DrawPreset(presets.GetArrayElementAtIndex(i), i);

            EditorGUILayout.Space(4f);
            if (GUILayout.Button("Adicionar preset"))
                ShowAddPresetMenu();
        }

        private void DrawPreset(SerializedProperty preset, int index)
        {
            SerializedProperty expanded = preset.FindPropertyRelative("Expanded");
            SerializedProperty name = preset.FindPropertyRelative("Name");
            SerializedProperty target = preset.FindPropertyRelative("Target");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            expanded.boolValue = EditorGUILayout.Foldout(expanded.boolValue,
                $"{name.stringValue} ({target.enumDisplayNames[Mathf.Clamp(target.enumValueIndex, 0, target.enumDisplayNames.Length - 1)]})",
                true);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Build", GUILayout.Width(64f)))
                RequestBuild(index);

            if (GUILayout.Button("...", GUILayout.Width(30f)))
                ShowPresetMenu(index);
            EditorGUILayout.EndHorizontal();

            if (expanded.boolValue)
                DrawPresetDetails(preset, index);
            EditorGUILayout.EndVertical();
        }

        private void DrawPresetDetails(SerializedProperty preset, int index)
        {
            SerializedProperty target = preset.FindPropertyRelative("Target");
            SerializedProperty profile = preset.FindPropertyRelative("UnityBuildProfile");
            SerializedProperty options = preset.FindPropertyRelative("Options");
            SerializedProperty scenes = preset.FindPropertyRelative("Scenes");

            EditorGUILayout.PropertyField(preset.FindPropertyRelative("Name"), new GUIContent("Nome"));
            EditorGUILayout.PropertyField(target, new GUIContent("Plataforma"));
            EditorGUILayout.PropertyField(profile, new GUIContent("Build Profile do Unity"));

            UnityEngine.Object profileObject = profile.objectReferenceValue;
            bool hasNativeProfile = profileObject != null && profileObject.GetType().FullName == ProfileTypeName;
            if (profileObject != null && !hasNativeProfile)
                EditorGUILayout.HelpBox("Selecione um Build Profile nativo do Unity 6.", MessageType.Error);
            else if (hasNativeProfile)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(profileObject.name, EditorStyles.miniLabel);
                if (GUILayout.Button("Ativar perfil", GUILayout.Width(95f)))
                    ActivateProfile(index);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.PropertyField(options, new GUIContent("Opções da build"));
            if ((BuildAssistantTarget)target.enumValueIndex == BuildAssistantTarget.Android)
                EditorGUILayout.PropertyField(preset.FindPropertyRelative("AndroidAppBundle"),
                    new GUIContent("Gerar App Bundle (.aab)"));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Cenas", EditorStyles.boldLabel);
            if (scenes.arraySize == 0)
                EditorGUILayout.HelpBox("Sem cenas próprias: serão usadas as cenas habilitadas no Build Profile ou em Build Settings.",
                    MessageType.Info);

            int sceneToRemove = -1;
            for (int i = 0; i < scenes.arraySize; i++)
            {
                SerializedProperty sceneEntry = scenes.GetArrayElementAtIndex(i);
                SerializedProperty sceneAsset = sceneEntry.FindPropertyRelative("Scene");
                SerializedProperty enabled = sceneEntry.FindPropertyRelative("Enabled");

                EditorGUILayout.BeginHorizontal();
                enabled.boolValue = EditorGUILayout.Toggle(enabled.boolValue, GUILayout.Width(18f));
                EditorGUILayout.PropertyField(sceneAsset, GUIContent.none);
                if (GUILayout.Button("^", GUILayout.Width(24f)) && i > 0)
                    scenes.MoveArrayElement(i, i - 1);
                if (GUILayout.Button("v", GUILayout.Width(24f)) && i < scenes.arraySize - 1)
                    scenes.MoveArrayElement(i, i + 1);
                if (GUILayout.Button("X", GUILayout.Width(24f)))
                    sceneToRemove = i;
                EditorGUILayout.EndHorizontal();
            }

            if (sceneToRemove >= 0)
                scenes.DeleteArrayElementAtIndex(sceneToRemove);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Adicionar cena"))
                AddScene(scenes);
            if (GUILayout.Button("Importar Build Settings", GUILayout.Width(170f)))
                ImportScenes(index);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawVersioning()
        {
            SerializedProperty version = serializedSettings.FindProperty("Version");
            EditorGUILayout.LabelField("Controle de versão", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(version, new GUIContent("Versão (MAJOR.MINOR.PATCH)"));
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("BuildNumber"),
                new GUIContent("Número de build"));

            if (!BuildAssistantSettings.TryParseVersion(version.stringValue, out _, out _, out _))
                EditorGUILayout.HelpBox("Use três números separados por ponto, por exemplo 1.2.3.",
                    MessageType.Error);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Incrementar patch"))
                BumpVersion(VersionIncrement.Patch);
            if (GUILayout.Button("Incrementar minor"))
                BumpVersion(VersionIncrement.Minor);
            if (GUILayout.Button("Incrementar major"))
                BumpVersion(VersionIncrement.Major);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Incremento automático após build bem-sucedida", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("AutoIncrementVersionOnBuild"),
                new GUIContent("Incrementar versão"));
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("Increment"),
                new GUIContent("Tipo de incremento"));
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("AutoIncrementBuildNumberOnBuild"),
                new GUIContent("Incrementar número de build"));

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Próximo changelog", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("ChangelogDraft"),
                new GUIContent("Notas"), GUILayout.MinHeight(88f));
            if (GUILayout.Button("Registrar notas na versão atual", GUILayout.Width(230f)))
                AddChangelogEntry();

            EditorGUILayout.Space(12f);
            DrawChangelogHistory();
        }

        private void DrawChangelogHistory()
        {
            EditorGUILayout.LabelField("Histórico", EditorStyles.boldLabel);
            SerializedProperty changelog = serializedSettings.FindProperty("Changelog");
            int entryToRemove = -1;
            for (int i = 0; i < changelog.arraySize; i++)
            {
                SerializedProperty entry = changelog.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    $"{entry.FindPropertyRelative("Version").stringValue}  |  {entry.FindPropertyRelative("Date").stringValue}",
                    EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Remover", GUILayout.Width(70f)))
                    entryToRemove = i;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField(entry.FindPropertyRelative("Notes").stringValue, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndVertical();
            }

            if (entryToRemove >= 0)
                changelog.DeleteArrayElementAtIndex(entryToRemove);
            if (changelog.arraySize == 0)
                EditorGUILayout.LabelField("Nenhuma nota registrada.", EditorStyles.miniLabel);
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Projeto", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("ProductName"),
                new GUIContent("Nome do produto"));
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("CompanyName"),
                new GUIContent("Empresa"));

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Destino", EditorStyles.boldLabel);
            SerializedProperty outputDirectory = serializedSettings.FindProperty("BuildOutputDirectory");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(outputDirectory, new GUIContent("Pasta de saída"));
            if (GUILayout.Button("Selecionar", GUILayout.Width(85f)))
                BrowseOutputDirectory();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("O caminho pode ser relativo à pasta do projeto ou absoluto.", MessageType.None);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Opções", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("DevelopmentBuild"),
                new GUIContent("Development Build para todas as plataformas"));
        }

        private void AddScene(SerializedProperty scenes)
        {
            int index = scenes.arraySize;
            scenes.InsertArrayElementAtIndex(index);
            SerializedProperty entry = scenes.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("Scene").objectReferenceValue = null;
            entry.FindPropertyRelative("Enabled").boolValue = true;
        }

        private void ShowAddPresetMenu()
        {
            GenericMenu menu = new GenericMenu();
            foreach (BuildAssistantTarget target in Enum.GetValues(typeof(BuildAssistantTarget)))
            {
                BuildAssistantTarget capturedTarget = target;
                menu.AddItem(new GUIContent(capturedTarget.ToString()), false, () => AddPreset(capturedTarget));
            }
            menu.ShowAsContext();
        }

        private void ShowPresetMenu(int index)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Duplicar"), false, () => DuplicatePreset(index));
            menu.AddItem(new GUIContent("Remover"), false, () => RemovePreset(index));
            menu.ShowAsContext();
        }

        private void AddPreset(BuildAssistantTarget target)
        {
            Undo.RecordObject(settings, "Adicionar preset de build");
            settings.AddPreset(target);
            SaveSettings();
        }

        private void DuplicatePreset(int index)
        {
            if (index < 0 || index >= settings.Presets.Count)
                return;

            Undo.RecordObject(settings, "Duplicar preset de build");
            BuildAssistantPreset original = settings.Presets[index];
            var duplicate = new BuildAssistantPreset
            {
                Name = original.Name + " (Cópia)",
                Target = original.Target,
                UnityBuildProfile = original.UnityBuildProfile,
                Options = original.Options,
                AndroidAppBundle = original.AndroidAppBundle
            };
            if (original.Scenes != null)
            {
                foreach (BuildAssistantScene scene in original.Scenes)
                {
                    if (scene == null)
                        continue;
                    duplicate.Scenes.Add(new BuildAssistantScene
                    {
                        Scene = scene.Scene,
                        Enabled = scene.Enabled
                    });
                }
            }

            settings.Presets.Insert(index + 1, duplicate);
            SaveSettings();
        }

        private void RemovePreset(int index)
        {
            if (index < 0 || index >= settings.Presets.Count ||
                !EditorUtility.DisplayDialog("Remover preset",
                    $"Remover o preset '{settings.Presets[index].Name}'?", "Remover", "Cancelar"))
                return;

            Undo.RecordObject(settings, "Remover preset de build");
            settings.Presets.RemoveAt(index);
            SaveSettings();
        }

        private void RequestBuild(int index)
        {
            serializedSettings.ApplyModifiedProperties();
            SaveSettings();
            BuildAssistantBuildRunner.RequestBuild(settings, index);
            Repaint();
        }

        private void ActivateProfile(int index)
        {
            serializedSettings.ApplyModifiedProperties();
            if (index >= 0 && index < settings.Presets.Count)
                BuildAssistantBuildRunner.ActivateProfile(settings.Presets[index]);
        }

        private void ImportScenes(int index)
        {
            serializedSettings.ApplyModifiedProperties();
            if (index < 0 || index >= settings.Presets.Count)
                return;

            Undo.RecordObject(settings, "Importar cenas de Build Settings");
            settings.ImportProjectScenes(settings.Presets[index]);
            SaveSettings();
        }

        private void BumpVersion(VersionIncrement increment)
        {
            serializedSettings.ApplyModifiedProperties();
            if (!BuildAssistantSettings.TryIncrementVersion(settings.Version, increment, out string nextVersion))
            {
                EditorUtility.DisplayDialog("Versão inválida",
                    "Não foi possível incrementar a versão. Use MAJOR.MINOR.PATCH.", "OK");
                return;
            }

            Undo.RecordObject(settings, "Incrementar versão");
            settings.Version = nextVersion;
            SaveSettings();
        }

        private void AddChangelogEntry()
        {
            serializedSettings.ApplyModifiedProperties();
            if (string.IsNullOrWhiteSpace(settings.ChangelogDraft))
            {
                EditorUtility.DisplayDialog("Changelog vazio", "Escreva as notas antes de registrá-las.", "OK");
                return;
            }

            Undo.RecordObject(settings, "Registrar changelog");
            if (settings.Changelog == null)
                settings.Changelog = new System.Collections.Generic.List<BuildChangelogEntry>();
            settings.Changelog.Insert(0, new BuildChangelogEntry
            {
                Version = settings.Version,
                Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                Notes = settings.ChangelogDraft.Trim()
            });
            settings.ChangelogDraft = string.Empty;
            SaveSettings();
        }

        private void BrowseOutputDirectory()
        {
            string currentPath = Path.GetFullPath(string.IsNullOrWhiteSpace(settings.BuildOutputDirectory)
                ? "Builds"
                : settings.BuildOutputDirectory);
            string selectedPath = EditorUtility.OpenFolderPanel("Pasta de saída das builds", currentPath, string.Empty);
            if (string.IsNullOrEmpty(selectedPath))
                return;

            serializedSettings.ApplyModifiedProperties();
            string projectRoot = Path.GetFullPath(".").TrimEnd(Path.DirectorySeparatorChar) +
                                 Path.DirectorySeparatorChar;
            string normalizedPath = Path.GetFullPath(selectedPath);
            string storedPath = normalizedPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase)
                ? normalizedPath.Substring(projectRoot.Length).Replace('\\', '/')
                : normalizedPath;

            Undo.RecordObject(settings, "Alterar pasta de saída");
            settings.BuildOutputDirectory = storedPath;
            SaveSettings();
        }

        private void LoadSettings()
        {
            settings = BuildAssistantSettings.LoadOrCreate();
            serializedSettings = new SerializedObject(settings);
        }

        internal static void RepaintOpenWindows()
        {
            foreach (BuildAssistantWindow window in Resources.FindObjectsOfTypeAll<BuildAssistantWindow>())
                window.Repaint();
        }

        private void SaveSettings()
        {
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            serializedSettings.Update();
            Repaint();
        }
    }
}
