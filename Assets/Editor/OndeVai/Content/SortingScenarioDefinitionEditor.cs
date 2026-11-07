using OndeVai.Gameplay;
using UnityEditor;
using UnityEngine;

namespace OndeVai.EditorTools
{
    /// <summary>Scenario inspector with a side by side preview of both item types and a playability check.</summary>
    [CustomEditor(typeof(SortingScenarioDefinition))]
    public sealed class SortingScenarioDefinitionEditor : Editor
    {
        private const float Preview = 72f;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();

            SortingScenarioDefinition scenario = (SortingScenarioDefinition)target;
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            DrawType(scenario.TypeA);
            GUILayout.Label("vs", EditorStyles.boldLabel, GUILayout.Width(24f), GUILayout.Height(Preview));
            DrawType(scenario.TypeB);
            EditorGUILayout.EndHorizontal();

            if (scenario.IsPlayable)
                EditorGUILayout.HelpBox("Playable.", MessageType.Info);
            else
                EditorGUILayout.HelpBox($"Not playable: {ContentEditorUtility.DescribeScenarioProblem(scenario)}.", MessageType.Warning);
        }

        private static void DrawType(ItemTypeDefinition type)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(Preview + 60f));
            Rect rect = GUILayoutUtility.GetRect(Preview, Preview, GUILayout.Width(Preview), GUILayout.Height(Preview));
            ContentEditorUtility.DrawSprite(rect, type != null ? type.Icon : null);
            string label = type != null ? $"{type.DisplayName}\n{type.Variants.Count} variant(s)" : "(none)";
            GUILayout.Label(label, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }
    }
}
