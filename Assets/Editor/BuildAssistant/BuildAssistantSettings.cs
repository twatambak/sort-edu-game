using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace BuildAssistant
{
    /// <summary>Supported player platforms in the Build Assistant.</summary>
    public enum BuildAssistantTarget
    {
        Windows,
        Linux,
        macOS,
        Android,
        iOS,
        WebGL
    }

    /// <summary>Semantic version component to increment after a successful build.</summary>
    public enum VersionIncrement
    {
        Patch,
        Minor,
        Major
    }

    [Serializable]
    /// <summary>Scene inclusion state stored by a build preset.</summary>
    public sealed class BuildAssistantScene
    {
        public SceneAsset Scene;
        public bool Enabled = true;
    }

    [Serializable]
    /// <summary>Platform, profile, scene list and build options for a quick build.</summary>
    public sealed class BuildAssistantPreset
    {
        public string Name = "Novo preset";
        public BuildAssistantTarget Target;
        public ScriptableObject UnityBuildProfile;
        public List<BuildAssistantScene> Scenes = new List<BuildAssistantScene>();
        public BuildOptions Options = BuildOptions.CompressWithLz4;
        public bool AndroidAppBundle;
        public bool Expanded;
    }

    [Serializable]
    /// <summary>Release note entry associated with a built version.</summary>
    public sealed class BuildChangelogEntry
    {
        public string Version;
        public string Date;
        [TextArea(2, 8)] public string Notes;
    }

    /// <summary>Project-wide build presets, version data and release notes.</summary>
    public sealed class BuildAssistantSettings : ScriptableObject
    {
        public const string AssetPath = "Assets/Editor/BuildAssistant/BuildAssistantSettings.asset";

        [Header("Projeto")]
        public string ProductName = "Game";
        public string CompanyName = "Company";
        public string Version = "1.0.0";
        [Min(1)] public int BuildNumber = 1;
        public string BuildOutputDirectory = "Builds";

        [Header("Versionamento")]
        public bool AutoIncrementVersionOnBuild = true;
        public bool AutoIncrementBuildNumberOnBuild = true;
        public VersionIncrement Increment = VersionIncrement.Patch;
        [TextArea(4, 12)] public string ChangelogDraft;
        public List<BuildChangelogEntry> Changelog = new List<BuildChangelogEntry>();

        [Header("Build")]
        public bool DevelopmentBuild;
        public List<BuildAssistantPreset> Presets = new List<BuildAssistantPreset>();

        /// <summary>Loads the project settings asset or creates it with common platform presets.</summary>
        public static BuildAssistantSettings LoadOrCreate()
        {
            BuildAssistantSettings settings = AssetDatabase.LoadAssetAtPath<BuildAssistantSettings>(AssetPath);
            if (settings != null)
            {
                if (settings.Presets == null)
                    settings.Presets = new List<BuildAssistantPreset>();
                if (settings.Changelog == null)
                    settings.Changelog = new List<BuildChangelogEntry>();
                foreach (BuildAssistantPreset preset in settings.Presets)
                {
                    if (preset != null && preset.Scenes == null)
                        preset.Scenes = new List<BuildAssistantScene>();
                }
                return settings;
            }

            EnsureAssetFolder("Assets/Editor");
            EnsureAssetFolder("Assets/Editor/BuildAssistant");

            settings = CreateInstance<BuildAssistantSettings>();
            settings.ProductName = PlayerSettings.productName;
            settings.CompanyName = PlayerSettings.companyName;
            if (TryParseVersion(PlayerSettings.bundleVersion, out _, out _, out _))
                settings.Version = PlayerSettings.bundleVersion;

            settings.BuildNumber = 1;
            PopulateDefaultPresets(settings);
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Build Assistant settings created at {AssetPath}.", settings);
            return settings;
        }

        /// <summary>Adds a quick-build preset for the selected target.</summary>
        public void AddPreset(BuildAssistantTarget target)
        {
            if (Presets == null)
                Presets = new List<BuildAssistantPreset>();
            Presets.Add(new BuildAssistantPreset
            {
                Name = $"{target} Build",
                Target = target
            });
        }

        /// <summary>Copies the current Build Settings scene list into a preset for independent editing.</summary>
        public void ImportProjectScenes(BuildAssistantPreset preset)
        {
            if (preset == null)
                return;

            if (preset.Scenes == null)
                preset.Scenes = new List<BuildAssistantScene>();
            preset.Scenes.Clear();
            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
            {
                SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(buildScene.path);
                if (sceneAsset == null)
                    continue;

                preset.Scenes.Add(new BuildAssistantScene
                {
                    Scene = sceneAsset,
                    Enabled = buildScene.enabled
                });
            }
        }

        /// <summary>Records the draft notes and advances configured counters after a successful build.</summary>
        public void RecordSuccessfulBuild(string builtVersion)
        {
            if (!string.IsNullOrWhiteSpace(ChangelogDraft))
            {
                if (Changelog == null)
                    Changelog = new List<BuildChangelogEntry>();
                Changelog.Insert(0, new BuildChangelogEntry
                {
                    Version = builtVersion,
                    Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                    Notes = ChangelogDraft.Trim()
                });
                ChangelogDraft = string.Empty;
            }

            if (AutoIncrementVersionOnBuild)
            {
                if (!TryIncrementVersion(Version, Increment, out string nextVersion))
                    throw new InvalidOperationException($"Version '{Version}' cannot be incremented.");
                Version = nextVersion;
            }

            if (AutoIncrementBuildNumberOnBuild)
            {
                if (BuildNumber == int.MaxValue)
                    throw new InvalidOperationException("The build number has reached its maximum value.");
                BuildNumber++;
            }
        }

        /// <summary>Parses a three-component numeric semantic version.</summary>
        public static bool TryParseVersion(string version, out int major, out int minor, out int patch)
        {
            major = 0;
            minor = 0;
            patch = 0;
            if (string.IsNullOrWhiteSpace(version))
                return false;

            string[] parts = version.Split('.');
            return parts.Length == 3 &&
                   TryParseVersionPart(parts[0], out major) &&
                   TryParseVersionPart(parts[1], out minor) &&
                   TryParseVersionPart(parts[2], out patch);
        }

        /// <summary>Increments one numeric semantic version component and resets lower components.</summary>
        public static bool TryIncrementVersion(string version, VersionIncrement increment, out string result)
        {
            result = null;
            if (!Enum.IsDefined(typeof(VersionIncrement), increment) ||
                !TryParseVersion(version, out int major, out int minor, out int patch))
                return false;

            switch (increment)
            {
                case VersionIncrement.Major:
                    if (major == int.MaxValue)
                        return false;
                    major++;
                    minor = 0;
                    patch = 0;
                    break;
                case VersionIncrement.Minor:
                    if (minor == int.MaxValue)
                        return false;
                    minor++;
                    patch = 0;
                    break;
                default:
                    if (patch == int.MaxValue)
                        return false;
                    patch++;
                    break;
            }

            result = string.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}", major, minor, patch);
            return true;
        }

        private static bool TryParseVersionPart(string part, out int value)
        {
            return int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value >= 0;
        }

        private static void PopulateDefaultPresets(BuildAssistantSettings settings)
        {
            settings.Presets = new List<BuildAssistantPreset>
            {
                new BuildAssistantPreset { Name = "Windows", Target = BuildAssistantTarget.Windows },
                new BuildAssistantPreset { Name = "Linux", Target = BuildAssistantTarget.Linux },
                new BuildAssistantPreset { Name = "macOS", Target = BuildAssistantTarget.macOS },
                new BuildAssistantPreset { Name = "Android APK", Target = BuildAssistantTarget.Android },
                new BuildAssistantPreset { Name = "iOS Xcode", Target = BuildAssistantTarget.iOS },
                new BuildAssistantPreset { Name = "WebGL", Target = BuildAssistantTarget.WebGL }
            };
        }

        private static void EnsureAssetFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            int separator = folder.LastIndexOf('/');
            string parent = folder.Substring(0, separator);
            string name = folder.Substring(separator + 1);
            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
