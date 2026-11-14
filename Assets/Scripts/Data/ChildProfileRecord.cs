using System.Text;
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

        /// <summary>Stable short identifier generated from the first initial and three digits.</summary>
        [MaxLength(4)]
        public string UserCode { get; set; }

        /// <summary>When true, UI surfaces show a masked version of the child's name.</summary>
        public bool HideName { get; set; }

        public string DisplayName => HideName ? MaskName(Name) : Name;

        public int Age { get; set; }

        /// <summary>Index into the avatar palette used by the UI.</summary>
        public int AvatarIndex { get; set; }

        public long CreatedAtUtcTicks { get; set; }

        /// <summary>Shows each word's first character and masks the remaining characters.</summary>
        public static string MaskName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            StringBuilder masked = new(name.Length);
            bool atWordStart = true;
            foreach (char character in name.Trim())
            {
                if (char.IsWhiteSpace(character))
                {
                    masked.Append(character);
                    atWordStart = true;
                }
                else if (atWordStart)
                {
                    masked.Append(char.ToUpperInvariant(character));
                    atWordStart = false;
                }
                else
                {
                    masked.Append('*');
                }
            }

            return masked.ToString();
        }
    }
}
