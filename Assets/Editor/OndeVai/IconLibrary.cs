using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace OndeVai.EditorTools
{
    /// <summary>
    /// Maps the project's pre-sliced white icon sheet to the semantic names used by the UI builder.
    /// </summary>
    public static class IconLibrary
    {
        public const string SheetPath = "Assets/Sprites/sheet_white1x.png";

        private static readonly (string Name, string AssetName)[] Icons =
        {
            ("icon_chart", "sheet_white1x_25"),
            ("icon_check", "sheet_white1x_9"),
            ("icon_info", "sheet_white1x_98"),
            ("icon_music", "sheet_white1x_5"),
            ("icon_music_off", "sheet_white1x_15"),
            ("icon_right", "sheet_white1x_96"),
            ("icon_people", "sheet_white1x_108"),
            ("icon_home", "sheet_white1x_8"),
            ("icon_left", "sheet_white1x_1"),
            ("icon_gear", "sheet_white1x_17"),
            ("icon_lock", "sheet_white1x_24"),
            ("icon_drag", "sheet_white1x_2"),
            ("icon_play", "sheet_white1x_88"),
            ("icon_trophy", "sheet_white1x_86"),
            ("icon_replay", "sheet_white1x_14"),
            ("icon_exit", "sheet_white1x_18"),
            ("icon_trash", "sheet_white1x_87"),
            ("icon_question", "sheet_white1x_26"),
            ("icon_plus", "sheet_white1x_62"),
            ("icon_star", "sheet_white1x_105"),
            ("icon_pause", "sheet_white1x_85"),
            ("icon_person", "sheet_white1x_13"),
            ("icon_cross", "sheet_white1x_104"),
            ("icon_sound", "sheet_white1x_70")
        };

        private static Dictionary<string, Sprite> _cache;

        /// <summary>Returns the icon sprite, slicing the sheet first when needed. Logs and returns null when missing.</summary>
        public static Sprite Get(string iconName)
        {
            if (_cache == null || _cache.Count == 0)
                Load();

            if (_cache.TryGetValue(iconName, out Sprite sprite) && sprite != null)
                return sprite;

            Debug.LogError($"Icon '{iconName}' was not found in {SheetPath}.");
            return null;
        }

        /// <summary>Forces the next <see cref="Get"/> to reload sprites from disk.</summary>
        public static void ClearCache()
        {
            _cache = null;
        }

        private static void Load()
        {
            _cache = new Dictionary<string, Sprite>();
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(SheetPath);
            if (assets.Length == 0)
            {
                Debug.LogError($"Icon sheet not found at {SheetPath}.");
                return;
            }

            Dictionary<string, Sprite> spritesByName = new();
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite)
                    spritesByName[sprite.name] = sprite;
            }

            foreach ((string name, string assetName) in Icons)
            {
                if (spritesByName.TryGetValue(assetName, out Sprite sprite))
                    _cache[name] = sprite;
                else
                    Debug.LogError($"Icon sprite '{assetName}' was not found in {SheetPath}.");
            }
        }
    }
}
