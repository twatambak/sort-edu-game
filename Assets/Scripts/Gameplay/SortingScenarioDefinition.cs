using UnityEngine;

namespace OndeVai.Gameplay
{
    /// <summary>
    /// A sorting scenario: the two item types the child separates during a match.
    /// </summary>
    [CreateAssetMenu(fileName = "Scenario", menuName = "Onde Vai/Sorting Scenario", order = 1)]
    public sealed class SortingScenarioDefinition : ScriptableObject
    {
        private const string DefaultId = "toys-tools";

        [Tooltip("Stable id stored in settings and in the history database. Do not change after release.")]
        [SerializeField] private string _id = DefaultId;

        [Tooltip("Name shown to parents and teachers (Portuguese).")]
        [SerializeField] private string _displayName = "Brinquedos e Ferramentas";

        [Tooltip("Goes to the first storage.")]
        [SerializeField] private ItemTypeDefinition _typeA;

        [Tooltip("Goes to the second storage.")]
        [SerializeField] private ItemTypeDefinition _typeB;

        private static SortingScenarioDefinition _default;

        public string Id => string.IsNullOrEmpty(_id) ? name : _id;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public ItemTypeDefinition TypeA => _typeA;
        public ItemTypeDefinition TypeB => _typeB;

        public ItemGroup GroupA => _typeA != null ? _typeA.Group : ItemGroup.Toy;
        public ItemGroup GroupB => _typeB != null ? _typeB.Group : ItemGroup.Tool;
        public Sprite IconA => _typeA != null ? _typeA.Icon : null;
        public Sprite IconB => _typeB != null ? _typeB.Icon : null;

        /// <summary>True when both types are assigned, different and have spawnable variants.</summary>
        public bool IsPlayable =>
            _typeA != null && _typeB != null && _typeA.Group != _typeB.Group &&
            _typeA.HasPlayableVariants && _typeB.HasPlayableVariants;

        /// <summary>Toys vs Tools fallback used when no scenario is configured.</summary>
        public static SortingScenarioDefinition Default
        {
            get
            {
                if (_default == null)
                {
                    _default = CreateInstance<SortingScenarioDefinition>();
                    _default.name = "Default Scenario";
                    _default.hideFlags = HideFlags.DontSave;
                }

                return _default;
            }
        }

        /// <summary>Id used when nothing has been chosen yet.</summary>
        public static string DefaultScenarioId => DefaultId;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id))
                _id = name.ToLowerInvariant().Replace(' ', '-');
        }
#endif
    }
}
