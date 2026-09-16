using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class QuickAccessTool : EditorWindow
{
    private const string DataPath = "Assets/Editor/QuickAccessData.asset";

    private QuickAccessData data;
    private int slotCount = 10;
    private Vector2 scrollPos;

    [MenuItem("Tools/Quick Access")]
    public static void ShowWindow()
    {
        GetWindow<QuickAccessTool>("Quick Access");
    }

    private void OnEnable()
    {
        LoadOrCreateData();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();

        if (data == null)
        {
            LoadOrCreateData();

            if (data == null)
            {
                EditorGUILayout.HelpBox(
                    "Could not load or create Quick Access data.",
                    MessageType.Error);

                return;
            }
        }

        EditorGUILayout.BeginHorizontal();

        float previousLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 70f;

        EditorGUI.BeginChangeCheck();

        slotCount = EditorGUILayout.IntSlider(
            "Slot Count",
            slotCount,
            1,
            50);

        if (EditorGUI.EndChangeCheck())
        {
            AdjustSlotCount();
        }

        EditorGUIUtility.labelWidth = previousLabelWidth;

        if (GUILayout.Button("Clear All"))
        {
            ClearAllSlots();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        for (int i = 0; i < data.slots.Count; i++)
        {
            DrawSlot(i);
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawSlot(int index)
    {
        EditorGUILayout.BeginHorizontal();

        float previousLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 45f;

        EditorGUI.BeginChangeCheck();

        Object obj = EditorGUILayout.ObjectField(
            $"Slot {index + 1}",
            data.slots[index],
            typeof(Object),
            false);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(data, "Update Quick Access Slot");

            data.slots[index] = obj;

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }

        EditorGUIUtility.labelWidth = previousLabelWidth;

        GUI.enabled = data.slots[index] != null;

        if (GUILayout.Button("Open", GUILayout.Width(60)))
        {
            AssetDatabase.OpenAsset(data.slots[index]);
        }

        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
    }

    private void AdjustSlotCount()
    {
        Undo.RecordObject(data, "Adjust Quick Access Slot Count");

        while (data.slots.Count < slotCount)
        {
            data.slots.Add(null);
        }

        while (data.slots.Count > slotCount)
        {
            data.slots.RemoveAt(data.slots.Count - 1);
        }

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
    }

    private void ClearAllSlots()
    {
        if (!EditorUtility.DisplayDialog(
                "Clear Quick Access",
                "Are you sure you want to clear all quick access slots?",
                "Clear",
                "Cancel"))
        {
            return;
        }

        Undo.RecordObject(data, "Clear Quick Access Slots");

        for (int i = 0; i < data.slots.Count; i++)
        {
            data.slots[i] = null;
        }

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
    }

    private void LoadOrCreateData()
    {
        data = AssetDatabase.LoadAssetAtPath<QuickAccessData>(DataPath);

        if (data != null)
        {
            slotCount = Mathf.Max(1, data.slots.Count);
            return;
        }

        CreateData();
    }

    private void CreateData()
    {
        string directory = System.IO.Path.GetDirectoryName(DataPath);

        if (!AssetDatabase.IsValidFolder(directory))
        {
            CreateEditorFolder();
        }

        data = ScriptableObject.CreateInstance<QuickAccessData>();

        data.slots = new List<Object>();

        for (int i = 0; i < slotCount; i++)
        {
            data.slots.Add(null);
        }

        AssetDatabase.CreateAsset(data, DataPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void CreateEditorFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Editor"))
        {
            AssetDatabase.CreateFolder("Assets", "Editor");
        }
    }
}