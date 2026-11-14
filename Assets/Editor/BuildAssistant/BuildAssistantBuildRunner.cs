using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BuildAssistant
{
    internal static class BuildAssistantBuildRunner
    {
        private const string PendingKey = "BuildAssistant.Pending";
        private const string SettingsPathKey = "BuildAssistant.SettingsPath";
        private const string PresetIndexKey = "BuildAssistant.PresetIndex";
        private const string ProfileRequestedKey = "BuildAssistant.ProfileRequested";
        private const string TargetSwitchRequestedKey = "BuildAssistant.TargetSwitchRequested";
        private const string MobileBuildKey = "BuildAssistant.MobileBuild";
        private const string MobileVersionKey = "BuildAssistant.MobileVersion";
        private const string MobileBuildNumberKey = "BuildAssistant.MobileBuildNumber";
        private const string ProfileTypeName = "UnityEditor.Build.Profile.BuildProfile";
        private const string WaitStartedKey = "BuildAssistant.WaitStarted";
        private const float SwitchTimeoutSeconds = 300f;

        private static bool waitingForEditor;

        public static bool IsBuildPending => SessionState.GetBool(PendingKey, false);

        [InitializeOnLoadMethod]
        private static void ResumePendingBuildAfterReload()
        {
            if (IsBuildPending)
                EditorApplication.delayCall += ProcessPendingBuild;
        }

        public static void RequestBuild(BuildAssistantSettings settings, int presetIndex)
        {
            if (settings == null || settings.Presets == null || presetIndex < 0 || presetIndex >= settings.Presets.Count)
            {
                Fail("O preset selecionado não está disponível.");
                return;
            }

            SessionState.SetString(SettingsPathKey, BuildAssistantSettings.AssetPath);
            SessionState.SetInt(PresetIndexKey, presetIndex);
            SessionState.SetBool(ProfileRequestedKey, false);
            SessionState.SetBool(TargetSwitchRequestedKey, false);
            SessionState.SetFloat(WaitStartedKey, (float)EditorApplication.timeSinceStartup);
            SessionState.SetBool(PendingKey, true);
            EditorApplication.delayCall += ProcessPendingBuild;
        }

        public static void CancelPendingBuild()
        {
            ClearPendingBuild();
            Debug.Log("Build Assistant: solicitação de build cancelada.");
        }

        public static bool ActivateProfile(BuildAssistantPreset preset)
        {
            if (preset == null || preset.UnityBuildProfile == null)
            {
                Fail("Atribua um Build Profile nativo do Unity ao preset.");
                return false;
            }

            if (!TrySetActiveBuildProfile(preset.UnityBuildProfile, out string error))
            {
                Fail(error);
                return false;
            }

            Debug.Log($"Build Assistant: perfil '{preset.UnityBuildProfile.name}' ativado.");
            return true;
        }

        private static void ProcessPendingBuild()
        {
            if (!IsBuildPending)
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                WaitForEditor();
                return;
            }

            BuildAssistantSettings settings =
                AssetDatabase.LoadAssetAtPath<BuildAssistantSettings>(SessionState.GetString(SettingsPathKey, string.Empty));
            int presetIndex = SessionState.GetInt(PresetIndexKey, -1);
            if (settings == null || settings.Presets == null || presetIndex < 0 || presetIndex >= settings.Presets.Count)
            {
                Fail("As configurações ou o preset da build pendente não foram encontrados.");
                ClearPendingBuild();
                return;
            }

            BuildAssistantPreset preset = settings.Presets[presetIndex];
            BuildTarget target = ToBuildTarget(preset.Target);
            BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(target);
            if (!BuildPipeline.IsBuildTargetSupported(targetGroup, target))
            {
                Fail($"O suporte para {preset.Target} não está instalado. Instale o módulo pelo Unity Hub.");
                ClearPendingBuild();
                return;
            }

            if (preset.UnityBuildProfile != null)
            {
                if (!IsBuildProfile(preset.UnityBuildProfile))
                {
                    Fail($"'{preset.UnityBuildProfile.name}' não é um Build Profile nativo do Unity.");
                    ClearPendingBuild();
                    return;
                }

                if (TryGetProfileTarget(preset.UnityBuildProfile, out BuildTarget profileTarget) &&
                    profileTarget != target)
                {
                    Fail($"O Build Profile selecionado é de {profileTarget}, mas o preset está configurado para {target}.");
                    ClearPendingBuild();
                    return;
                }

                if (GetActiveBuildProfile() != preset.UnityBuildProfile)
                {
                    if (!SessionState.GetBool(ProfileRequestedKey, false))
                    {
                        SessionState.SetBool(ProfileRequestedKey, true);
                        if (!TrySetActiveBuildProfile(preset.UnityBuildProfile, out string error))
                        {
                            Fail(error);
                            ClearPendingBuild();
                            return;
                        }
                    }

                    if (preset.UnityBuildProfile != null && GetActiveBuildProfile() != preset.UnityBuildProfile)
                    {
                        SessionState.SetBool(ProfileRequestedKey, false);
                        WaitForEditor();
                        return;
                    }

                    if (GetActiveBuildProfile() != preset.UnityBuildProfile)
                    {
                        WaitForEditor();
                        return;
                    }
                }
            }

            if (EditorUserBuildSettings.activeBuildTarget != target)
            {
                if (!SessionState.GetBool(TargetSwitchRequestedKey, false))
                {
                    SessionState.SetBool(TargetSwitchRequestedKey, true);
                    if (!EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, target))
                    {
                        Fail($"Não foi possível trocar a plataforma ativa para {preset.Target}.");
                        ClearPendingBuild();
                        return;
                    }
                }

                if (EditorUserBuildSettings.activeBuildTarget != target)
                {
                    WaitForEditor();
                    return;
                }
            }

            ClearPendingBuild();
            Build(settings, preset);
        }

        private static void WaitForEditor()
        {
            if (waitingForEditor)
                return;

            float waitStarted = SessionState.GetFloat(WaitStartedKey, (float)EditorApplication.timeSinceStartup);
            if (EditorApplication.timeSinceStartup - waitStarted > SwitchTimeoutSeconds)
            {
                Fail("A troca de plataforma ou perfil não foi concluída. Verifique o status do Unity e tente novamente.");
                ClearPendingBuild();
                return;
            }

            waitingForEditor = true;
            EditorApplication.update += CheckEditorReady;
        }

        private static void CheckEditorReady()
        {
            EditorApplication.update -= CheckEditorReady;
            waitingForEditor = false;
            if (!EditorApplication.isCompiling && !EditorApplication.isUpdating)
                ProcessPendingBuild();
            else
                WaitForEditor();
        }

        private static void Build(BuildAssistantSettings settings, BuildAssistantPreset preset)
        {
            if (string.IsNullOrWhiteSpace(settings.ProductName))
            {
                Fail("Informe o nome do produto nas configurações.");
                return;
            }

            if (!BuildAssistantSettings.TryParseVersion(settings.Version, out _, out _, out _))
            {
                Fail($"A versão '{settings.Version}' é inválida. Use o formato MAJOR.MINOR.PATCH, por exemplo 1.2.3.");
                return;
            }

            if (settings.AutoIncrementVersionOnBuild &&
                !BuildAssistantSettings.TryIncrementVersion(settings.Version, settings.Increment, out _))
            {
                Fail("A versão não pode ser incrementada. Revise os valores antes de iniciar a build.");
                return;
            }

            if (settings.AutoIncrementBuildNumberOnBuild && settings.BuildNumber == int.MaxValue)
            {
                Fail("O número de build atingiu o limite máximo.");
                return;
            }

            if (!TryGetScenes(preset, out string[] scenes, out string sceneError))
            {
                Fail(sceneError);
                return;
            }

            string outputPath;
            try
            {
                outputPath = GetOutputPath(settings, preset);
                string outputDirectory = preset.Target == BuildAssistantTarget.WebGL ||
                                         preset.Target == BuildAssistantTarget.iOS
                    ? outputPath
                    : Path.GetDirectoryName(outputPath);
                Directory.CreateDirectory(outputDirectory);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException ||
                                               exception is ArgumentException || exception is SecurityException)
            {
                Fail($"Não foi possível preparar a pasta de saída: {exception.Message}");
                return;
            }

            string builtVersion = settings.Version;
            int buildNumber = ResolveBuildNumber(settings, preset);
            if (settings.AutoIncrementBuildNumberOnBuild && buildNumber == int.MaxValue)
            {
                Fail("O número de build atual da plataforma atingiu o limite máximo.");
                return;
            }

            BuildOptions options = preset.Options;
            if (settings.DevelopmentBuild)
                options |= BuildOptions.Development;

            bool originalAndroidBundleSetting = EditorUserBuildSettings.buildAppBundle;
            if (preset.Target == BuildAssistantTarget.Android)
                EditorUserBuildSettings.buildAppBundle = preset.AndroidAppBundle;

            SessionState.SetBool(MobileBuildKey, true);
            SessionState.SetString(MobileVersionKey, builtVersion);
            SessionState.SetInt(MobileBuildNumberKey, buildNumber);

            try
            {
                PlayerSettings.productName = settings.ProductName;
                PlayerSettings.companyName = settings.CompanyName;
                PlayerSettings.bundleVersion = builtVersion;

                BuildPlayerOptions buildOptions = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = outputPath,
                    target = ToBuildTarget(preset.Target),
                    targetGroup = BuildPipeline.GetBuildTargetGroup(ToBuildTarget(preset.Target)),
                    options = options
                };

                BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
                BuildSummary summary = report.summary;
                if (summary.result != BuildResult.Succeeded)
                {
                    Fail($"A build para {preset.Target} falhou ({summary.totalErrors} erro(s)). Consulte o Console do Unity.");
                    return;
                }

                settings.BuildNumber = Mathf.Max(1, SessionState.GetInt(MobileBuildNumberKey, buildNumber));
                settings.RecordSuccessfulBuild(builtVersion);
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                Debug.Log($"Build Assistant: {preset.Target} concluída em '{outputPath}' " +
                          $"({summary.totalSize / (1024f * 1024f):0.0} MB), versão {builtVersion}.");
                BuildAssistantWindow.RepaintOpenWindows();
                if (!Application.isBatchMode)
                    EditorUtility.RevealInFinder(outputPath);
            }
            finally
            {
                SessionState.SetBool(MobileBuildKey, false);
                SessionState.EraseString(MobileVersionKey);
                SessionState.EraseInt(MobileBuildNumberKey);
                EditorUserBuildSettings.buildAppBundle = originalAndroidBundleSetting;
            }
        }

        private static bool TryGetScenes(
            BuildAssistantPreset preset,
            out string[] scenes,
            out string error)
        {
            scenes = null;
            error = null;
            var selectedScenes = new System.Collections.Generic.List<string>();

            if (preset.Scenes != null && preset.Scenes.Count > 0)
            {
                foreach (BuildAssistantScene entry in preset.Scenes)
                {
                    if (entry == null || !entry.Enabled)
                        continue;
                    if (entry.Scene == null)
                    {
                        error = $"O preset '{preset.Name}' contém uma cena habilitada sem referência.";
                        return false;
                    }

                    selectedScenes.Add(AssetDatabase.GetAssetPath(entry.Scene));
                }

                if (selectedScenes.Count == 0)
                {
                    error = $"O preset '{preset.Name}' não tem cenas habilitadas.";
                    return false;
                }
            }
            else if (!TryGetProfileScenes(preset.UnityBuildProfile, selectedScenes))
            {
                foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                {
                    if (scene.enabled)
                        selectedScenes.Add(scene.path);
                }
            }

            if (selectedScenes.Count == 0)
            {
                error = "Nenhuma cena está habilitada. Adicione cenas ao preset ou configure File > Build Profiles.";
                return false;
            }

            foreach (string scenePath in selectedScenes)
            {
                if (string.IsNullOrWhiteSpace(scenePath) || !File.Exists(scenePath))
                {
                    error = $"A cena '{scenePath}' não existe no projeto. Atualize as cenas do preset.";
                    return false;
                }
            }

            scenes = selectedScenes.ToArray();
            return true;
        }

        private static bool TryGetProfileScenes(ScriptableObject profile, System.Collections.Generic.List<string> scenes)
        {
            if (profile == null || !IsBuildProfile(profile))
                return false;

            Type profileType = profile.GetType();
            PropertyInfo overrideProperty = profileType.GetProperty("overrideGlobalScenes");
            PropertyInfo scenesProperty = profileType.GetProperty("scenes");
            if (overrideProperty == null || scenesProperty == null ||
                !(overrideProperty.GetValue(profile) is bool overrideScenes) || !overrideScenes)
                return false;

            if (!(scenesProperty.GetValue(profile) is IEnumerable profileScenes))
                return false;

            foreach (object scene in profileScenes)
            {
                if (scene == null)
                    continue;

                Type sceneType = scene.GetType();
                PropertyInfo enabledProperty = sceneType.GetProperty("enabled");
                PropertyInfo pathProperty = sceneType.GetProperty("path");
                if (enabledProperty == null || pathProperty == null)
                    return false;

                if (enabledProperty.GetValue(scene) is bool enabled && enabled &&
                    pathProperty.GetValue(scene) is string scenePath)
                    scenes.Add(scenePath);
            }

            return true;
        }

        private static string GetOutputPath(BuildAssistantSettings settings, BuildAssistantPreset preset)
        {
            string root = Path.GetFullPath(string.IsNullOrWhiteSpace(settings.BuildOutputDirectory)
                ? "Builds"
                : settings.BuildOutputDirectory);
            string folder = Path.Combine(root, preset.Target.ToString());
            string productName = MakeFileSafe(settings.ProductName);
            string presetName = MakeFileSafe(preset.Name);
            string baseName = $"{productName}_{settings.Version}_{presetName}";

            switch (preset.Target)
            {
                case BuildAssistantTarget.Windows:
                    return Path.Combine(folder, baseName + ".exe");
                case BuildAssistantTarget.Linux:
                    return Path.Combine(folder, baseName + ".x86_64");
                case BuildAssistantTarget.macOS:
                    return Path.Combine(folder, baseName + ".app");
                case BuildAssistantTarget.Android:
                    return Path.Combine(folder, baseName + (preset.AndroidAppBundle ? ".aab" : ".apk"));
                case BuildAssistantTarget.iOS:
                    return Path.Combine(folder, baseName + "_Xcode");
                case BuildAssistantTarget.WebGL:
                    return Path.Combine(folder, baseName + "_WebGL");
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset.Target), preset.Target, "Unsupported build target.");
            }
        }

        private static int ResolveBuildNumber(BuildAssistantSettings settings, BuildAssistantPreset preset)
        {
            int buildNumber = Mathf.Max(1, settings.BuildNumber);
            if (preset.Target == BuildAssistantTarget.Android)
                return Mathf.Max(buildNumber, PlayerSettings.Android.bundleVersionCode);

            if (preset.Target == BuildAssistantTarget.iOS &&
                int.TryParse(PlayerSettings.iOS.buildNumber, NumberStyles.None, CultureInfo.InvariantCulture,
                    out int iosBuildNumber))
                return Mathf.Max(buildNumber, iosBuildNumber);

            return buildNumber;
        }

        private static string MakeFileSafe(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Game";

            char[] invalid = Path.GetInvalidFileNameChars();
            var result = new System.Text.StringBuilder(value.Length);
            foreach (char character in value.Trim())
            {
                bool isInvalid = Array.IndexOf(invalid, character) >= 0 || character == '/' || character == '\\';
                if (!isInvalid)
                    result.Append(char.IsWhiteSpace(character) ? '_' : character);
            }

            return result.Length == 0 ? "Game" : result.ToString();
        }

        private static BuildTarget ToBuildTarget(BuildAssistantTarget target)
        {
            switch (target)
            {
                case BuildAssistantTarget.Windows: return BuildTarget.StandaloneWindows64;
                case BuildAssistantTarget.Linux: return BuildTarget.StandaloneLinux64;
                case BuildAssistantTarget.macOS: return BuildTarget.StandaloneOSX;
                case BuildAssistantTarget.Android: return BuildTarget.Android;
                case BuildAssistantTarget.iOS: return BuildTarget.iOS;
                case BuildAssistantTarget.WebGL: return BuildTarget.WebGL;
                default: throw new ArgumentOutOfRangeException(nameof(target), target, "Unsupported build target.");
            }
        }

        private static bool IsBuildProfile(ScriptableObject profile)
        {
            return profile != null && profile.GetType().FullName == ProfileTypeName;
        }

        private static ScriptableObject GetActiveBuildProfile()
        {
            Type profileType = typeof(EditorWindow).Assembly.GetType(ProfileTypeName);
            MethodInfo method = profileType?.GetMethod("GetActiveBuildProfile", BindingFlags.Public | BindingFlags.Static);
            return method?.Invoke(null, null) as ScriptableObject;
        }

        private static bool TryGetProfileTarget(ScriptableObject profile, out BuildTarget target)
        {
            target = default;
            var serializedProfile = new SerializedObject(profile);
            SerializedProperty targetProperty = serializedProfile.FindProperty("m_BuildTarget");
            if (targetProperty == null || !Enum.IsDefined(typeof(BuildTarget), targetProperty.intValue))
                return false;

            target = (BuildTarget)targetProperty.intValue;
            return true;
        }

        private static bool TrySetActiveBuildProfile(ScriptableObject profile, out string error)
        {
            error = null;
            if (!IsBuildProfile(profile))
            {
                error = "Selecione um Build Profile nativo do Unity 6.";
                return false;
            }

            Type profileType = profile.GetType();
            MethodInfo method = profileType.GetMethod("SetActiveBuildProfile", BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                error = "A API de Build Profiles não está disponível nesta versão do Unity.";
                return false;
            }

            try
            {
                method.Invoke(null, new object[] { profile });
                return true;
            }
            catch (TargetInvocationException exception)
            {
                error = exception.InnerException == null
                    ? exception.Message
                    : exception.InnerException.Message;
                return false;
            }
        }

        private static void ClearPendingBuild()
        {
            SessionState.SetBool(PendingKey, false);
            SessionState.EraseString(SettingsPathKey);
            SessionState.EraseInt(PresetIndexKey);
            SessionState.SetBool(ProfileRequestedKey, false);
            SessionState.SetBool(TargetSwitchRequestedKey, false);
            SessionState.SetFloat(WaitStartedKey, 0f);
        }

        private static void Fail(string message)
        {
            Debug.LogError($"Build Assistant: {message}");
            BuildAssistantWindow.RepaintOpenWindows();
            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Build Assistant", message, "OK");
        }

    }

    public sealed class BuildAssistantMobileVersionHook : IPreprocessBuildWithReport
    {
        public int callbackOrder => 10000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!SessionState.GetBool("BuildAssistant.MobileBuild", false))
                return;

            string version = SessionState.GetString("BuildAssistant.MobileVersion", string.Empty);
            int buildNumber = SessionState.GetInt("BuildAssistant.MobileBuildNumber", 1);
            BuildAssistantSettings settings =
                AssetDatabase.LoadAssetAtPath<BuildAssistantSettings>(BuildAssistantSettings.AssetPath);
            if (settings == null)
                throw new BuildFailedException("Build Assistant: as configurações de build não foram encontradas.");

            PlayerSettings.productName = settings.ProductName;
            PlayerSettings.companyName = settings.CompanyName;
            PlayerSettings.bundleVersion = version;

            if (report.summary.platform == BuildTarget.Android)
            {
                buildNumber = Mathf.Max(buildNumber, PlayerSettings.Android.bundleVersionCode);
                PlayerSettings.Android.bundleVersionCode = buildNumber;
            }
            else if (report.summary.platform == BuildTarget.iOS)
            {
                if (int.TryParse(PlayerSettings.iOS.buildNumber, NumberStyles.None, CultureInfo.InvariantCulture,
                        out int currentBuildNumber))
                    buildNumber = Mathf.Max(buildNumber, currentBuildNumber);
                PlayerSettings.iOS.buildNumber = buildNumber.ToString(CultureInfo.InvariantCulture);
            }

            if (settings.AutoIncrementBuildNumberOnBuild && buildNumber == int.MaxValue)
                throw new BuildFailedException("Build Assistant: o número de build da plataforma atingiu o limite máximo.");

            SessionState.SetInt("BuildAssistant.MobileBuildNumber", buildNumber);
        }
    }
}
