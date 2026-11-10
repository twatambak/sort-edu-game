using SQLite;

namespace OndeVai.Data
{
    /// <summary>
    /// One attempt to sort one item. Gives parents and teachers per-category insight.
    /// Columns added after the first release default to 0 for older rows.
    /// </summary>
    public sealed class ItemAttemptRecord
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int SessionId { get; set; }

        [Indexed]
        public int ChildProfileId { get; set; }

        /// <summary>Correct <see cref="OndeVai.Gameplay.ItemGroup"/> of the item, as an integer.</summary>
        public int ItemGroup { get; set; }

        /// <summary><see cref="OndeVai.Gameplay.ItemGroup"/> of the storage the child chose, as an integer.</summary>
        public int ChosenGroup { get; set; }

        /// <summary>Id of the item variant that was shown.</summary>
        public string ItemName { get; set; }

        public bool Correct { get; set; }

        /// <summary>Seconds between the item becoming available and this attempt.</summary>
        public float ResponseSeconds { get; set; }

        /// <summary>True when the tutorial pointer was guiding the child.</summary>
        public bool DuringTutorial { get; set; }

        /// <summary>Order of the attempt inside its match, starting at 0.</summary>
        public int AttemptIndex { get; set; }

        /// <summary>How many answers the child had already given for this same item. 0 is the first try.</summary>
        public int RetryIndex { get; set; }

        /// <summary>Times the item was picked up before this answer. More than one suggests hesitation.</summary>
        public int PickupCount { get; set; }

        /// <summary>Seconds of play (pauses excluded) between the start of the match and this attempt.</summary>
        public float SecondsIntoMatch { get; set; }

        public long OccurredAtUtcTicks { get; set; }
    }
}
