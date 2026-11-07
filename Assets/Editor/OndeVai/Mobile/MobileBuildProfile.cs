using UnityEditor;
using UnityEngine;

namespace OndeVai.EditorTools.Mobile
{
    /// <summary>
    /// Every mobile store and player setting in one editable asset. Applied to the Player Settings by
    /// <see cref="MobilePlayerSettings"/> from the menu and automatically before Android and iOS builds.
    /// Signing passwords are never stored here; they come from environment variables.
    /// </summary>
    public sealed class MobileBuildProfile : ScriptableObject
    {
        public const string AssetPath = "Assets/Editor/OndeVai/Mobile/MobileBuildProfile.asset";

        [Header("App")]
        public string ProductName = "Onde Vai?";
        public string CompanyName = "TLW";
        [Tooltip("Reverse domain id used by both stores. Cannot change after the first store release.")]
        public string BundleIdentifier = "com.tlw.ondevai";
        [Tooltip("Version shown to users (e.g. 1.2.0).")]
        public string Version = "0.1.0";
        [Tooltip("Integer that must grow with every store upload (Android versionCode, iOS build number). Overridden by the BUILD_NUMBER environment variable in CI.")]
        [Min(1)] public int BuildNumber = 1;
        [Tooltip("Optional square icon (1024x1024 recommended). Uses the Unity default when empty.")]
        public Texture2D AppIcon;

        [Header("Behavior")]
        [Tooltip("The scene and UI are designed for portrait. Landscape would crop or stretch the layout.")]
        public bool PortraitOnly = true;
        [Tooltip("Applies these settings automatically before every Android or iOS build.")]
        public bool ApplyBeforeBuild = true;

        [Header("Android")]
        public AndroidSdkVersions MinimumSdk = AndroidSdkVersions.AndroidApiLevel25;
        public AndroidSdkVersions TargetSdk = AndroidSdkVersions.AndroidApiLevelAuto;
        [Tooltip("ARM64 is required by Google Play. ARMv7 keeps older, cheaper tablets supported.")]
        public AndroidArchitecture Architectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
        [Tooltip("Keystore path relative to the project folder. Passwords come from ANDROID_KEYSTORE_PASS and ANDROID_KEYALIAS_PASS.")]
        public string KeystorePath = "";
        public string KeyAliasName = "";

        [Header("iOS")]
        public string MinimumIOSVersion = "15.0";

        [Header("Output")]
        [Tooltip("Folder, relative to the project, where builds are written.")]
        public string OutputFolder = "Builds";

        /// <summary>Loads the profile, creating it with defaults when missing.</summary>
        public static MobileBuildProfile LoadOrCreate()
        {
            MobileBuildProfile profile = AssetDatabase.LoadAssetAtPath<MobileBuildProfile>(AssetPath);
            if (profile != null)
                return profile;

            profile = CreateInstance<MobileBuildProfile>();
            AssetDatabase.CreateAsset(profile, AssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created the mobile build profile at {AssetPath}. Commit it so CI uses the same settings.", profile);
            return profile;
        }

        /// <summary>Build number from the BUILD_NUMBER environment variable when set, else the profile value.</summary>
        public int ResolveBuildNumber()
        {
            string fromEnvironment = System.Environment.GetEnvironmentVariable("BUILD_NUMBER");
            return int.TryParse(fromEnvironment, out int number) && number > 0 ? number : BuildNumber;
        }
    }
}
