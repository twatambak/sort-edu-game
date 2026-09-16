using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewQuickAccessData", menuName = "Tools/Quick Access Data")]
public class QuickAccessData : ScriptableObject
{
    public List<Object> slots = new List<Object>();
}