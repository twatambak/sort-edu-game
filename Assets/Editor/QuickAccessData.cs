using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Slots of the Quick Access window (<see cref="QuickAccessTool"/>). Personal to each user: stored in
/// UserSettings/QuickAccess.asset, which is outside Assets/ and not committed to version control.
/// </summary>
[FilePath("UserSettings/QuickAccess.asset", FilePathAttribute.Location.ProjectFolder)]
public class QuickAccessData : ScriptableSingleton<QuickAccessData>
{
    [Tooltip("Asset shortcuts. Empty entries are free slots. Scene objects are not supported.")]
    public List<Object> slots = new List<Object>();

    /// <summary>Writes the slots to the user settings file.</summary>
    public void Persist()
    {
        Save(true);
    }
}
