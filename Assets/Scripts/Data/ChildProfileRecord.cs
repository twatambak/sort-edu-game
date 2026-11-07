using SQLite;

namespace OndeVai.Data
{
    /// <summary>
    /// A child registered on this device. Stored only in the local database.
    /// </summary>
    public sealed class ChildProfileRecord
    {
        public const int MinAge = 1;
        public const int MaxAge = 12;
        public const int MaxNameLength = 24;

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(MaxNameLength), NotNull]
        public string Name { get; set; }

        public int Age { get; set; }

        /// <summary>Index into the avatar palette used by the UI.</summary>
        public int AvatarIndex { get; set; }

        public long CreatedAtUtcTicks { get; set; }
    }
}
