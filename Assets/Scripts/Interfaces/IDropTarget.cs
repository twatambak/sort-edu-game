using UnityEngine;

/// <summary>Receives objects released on top of it.</summary>
public interface IDropTarget
{
    public void OnDrop(GameObject droppedObject);
}

/// <summary>
/// Optional companion of <see cref="IDropTarget"/> for targets that react while a dragged object
/// is held over them.
/// </summary>
public interface IDropHoverTarget
{
    /// <summary>Called when a dragged object enters (true) or leaves (false) the target.</summary>
    public void SetHovered(bool hovered);
}
