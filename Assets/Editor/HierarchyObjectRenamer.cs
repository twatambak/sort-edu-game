using UnityEditor;
using UnityEngine;

public class HierarchyObjectRenamer : EditorWindow
{
    private string baseName = "GameObject";
    private int startIndex = 1;
    private int increment = 1;
    private bool useIncrement = true;

    [MenuItem("Tools/Hierarchy Renamer")]
    public static void ShowWindow()
    {
        GetWindow<HierarchyObjectRenamer>("Hierarchy Renamer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Settings", EditorStyles.boldLabel);

        baseName = EditorGUILayout.TextField("Base Name", baseName);
        useIncrement = EditorGUILayout.Toggle("Add Index Suffix", useIncrement);

        if (useIncrement)
        {
            startIndex = EditorGUILayout.IntField("Start Index", startIndex);
            increment = EditorGUILayout.IntField("Increment", increment);
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Rename Selected"))
        {
            RenameSelectedObjects();
        }
    }

    private void RenameSelectedObjects()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select one or more GameObjects in the Hierarchy.", "OK");
            return;
        }

        // Sort by hierarchy order (not name)
        System.Array.Sort(selectedObjects, (a, b) =>
            a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

        Undo.RecordObjects(selectedObjects, "Batch Rename");

        for (int i = 0; i < selectedObjects.Length; i++)
        {
            if (useIncrement)
            {
                int currentIndex = startIndex + (i * increment);
                selectedObjects[i].name = $"{baseName}_{currentIndex}";
            }
            else
            {
                selectedObjects[i].name = baseName;
            }
        }
    }

    [MenuItem("GameObject/Hierarchy Renamer", false, 49)]
    private static void OpenRenamerWindow()
    {
        HierarchyObjectRenamer.ShowWindow();
    }

    [MenuItem("GameObject/Hierarchy Renamer", true)]
    private static bool ValidateOpenRenamerWindow()
    {
        return Selection.gameObjects.Length > 0;
    }
}
