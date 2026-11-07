using UnityEngine;

namespace OndeVai.UI
{
    /// <summary>Colors a child can pick to recognise their profile without reading.</summary>
    public static class AvatarPalette
    {
        private static readonly Color[] Colors =
        {
            new(0.98f, 0.42f, 0.42f, 1f),
            new(1f, 0.66f, 0.25f, 1f),
            new(0.99f, 0.84f, 0.25f, 1f),
            new(0.45f, 0.82f, 0.45f, 1f),
            new(0.35f, 0.7f, 0.98f, 1f),
            new(0.68f, 0.5f, 0.95f, 1f),
            new(0.98f, 0.55f, 0.8f, 1f),
            new(0.55f, 0.42f, 0.32f, 1f)
        };

        public static int Count => Colors.Length;

        public static Color Get(int index)
        {
            return Colors[Mathf.Abs(index) % Colors.Length];
        }

        /// <summary>First letter of the name in upper case, or "?" when empty.</summary>
        public static string Initial(string name)
        {
            return string.IsNullOrWhiteSpace(name) ? "?" : char.ToUpperInvariant(name.Trim()[0]).ToString();
        }
    }
}
