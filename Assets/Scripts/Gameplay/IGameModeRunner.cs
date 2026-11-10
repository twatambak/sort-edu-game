using System;
using UnityEngine;

namespace OndeVai.Gameplay
{
    /// <summary>
    /// Mode-specific rules of a match (how items appear and how the child answers).
    /// Scoring, errors, tutorial and persistence stay in <see cref="GameController"/>.
    /// </summary>
    public interface IGameModeRunner
    {
        /// <summary>True when wrong drops should push the item away physically.</summary>
        bool EjectOnError { get; }

        /// <summary>The item the tutorial should point at, or null.</summary>
        Item FocusItem { get; }

        void Begin(GameConfiguration config);

        /// <summary>Cancels pending work. Called when the match ends or is aborted.</summary>
        void End();

        /// <summary>Called when the child taps a storage.</summary>
        void HandleStorageSelected(SortingSource storage);

        /// <summary>Called after the controller evaluated an answer and played the storage feedback.</summary>
        void HandleItemResolved(Item item, bool correct);
    }

    /// <summary>Services a mode runner needs from the match.</summary>
    public sealed class GameModeContext
    {
        public ItemSpawner Spawner;
        public Transform QuestionPoint;

        /// <summary>Notifies listeners that an item appeared.</summary>
        public Action<Item> NotifySpawned;

        /// <summary>Evaluates an answer. Returns true when correct.</summary>
        public Func<Item, SortingSource, bool> Submit;

        /// <summary>Evaluates a selected category without requiring a physical storage.</summary>
        public Func<Item, ItemGroup, bool> SubmitGroup;

        /// <summary>Notifies listeners that an item was picked up.</summary>
        public Action<Item> NotifyPicked;

        /// <summary>Notifies listeners that an item was released.</summary>
        public Action<Item> NotifyReleased;

        /// <summary>True while input is allowed (not paused, not game over).</summary>
        public Func<bool> CanAcceptInput;
    }
}
