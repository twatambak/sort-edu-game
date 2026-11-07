using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OndeVai.EditorTools.Mobile
{
    /// <summary>
    /// Writes a <see cref="MobileBuildProfile"/> into the Player Settings, and re-applies it before
    /// Android and iOS builds so local and CI builds always match the profile.
    /// </summary>
    public sealed class MobilePlayerSettings : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100;

        [MenuItem("Tools/Onde Vai/Mobile/Apply Mobile Player Settings", priority = 0)]
        public static void ApplyFromMenu()
        {
            Apply(MobileBuildProfile.LoadOrCreate());
            AssetDatabase.SaveAssets();
            Debug.Log("Mobile player settings applied. Commit ProjectSettings/ProjectSettings.asset.");
        }

        [MenuItem("Tools/Onde Vai/Mobile/Select Build Profile", priority = 1)]
        public static void SelectProfile()
        {
            MobileBuildProfile profile = MobileBuildProfile.LoadOrCreate();
            Selection.activeObject = profile;
            EditorGUIUtility.PingObject(profile);
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            BuildTarget target = report.summary.platform;
            if (target != BuildTarget.Android && target != BuildTarget.iOS)
                return;

            MobileBuildProfile profile = MobileBuildProfile.LoadOrCreate();
            if (profile.ApplyBeforeBuild)
                Apply(profile);
        }

        /// <summary>Applies every setting of the profile. Safe to call repeatedly.</summary>
        public static void Apply(MobileBuildProfile profile)
        {
            if (profile == null)
            {
                Debug.LogError("No mobile build profile to apply.");
                return;
            }

            if (string.IsNullOrWhiteSpace(profile.BundleIdentifier) || !profile.BundleIdentifier.Contains("."))
                Debug.LogError($"Bundle identifier '{profile.BundleIdentifier}' is not a valid reverse domain id.", profile);

            int buildNumber = profile.ResolveBuildNumber();
            PlayerSettings.productName = profile.ProductName;
            PlayerSettings.companyName = profile.CompanyName;
            PlayerSettings.bundleVersion = profile.Version;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, profile.BundleIdentifier);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, profile.BundleIdentifier);

            ApplyOrientation(profile.PortraitOnly);
            ApplyAndroid(profile, buildNumber);
            ApplyIOS(profile, buildNumber);

            if (profile.AppIcon != null)
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { profile.AppIcon }, IconKind.Any);
            else
                Debug.LogWarning("Mobile build profile has no app icon. The Unity default icon will be used.", profile);
        }

        private static void ApplyOrientation(bool portraitOnly)
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = true;
            PlayerSettings.allowedAutorotateToLandscapeLeft = !portraitOnly;
            PlayerSettings.allowedAutorotateToLandscapeRight = !portraitOnly;
            PlayerSettings.useAnimatedAutorotation = true;
        }

        private static void ApplyAndroid(MobileBuildProfile profile, int buildNumber)
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);
            // Never lower the version code: CI may already have set a higher one (e.g. from the run number).
            PlayerSettings.Android.bundleVersionCode = Mathf.Max(buildNumber, PlayerSettings.Android.bundleVersionCode);
            PlayerSettings.Android.minSdkVersion = profile.MinimumSdk;
            PlayerSettings.Android.targetSdkVersion = profile.TargetSdk;
            PlayerSettings.Android.targetArchitectures = profile.Architectures | AndroidArchitecture.ARM64;
            PlayerSettings.Android.startInFullscreen = true;
            PlayerSettings.Android.renderOutsideSafeArea = true;
        }

        private static void ApplyIOS(MobileBuildProfile profile, int buildNumber)
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS, ManagedStrippingLevel.Low);
            int currentIOSBuild = int.TryParse(PlayerSettings.iOS.buildNumber, out int parsed) ? parsed : 0;
            PlayerSettings.iOS.buildNumber = Mathf.Max(buildNumber, currentIOSBuild).ToString();
            PlayerSettings.iOS.targetOSVersionString = profile.MinimumIOSVersion;
            PlayerSettings.iOS.requiresFullScreen = true;
            PlayerSettings.statusBarHidden = true;
        }
    }
}
