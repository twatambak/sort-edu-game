using OndeVai.Gameplay;
using UnityEngine;

/// <summary>Outcome of one sorting attempt, broadcast by <see cref="GameController.OnItemSorted"/>.</summary>
public readonly struct ItemSortResult
{
    public readonly Item Item;

    /// <summary>Storage that received the drop, or null when the answer was a swipe.</summary>
    public readonly SortingSource Storage;
    public readonly bool Correct;
    public readonly bool DuringTutorial;

    /// <summary>Where the item was when the answer was given.</summary>
    public readonly Vector3 Position;

    /// <summary>Consecutive correct answers including this one. 0 after a mistake.</summary>
    public readonly int Streak;

    public ItemSortResult(Item item, SortingSource storage, bool correct, bool duringTutorial, Vector3 position, int streak)
    {
        Item = item;
        Storage = storage;
        Correct = correct;
        DuringTutorial = duringTutorial;
        Position = position;
        Streak = streak;
    }
}
