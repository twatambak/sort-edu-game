using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window with a list of asset shortcuts. Slots are personal: stored per user by <see cref="QuickAccessData"/>
/// in UserSettings/, which is not committed. Assets can be assigned per slot or dropped anywhere on the window;
/// each slot can be opened, pinged in the Project window or cleared.
/// Documented in Assets/Documentation/Tooling/EditorTools.md.
/// </summary>
public class QuickAccessTool : EditorWindow
{
    private const int MinSlots = 1;
    private const int MaxSlots = 50;
    private const int DefaultSlots = 10;

    private static readonly GUIContent OpenContent = new GUIContent("Open", "Open the asset (double-clicking it in the Project window does the same).");
    private static readonly GUIContent PingContent = new GUIContent("Ping", "Highlight the asset in the Project window.");
    private static readonly GUIContent ClearContent = new GUIContent("X", "Clear this slot.");

    private readonly List<GUIContent> slotLabels = new List<GUIContent>(MaxSlots);
    private QuickAccessData data;
    private int slotCount = DefaultSlots;
    private Vector2 scrollPos;

    [MenuItem("Tools/Quick Access")]
    public static void ShowWindow()
    {
        GetWindow<QuickAccessTool>("Quick Access");
    }

    private void OnEnable()
    {
        LoadData();
        Undo.undoRedoPerformed += HandleUndoRedo;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= HandleUndoRedo;
    }

    private void OnGUI()
    {
        if (data == null)
        {
            LoadData();
        }

        EditorGUILayout.Space();
        DrawToolbar();
        EditorGUILayout.Space();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        for (int i = 0; i < data.slots.Count; i++)
        {
            DrawSlot(i);
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.EndScrollView();

        HandleDragAndDrop();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal();

        float previousLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 70f;

        EditorGUI.BeginChangeCheck();
        int requested = EditorGUILayout.IntSlider("Slot Count", slotCount, MinSlots, MaxSlots);
        if (EditorGUI.EndChangeCheck())
        {
            SetSlotCount(requested);
        }

        EditorGUIUtility.labelWidth = previousLabelWidth;

        if (GUILayout.Button("Clear All", GUILayout.Width(70f)))
        {
            ClearAllSlots();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("Drop assets anywhere on this window to fill the first empty slot.", EditorStyles.miniLabel);
    }

    private void DrawSlot(int index)
    {
        EditorGUILayout.BeginHorizontal();

        float previousLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 50f;

        EditorGUI.BeginChangeCheck();
        // Scene objects are not allowed: an asset cannot keep references to objects in a scene.
        Object assigned = EditorGUILayout.ObjectField(SlotLabel(index), data.slots[index], typeof(Object), false);
        if (EditorGUI.EndChangeCheck())
        {
            AssignSlot(index, assigned, "Update Quick Access Slot");
        }

        EditorGUIUtility.labelWidth = previousLabelWidth;

        Object current = data.slots[index];
        using (new EditorGUI.DisabledScope(current == null))
        {
            if (GUILayout.Button(OpenContent, GUILayout.Width(48f)))
            {
                AssetDatabase.OpenAsset(current);
            }

            if (GUILayout.Button(PingContent, GUILayout.Width(40f)))
            {
                EditorGUIUtility.PingObject(current);
            }

            if (GUILayout.Button(ClearContent, GUILayout.Width(22f)))
            {
                AssignSlot(index, null, "Clear Quick Access Slot");
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>Cached slot labels, so repaints do not allocate a new string per slot.</summary>
    private GUIContent SlotLabel(int index)
    {
        while (slotLabels.Count <= index)
        {
            slotLabels.Add(new GUIContent($"Slot {slotLabels.Count + 1}"));
        }

        return slotLabels[index];
    }

    private void HandleDragAndDrop()
    {
        Event current = Event.current;
        if (current.type != EventType.DragUpdated && current.type != EventType.DragPerform)
        {
            return;
        }

        bool hasAssets = false;
        foreach (Object dragged in DragAndDrop.objectReferences)
        { 
            if (EditorUtility.IsPersistent(dragged))
            {
                hasAssets = true;
                break;
            }
        }

        if (!hasAssets)
        {
            return;
        }

        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
        if (current.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            AddToEmptySlots(DragAndDrop.objectReferences);
        }

        current.Use();
    }

    private void AddToEmptySlots(Object[] assets)
    {
        Undo.RecordObject(data, "Add To Quick Access");
        int added = 0;
        int skipped = 0;

        foreach (Object asset in assets)
        {
            if (!EditorUtility.IsPersistent(asset) || data.slots.Contains(asset))
            {
                continue;
            }

            int empty = data.slots.IndexOf(null);
            if (empty < 0)
            {
                if (data.slots.Count >= MaxSlots)
                {
                    skipped++;
                    continue;
                }

                data.slots.Add(null);
                empty = data.slots.Count - 1;
            }

            data.slots[empty] = asset;
            added++;
        }

        if (added > 0)
        {
            slotCount = data.slots.Count;
            Save();
        }

        if (skipped > 0)
        {
            Debug.LogWarning($"Quick Access is full ({MaxSlots} slots). {skipped} dropped asset(s) were not added.");
        }
    }

    private void AssignSlot(int index, Object value, string undoName)
    {
        Undo.RecordObject(data, undoName);
        data.slots[index] = value;
        Save();
    }

    private void SetSlotCount(int requested)
    {
        requested = Mathf.Clamp(requested, MinSlots, MaxSlots);
        int assignedLost = 0;
        for (int i = requested; i < data.slots.Count; i++)
        {
            if (data.slots[i] != null)
            {
                assignedLost++;
            }
        }

        if (assignedLost > 0 && !EditorUtility.DisplayDialog("Reduce Quick Access slots",
                $"{assignedLost} assigned slot(s) will be removed. This can be undone.", "Remove", "Cancel"))
        {
            return;
        }

        Undo.RecordObject(data, "Adjust Quick Access Slot Count");
        while (data.slots.Count < requested)
        {
            data.slots.Add(null);
        }

        if (data.slots.Count > requested)
        {
            data.slots.RemoveRange(requested, data.slots.Count - requested);
        }

        slotCount = requested;
        Save();
    }

    private void ClearAllSlots()
    {
        if (!EditorUtility.DisplayDialog("Clear Quick Access", "Are you sure you want to clear all quick access slots?",
                "Clear", "Cancel"))
        {
            return;
        }

        Undo.RecordObject(data, "Clear Quick Access Slots");
        for (int i = 0; i < data.slots.Count; i++)
        {
            data.slots[i] = null;
        }

        Save();
    }

    /// <summary>Writes only the personal settings file; no project assets are saved.</summary>
    private void Save()
    {
        data.Persist();
    }

    private void HandleUndoRedo()
    {
        if (data != null)
        {
            slotCount = Mathf.Max(MinSlots, data.slots.Count);
        }

        Repaint();
    }

    /// <summary>Loads the personal slots, starting with empty slots the first time.</summary>
    private void LoadData()
    {
        data = QuickAccessData.instance;
        if (data.slots == null)
        {
            data.slots = new List<Object>();
        }

        if (data.slots.Count == 0)
        {
            for (int i = 0; i < DefaultSlots; i++)
            {
                data.slots.Add(null);
            }

            Save();
        }

        slotCount = Mathf.Clamp(data.slots.Count, MinSlots, MaxSlots);
    }
}
