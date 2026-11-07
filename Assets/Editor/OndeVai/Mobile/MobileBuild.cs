using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OndeVai.EditorTools.Mobile
{
    /// <summary>
    /// One click and command line builds for Android and iOS, driven by <see cref="MobileBuildProfile"/>.
    /// Command line: Unity -batchmode -quit -projectPath . -executeMethod OndeVai.EditorTools.Mobile.MobileBuild.BuildAndroidRelease
    /// </summary>
    public static class MobileBuild
    {
        private const string MenuRoot = "Tools/Onde Vai/Mobile/";

        [MenuItem(MenuRoot + "Build Android APK (Development)", priority = 20)]
        public static void BuildAndroidDevelopmentMenu()
        {
            if (ConfirmTargetSwitch(BuildTarget.Android))
                BuildAndroidDevelopment();
        }

        [MenuItem(MenuRoot + "Build Android App Bundle (Release)", priority = 21)]
        public static void BuildAndroidReleaseMenu()
        {
            if (ConfirmTargetSwitch(BuildTarget.Android))
                BuildAndroidRelease();
        }

        [MenuItem(MenuRoot + "Export iOS Xcode Project", priority = 22)]
        public static void BuildIOSMenu()
        {
            if (ConfirmTargetSwitch(BuildTarget.iOS))
                BuildIOS();
        }

        /// <summary>Debug-signed APK with the development flag, for testing on a device.</summary>
        public static void BuildAndroidDevelopment()
        {
            MobileBuildProfile profile = MobileBuildProfile.LoadOrCreate();
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.Android.useCustomKeystore = false;
            Build(profile, BuildTarget.Android, $"{FileSafeName(profile)}-{profile.Version}-dev.apk", BuildOptions.Development);
        }

        /// <summary>Signed App Bundle for Google Play. Fails when the keystore is not configured.</summary>
        public static void BuildAndroidRelease()
        {
            MobileBuildProfile profile = MobileBuildProfile.LoadOrCreate();
            if (!ConfigureKeystore(profile))
            {
                FailBuild("Android release build needs a keystore. Set KeystorePath and KeyAliasName in the mobile build profile and the ANDROID_KEYSTORE_PASS / ANDROID_KEYALIAS_PASS environment variables.");
                return;
            }

            EditorUserBuildSettings.buildAppBundle = true;
            Build(profile, BuildTarget.Android, $"{FileSafeName(profile)}-{profile.Version}-{profile.ResolveBuildNumber()}.aab", BuildOptions.None);
        }

        /// <summary>Exports an Xcode project. Building and signing the app is then done in Xcode on a Mac.</summary>
        public static void BuildIOS()
        {
            MobileBuildProfile profile = MobileBuildProfile.LoadOrCreate();
            Build(profile, BuildTarget.iOS, "iOS", BuildOptions.None);
        }

        private static void Build(MobileBuildProfile profile, BuildTarget target, string outputName, BuildOptions options)
        {
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0)
            {
                FailBuild("No enabled scenes in the build settings.");
                return;
            }

            string folder = Path.Combine(profile.OutputFolder, target.ToString());
            Directory.CreateDirectory(folder);
            string outputPath = Path.Combine(folder, outputName);

            BuildPlayerOptions buildOptions = new()
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                options = options
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                FailBuild($"{target} build failed with {summary.totalErrors} error(s). See the console for details.");
                return;
            }

            Debug.Log($"{target} build succeeded: {Path.GetFullPath(outputPath)} ({summary.totalSize / (1024f * 1024f):0.0} MB).");
            if (!Application.isBatchMode)
                EditorUtility.RevealInFinder(outputPath);
        }

        private static bool ConfigureKeystore(MobileBuildProfile profile)
        {
            string keystorePass = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PASS");
            string aliasPass = Environment.GetEnvironmentVariable("ANDROID_KEYALIAS_PASS");
            if (string.IsNullOrEmpty(profile.KeystorePath) || string.IsNullOrEmpty(profile.KeyAliasName) ||
                string.IsNullOrEmpty(keystorePass) || string.IsNullOrEmpty(aliasPass) || !File.Exists(profile.KeystorePath))
                return false;

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = Path.GetFullPath(profile.KeystorePath);
            PlayerSettings.Android.keystorePass = keystorePass;
            PlayerSettings.Android.keyaliasName = profile.KeyAliasName;
            PlayerSettings.Android.keyaliasPass = aliasPass;
            return true;
        }

        private static bool ConfirmTargetSwitch(BuildTarget target)
        {
            if (EditorUserBuildSettings.activeBuildTarget == target)
                return true;

            if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target))
            {
                EditorUtility.DisplayDialog("Onde Vai", $"{target} build support is not installed. Add it from the Unity Hub.", "OK");
                return false;
            }

            bool confirmed = EditorUtility.DisplayDialog("Onde Vai",
                $"Building for {target} switches the active platform from {EditorUserBuildSettings.activeBuildTarget}. This reimports assets and can take a while.",
                "Switch and build", "Cancel");

            return confirmed && EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(target), target);
        }

        private static string FileSafeName(MobileBuildProfile profile)
        {
            string name = new(profile.ProductName.Where(char.IsLetterOrDigit).ToArray());
            return string.IsNullOrEmpty(name) ? "Game" : name;
        }

        private static void FailBuild(string message)
        {
            Debug.LogError(message);
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
            else
                EditorUtility.DisplayDialog("Onde Vai", message, "OK");
        }
    }
}
