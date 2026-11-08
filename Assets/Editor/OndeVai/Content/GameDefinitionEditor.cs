using System.Collections.Generic;
using OndeVai.Gameplay;
using UnityEditor;
using UnityEngine;

namespace OndeVai.EditorTools
{
    /// <summary>Game definition inspector: content overview, validation and shortcuts to create new content.</summary>
    [CustomEditor(typeof(GameDefinition))]
    public sealed class GameDefinitionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();

            GameDefinition definition = (GameDefinition)target;
            EditorGUILayout.Space();
            DrawOverview(definition);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("New Item Type"))
                ContentEditorUtility.CreateContentAsset<ItemTypeDefinition>(ContentEditorUtility.ItemTypesFolder, "NewItemType", definition);

            if (GUILayout.Button("New Scenario"))
                ContentEditorUtility.CreateContentAsset<SortingScenarioDefinition>(ContentEditorUtility.ScenariosFolder, "NewScenario", definition);

            EditorGUILayout.EndHorizontal();

            List<string> problems = ContentEditorUtility.Validate(definition);
            foreach (string problem in problems)
                EditorGUILayout.HelpBox(problem, MessageType.Warning);
        }

        private static void DrawOverview(GameDefinition definition)
        {
            EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
            foreach (SortingScenarioDefinition scenario in definition.Scenarios)
            {
                if (scenario == null)
                    continue;

                string status = scenario.IsPlayable ? "Playable" : "Needs content";
                string details = $"{Count(scenario.TypeA)} x {Count(scenario.TypeB)} variants";
                if (scenario.TypeC != null || scenario.TypeD != null)
                    details += $" / 4-way: {(scenario.IsFourWayPlayable ? "Playable" : "Needs content")}";

                EditorGUILayout.LabelField(scenario.DisplayName, $"{details}   {status}");
            }
        }

        private static int Count(ItemTypeDefinition type)
        {
            if (type == null)
                return 0;

            int count = 0;
            foreach (ItemVariant variant in type.Variants)
            {
                if (variant != null && variant.IsPlayable)
                    count++;
            }

            return count;
        }
    }
}
