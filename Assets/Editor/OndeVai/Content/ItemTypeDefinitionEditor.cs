using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using OndeVai.Gameplay;

namespace OndeVai.EditorTools
{
    /// <summary>
    /// Inspector for item types: variant list with sprite thumbnails, drag and drop of sprites or
    /// sprite sheets to create variants, and inline validation.
    /// </summary>
    [CustomEditor(typeof(ItemTypeDefinition))]
    public sealed class ItemTypeDefinitionEditor : Editor
    {
        private const float Thumbnail = 68f;
        private const float Spacing = 2f;
        private const int FieldRows = 4;

        private SerializedProperty _variants;
        private ReorderableList _list;

        private void OnEnable()
        {
            _variants = serializedObject.FindProperty("_variants");
            _list = new ReorderableList(serializedObject, _variants, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, $"Variants ({_variants.arraySize})"),
                elementHeightCallback = _ => FieldRows * (EditorGUIUtility.singleLineHeight + Spacing) + 8f,
                drawElementCallback = DrawVariant,
                onAddCallback = AddEmptyVariant
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script", "_variants");

            EditorGUILayout.Space();
            DrawDropArea();
            _list.DoLayoutList();
            DrawTools();
            DrawWarnings();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawVariant(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty element = _variants.GetArrayElementAtIndex(index);
            SerializedProperty sprite = element.FindPropertyRelative("Sprite");
            float line = EditorGUIUtility.singleLineHeight;
            rect.y += 4f;

            Rect thumbRect = new(rect.x, rect.y, Thumbnail, Thumbnail);
            ContentEditorUtility.DrawSprite(thumbRect, sprite.objectReferenceValue as Sprite);

            float x = rect.x + Thumbnail + 8f;
            float width = rect.width - Thumbnail - 8f;
            float half = (width - 6f) * 0.5f;
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 52f;

            Rect row = new(x, rect.y, width, line);
            EditorGUI.PropertyField(new Rect(row.x, row.y, 70f, line), element.FindPropertyRelative("Enabled"), new GUIContent("On"));
            EditorGUI.PropertyField(new Rect(row.x + 76f, row.y, width - 76f, line), element.FindPropertyRelative("Id"));

            row.y += line + Spacing;
            EditorGUI.PropertyField(row, sprite);

            row.y += line + Spacing;
            EditorGUI.PropertyField(new Rect(row.x, row.y, half, line), element.FindPropertyRelative("Weight"));
            EditorGUI.PropertyField(new Rect(row.x + half + 6f, row.y, half, line), element.FindPropertyRelative("Scale"));

            row.y += line + Spacing;
            EditorGUI.PropertyField(new Rect(row.x, row.y, half, line), element.FindPropertyRelative("Tint"));
            EditorGUI.PropertyField(new Rect(row.x + half + 6f, row.y, half, line), element.FindPropertyRelative("Collider"), new GUIContent("Shape"));

            EditorGUIUtility.labelWidth = labelWidth;
        }

        private void AddEmptyVariant(ReorderableList list)
        {
            int index = _variants.arraySize;
            _variants.InsertArrayElementAtIndex(index);
            SerializedProperty element = _variants.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("Id").stringValue = string.Empty;
            element.FindPropertyRelative("Sprite").objectReferenceValue = null;
            element.FindPropertyRelative("Enabled").boolValue = true;
            element.FindPropertyRelative("Weight").floatValue = 1f;
            element.FindPropertyRelative("Scale").floatValue = 1f;
            element.FindPropertyRelative("Tint").colorValue = Color.white;
            element.FindPropertyRelative("Collider").enumValueIndex = (int)ItemColliderOverride.UseTypeDefault;
            list.index = index;
        }

        private void DrawDropArea()
        {
            Rect dropRect = GUILayoutUtility.GetRect(0f, 44f, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "Drop sprites or sprite sheets here to add variants", EditorStyles.helpBox);

            Event current = Event.current;
            if (!dropRect.Contains(current.mousePosition) || (current.type != EventType.DragUpdated && current.type != EventType.DragPerform))
                return;

            UnityEditor.DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (current.type == EventType.DragPerform)
            {
                UnityEditor.DragAndDrop.AcceptDrag();
                AddSprites(ContentEditorUtility.CollectSprites(UnityEditor.DragAndDrop.objectReferences));
            }

            current.Use();
        }

        private void DrawTools()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Selected Sprites"))
                AddSprites(ContentEditorUtility.CollectSprites(Selection.objects));

            if (GUILayout.Button("Sort by Id"))
                SortVariants();

            if (GUILayout.Button("Enable All"))
                SetAllEnabled(true);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawWarnings()
        {
            ItemTypeDefinition type = (ItemTypeDefinition)target;
            if (!type.HasPlayableVariants)
                EditorGUILayout.HelpBox("No enabled variant with a sprite. Scenarios using this type are hidden from the game.", MessageType.Warning);

            int missingSprites = 0;
            int missingShapes = 0;
            foreach (ItemVariant variant in type.Variants)
            {
                if (variant == null || !variant.Enabled)
                    continue;

                if (variant.Sprite == null)
                    missingSprites++;
                else if (type.GetColliderShape(variant) == ItemColliderShape.PhysicsShape && variant.Sprite.GetPhysicsShapeCount() == 0)
                    missingShapes++;
            }

            if (missingSprites > 0)
                EditorGUILayout.HelpBox($"{missingSprites} enabled variant(s) have no sprite and will never spawn.", MessageType.Warning);

            if (missingShapes > 0)
                EditorGUILayout.HelpBox($"{missingShapes} variant(s) use Physics Shape but their sprite has none. A box collider is used instead. Enable 'Generate Physics Shape' in the sprite import settings.", MessageType.Info);
        }

        private void AddSprites(List<Sprite> sprites)
        {
            if (sprites.Count == 0)
                return;

            serializedObject.ApplyModifiedProperties();
            ItemTypeDefinition type = (ItemTypeDefinition)target;
            Undo.RecordObject(type, "Add Item Variants");
            int added = type.AddSprites(sprites);
            EditorUtility.SetDirty(type);
            serializedObject.Update();

            if (added < sprites.Count)
                Debug.Log($"Added {added} variant(s) to {type.name}. {sprites.Count - added} sprite(s) were already in the list.", type);
        }

        private void SortVariants()
        {
            serializedObject.ApplyModifiedProperties();
            ItemTypeDefinition type = (ItemTypeDefinition)target;
            Undo.RecordObject(type, "Sort Item Variants");
            type.SortVariants((a, b) => EditorUtility.NaturalCompare(a?.Id ?? string.Empty, b?.Id ?? string.Empty));
            EditorUtility.SetDirty(type);
            serializedObject.Update();
        }

        private void SetAllEnabled(bool enabled)
        {
            for (int i = 0; i < _variants.arraySize; i++)
                _variants.GetArrayElementAtIndex(i).FindPropertyRelative("Enabled").boolValue = enabled;
        }
    }
}
