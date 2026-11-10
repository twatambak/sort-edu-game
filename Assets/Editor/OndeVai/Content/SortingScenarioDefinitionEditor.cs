using OndeVai.Gameplay;
using UnityEditor;
using UnityEngine;

namespace OndeVai.EditorTools
{
    /// <summary>Scenario inspector with previews of its categories and playability checks.</summary>
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
            DrawType(scenario.TypeA, "Right");
            DrawType(scenario.TypeB, "Left");
            EditorGUILayout.EndHorizontal();

            if (!scenario.IsPlayable)
                EditorGUILayout.HelpBox($"Not playable: {ContentEditorUtility.DescribeScenarioProblem(scenario)}.", MessageType.Warning);
            else
                EditorGUILayout.HelpBox("Playable in drag and two-direction swipe modes.", MessageType.Info);
        }

        private static void DrawType(ItemTypeDefinition type, string direction)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(Preview + 60f));
            Rect rect = GUILayoutUtility.GetRect(Preview, Preview, GUILayout.Width(Preview), GUILayout.Height(Preview));
            ContentEditorUtility.DrawSprite(rect, type != null ? type.Icon : null);
            string label = type != null ? $"{type.DisplayName}\n{type.Variants.Count} variant(s)" : "(none)";
            GUILayout.Label(direction, EditorStyles.boldLabel);
            GUILayout.Label(label, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }
    }
}
