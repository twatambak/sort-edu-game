using SQLite;

namespace OndeVai.Data
{
    /// <summary>
    /// One attempt to sort one item. Gives parents and teachers per-category insight.
    /// </summary>
    public sealed class ItemAttemptRecord
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int SessionId { get; set; }

        [Indexed]
        public int ChildProfileId { get; set; }

        /// <summary>Correct <see cref="ItemGroup"/> of the item, as an integer.</summary>
        public int ItemGroup { get; set; }

        /// <summary><see cref="ItemGroup"/> of the storage the child chose, as an integer.</summary>
        public int ChosenGroup { get; set; }

        public string ItemName { get; set; }

        public bool Correct { get; set; }

        /// <summary>Seconds between the item appearing and this attempt.</summary>
        public float ResponseSeconds { get; set; }

        /// <summary>True when the tutorial pointer was guiding the child.</summary>
        public bool DuringTutorial { get; set; }
    }
}
