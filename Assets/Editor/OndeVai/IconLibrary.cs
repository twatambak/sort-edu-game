using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace OndeVai.EditorTools
{
    /// <summary>
    /// Slices the white game icon sheet into named sprites on a 100px grid and loads them by name.
    /// Slicing only happens once; later calls reuse the existing sprites.
    /// </summary>
    public static class IconLibrary
    {
        public const string SheetPath = "Assets/Sprites/Game Icons/sheet_white2x.png";

        private const int CellSize = 100;
        private const int Padding = 2;

        // Grid positions (row from the top, column from the left) of the icons used by the UI.
        private static readonly (string Name, int Row, int Column)[] Icons =
        {
            ("icon_chart", 0, 1),
            ("icon_check", 1, 4),
            ("icon_info", 1, 3),
            ("icon_music", 1, 2),
            ("icon_music_off", 2, 2),
            ("icon_right", 2, 5),
            ("icon_people", 3, 2),
            ("icon_home", 3, 3),
            ("icon_left", 3, 5),
            ("icon_gear", 4, 3),
            ("icon_lock", 4, 5),
            ("icon_drag", 7, 0),
            ("icon_play", 10, 1),
            ("icon_trophy", 11, 0),
            ("icon_replay", 12, 1),
            ("icon_exit", 12, 3),
            ("icon_trash", 13, 0),
            ("icon_question", 13, 1),
            ("icon_plus", 16, 1),
            ("icon_star", 17, 0),
            ("icon_pause", 18, 1),
            ("icon_person", 19, 0),
            ("icon_cross", 19, 3),
            ("icon_sound", 19, 4)
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
            if (!IsSliced())
                Slice();

            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(SheetPath))
            {
                if (asset is Sprite sprite)
                    _cache[sprite.name] = sprite;
            }
        }

        private static bool IsSliced()
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(SheetPath))
            {
                if (asset is Sprite sprite && sprite.name == Icons[0].Name)
                    return true;
            }

            return false;
        }

        private static void Slice()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SheetPath) as TextureImporter;
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SheetPath);
            if (importer == null || texture == null)
            {
                Debug.LogError($"Icon sheet not found at {SheetPath}.");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            SpriteDataProviderFactories factories = new();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            int height = texture.height;
            List<SpriteRect> rects = new();
            List<SpriteNameFileIdPair> pairs = new();
            foreach ((string name, int row, int column) in Icons)
            {
                SpriteRect spriteRect = new()
                {
                    name = name,
                    rect = new Rect(column * CellSize + Padding, height - (row + 1) * CellSize + Padding, CellSize - Padding * 2, CellSize - Padding * 2),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = GUID.Generate()
                };
                rects.Add(spriteRect);
                pairs.Add(new SpriteNameFileIdPair(spriteRect.name, spriteRect.spriteID));
            }

            provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()?.SetNameFileIdPairs(pairs);
            provider.Apply();
            importer.SaveAndReimport();
            Debug.Log($"Sliced {rects.Count} icons in {SheetPath}.");
        }
    }
}
