using System.Collections.Generic;
using System.IO;
using OndeVai.Gameplay;
using UnityEditor;
using UnityEngine;

namespace OndeVai.EditorTools
{
    /// <summary>Shared helpers for the content inspectors and menus.</summary>
    public static class ContentEditorUtility
    {
        public const string ItemTypesFolder = "Assets/Data/ItemTypes";
        public const string ScenariosFolder = "Assets/Data/Scenarios";
        private const string MenuRoot = "Tools/Onde Vai/Content/";

        /// <summary>Draws a sprite (including sprites packed in a sheet) scaled to fit the rect.</summary>
        public static void DrawSprite(Rect rect, Sprite sprite)
        {
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.15f));
            if (sprite == null || sprite.texture == null)
                return;

            Rect textureRect = sprite.textureRect;
            Texture2D texture = sprite.texture;
            Rect uv = new(textureRect.x / texture.width, textureRect.y / texture.height, textureRect.width / texture.width, textureRect.height / texture.height);

            float aspect = textureRect.width / Mathf.Max(1f, textureRect.height);
            Rect fitted = rect;
            if (aspect > 1f)
            {
                fitted.height = rect.width / aspect;
                fitted.y += (rect.height - fitted.height) * 0.5f;
            }
            else
            {
                fitted.width = rect.height * aspect;
                fitted.x += (rect.width - fitted.width) * 0.5f;
            }

            GUI.DrawTextureWithTexCoords(fitted, texture, uv, true);
        }

        /// <summary>Collects sprites from dragged or selected objects. Textures contribute all of their sprites.</summary>
        public static List<Sprite> CollectSprites(IEnumerable<Object> objects)
        {
            List<Sprite> sprites = new();
            foreach (Object obj in objects)
            {
                if (obj is Sprite sprite)
                {
                    sprites.Add(sprite);
                    continue;
                }

                if (obj is not Texture2D)
                    continue;

                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(obj)))
                {
                    if (asset is Sprite textureSprite)
                        sprites.Add(textureSprite);
                }
            }

            sprites.Sort((a, b) => EditorUtility.NaturalCompare(a.name, b.name));
            return sprites;
        }

        /// <summary>Finds the GameDefinition asset of the project, or null.</summary>
        public static GameDefinition FindGameDefinition()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:GameDefinition"))
            {
                GameDefinition definition = AssetDatabase.LoadAssetAtPath<GameDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (definition != null)
                    return definition;
            }

            return null;
        }

        /// <summary>Creates a new asset in <paramref name="folder"/> with a unique name and registers it in the definition.</summary>
        public static T CreateContentAsset<T>(string folder, string assetName, GameDefinition definition) where T : ScriptableObject
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }

            T asset = ScriptableObject.CreateInstance<T>();
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{assetName}.asset");
            AssetDatabase.CreateAsset(asset, path);

            if (definition != null)
            {
                Undo.RecordObject(definition, $"Add {typeof(T).Name}");
                if (asset is ItemTypeDefinition type)
                    definition.AddItemType(type);
                else if (asset is SortingScenarioDefinition scenario)
                    definition.AddScenario(scenario);

                EditorUtility.SetDirty(definition);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            return asset;
        }

        [MenuItem(MenuRoot + "Open Game Definition", priority = 40)]
        public static void SelectGameDefinition()
        {
            GameDefinition definition = FindGameDefinition();
            if (definition == null)
            {
                Debug.LogError("No GameDefinition asset found. Create one with Create > Onde Vai > Game Definition.");
                return;
            }

            Selection.activeObject = definition;
            EditorGUIUtility.PingObject(definition);
        }

        [MenuItem(MenuRoot + "New Item Type", priority = 41)]
        public static void NewItemType()
        {
            CreateContentAsset<ItemTypeDefinition>(ItemTypesFolder, "NewItemType", FindGameDefinition());
        }

        [MenuItem(MenuRoot + "New Scenario", priority = 42)]
        public static void NewScenario()
        {
            CreateContentAsset<SortingScenarioDefinition>(ScenariosFolder, "NewScenario", FindGameDefinition());
        }

        [MenuItem(MenuRoot + "Validate Content", priority = 60)]
        public static void ValidateContentMenu()
        {
            GameDefinition definition = FindGameDefinition();
            List<string> problems = Validate(definition);
            if (problems.Count == 0)
            {
                Debug.Log("Onde Vai content is valid.", definition);
                return;
            }

            foreach (string problem in problems)
                Debug.LogWarning(problem, definition);
        }

        /// <summary>Returns a human readable list of content problems. Empty means everything is valid.</summary>
        public static List<string> Validate(GameDefinition definition)
        {
            List<string> problems = new();
            if (definition == null)
            {
                problems.Add("No GameDefinition asset found.");
                return problems;
            }

            if (definition.ItemPrefab == null)
                problems.Add("GameDefinition has no Item prefab.");

            HashSet<ItemGroup> groups = new();
            foreach (ItemTypeDefinition type in definition.ItemTypes)
            {
                if (type == null)
                {
                    problems.Add("GameDefinition has an empty item type slot.");
                    continue;
                }

                if (!groups.Add(type.Group))
                    problems.Add($"Group {type.Group} is used by more than one item type ({type.name}).");

                for (int i = 0; i < type.Variants.Count; i++)
                {
                    ItemVariant variant = type.Variants[i];
                    if (variant != null && variant.Enabled && variant.Sprite == null)
                        problems.Add($"{type.name}: variant {i} ('{variant.Id}') has no sprite.");
                }
            }

            HashSet<string> ids = new();
            int playable = 0;
            foreach (SortingScenarioDefinition scenario in definition.Scenarios)
            {
                if (scenario == null)
                {
                    problems.Add("GameDefinition has an empty scenario slot.");
                    continue;
                }

                if (!ids.Add(scenario.Id))
                    problems.Add($"Scenario id '{scenario.Id}' is used more than once.");

                if (scenario.IsPlayable)
                    playable++;
                else
                    problems.Add($"Scenario '{scenario.DisplayName}' is not playable yet ({DescribeScenarioProblem(scenario)}).");

                if (scenario.TypeA != null && !Contains(definition.ItemTypes, scenario.TypeA))
                    problems.Add($"Scenario '{scenario.DisplayName}' uses {scenario.TypeA.name}, which is not registered in the GameDefinition.");

                if (scenario.TypeB != null && !Contains(definition.ItemTypes, scenario.TypeB))
                    problems.Add($"Scenario '{scenario.DisplayName}' uses {scenario.TypeB.name}, which is not registered in the GameDefinition.");

                if (scenario.TypeC != null && !Contains(definition.ItemTypes, scenario.TypeC))
                    problems.Add($"Scenario '{scenario.DisplayName}' uses {scenario.TypeC.name}, which is not registered in the GameDefinition.");

                if (scenario.TypeD != null && !Contains(definition.ItemTypes, scenario.TypeD))
                    problems.Add($"Scenario '{scenario.DisplayName}' uses {scenario.TypeD.name}, which is not registered in the GameDefinition.");

                if ((scenario.TypeC != null || scenario.TypeD != null) && !scenario.IsFourWayPlayable)
                    problems.Add($"Scenario '{scenario.DisplayName}' is not playable in four-way mode ({DescribeFourWayScenarioProblem(scenario)}).");
            }

            if (playable == 0)
                problems.Add("No playable scenario. The game falls back to Toys vs Tools, which also needs content.");

            return problems;
        }

        /// <summary>Short reason why a scenario cannot be played.</summary>
        public static string DescribeScenarioProblem(SortingScenarioDefinition scenario)
        {
            if (scenario.TypeA == null || scenario.TypeB == null)
                return "both item types must be assigned";

            if (scenario.TypeA.Group == scenario.TypeB.Group)
                return "the two item types use the same group";

            if (!scenario.TypeA.HasPlayableVariants)
                return $"{scenario.TypeA.name} has no enabled variant with a sprite";

            return scenario.TypeB.HasPlayableVariants ? "unknown" : $"{scenario.TypeB.name} has no enabled variant with a sprite";
        }

        /// <summary>Short reason why a scenario cannot be played with four categories.</summary>
        public static string DescribeFourWayScenarioProblem(SortingScenarioDefinition scenario)
        {
            if (!scenario.IsPlayable)
                return DescribeScenarioProblem(scenario);

            if (scenario.TypeC == null || scenario.TypeD == null)
                return "both optional item types must be assigned";

            ItemTypeDefinition[] types = { scenario.TypeA, scenario.TypeB, scenario.TypeC, scenario.TypeD };
            for (int i = 0; i < types.Length; i++)
            {
                if (!types[i].HasPlayableVariants)
                    return $"{types[i].name} has no enabled variant with a sprite";

                for (int j = 0; j < i; j++)
                {
                    if (types[j].Group == types[i].Group)
                        return $"{types[i].name} uses the same group as {types[j].name}";
                }
            }

            return "unknown";
        }

        private static bool Contains(IReadOnlyList<ItemTypeDefinition> list, ItemTypeDefinition type)
        {
            foreach (ItemTypeDefinition entry in list)
            {
                if (entry == type)
                    return true;
            }

            return false;
        }
    }
}
