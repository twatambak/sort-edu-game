using UnityEngine;

namespace OndeVai.Gameplay
{
    /// <summary>
    /// A sorting scenario: two item types for classic play and optional additional types for four-way play.
    /// </summary>
    [CreateAssetMenu(fileName = "Scenario", menuName = "Onde Vai/Sorting Scenario", order = 1)]
    public sealed class SortingScenarioDefinition : ScriptableObject
    {
        private const string DefaultId = "toys-tools";

        [Tooltip("Stable id stored in settings and in the history database. Do not change after release.")]
        [SerializeField] private string _id = DefaultId;

        [Tooltip("Name shown to parents and teachers (Portuguese).")]
        [SerializeField] private string _displayName = "Brinquedos e Ferramentas";

        [Tooltip("First category. Right swipe in card modes.")]
        [SerializeField] private ItemTypeDefinition _typeA;

        [Tooltip("Second category. Left swipe in card modes.")]
        [SerializeField] private ItemTypeDefinition _typeB;

        [Tooltip("Optional. Assigned to the Up swipe in four-way mode.")]
        [SerializeField] private ItemTypeDefinition _typeC;

        [Tooltip("Optional. Assigned to the Down swipe in four-way mode.")]
        [SerializeField] private ItemTypeDefinition _typeD;

        private static SortingScenarioDefinition _default;

        public string Id => string.IsNullOrEmpty(_id) ? name : _id;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public ItemTypeDefinition TypeA => _typeA;
        public ItemTypeDefinition TypeB => _typeB;
        public ItemTypeDefinition TypeC => _typeC;
        public ItemTypeDefinition TypeD => _typeD;

        public ItemGroup GroupA => _typeA != null ? _typeA.Group : ItemGroup.Toy;
        public ItemGroup GroupB => _typeB != null ? _typeB.Group : ItemGroup.Tool;
        public ItemGroup GroupC => _typeC != null ? _typeC.Group : ItemGroup.Toy;
        public ItemGroup GroupD => _typeD != null ? _typeD.Group : ItemGroup.Tool;
        public Sprite IconA => _typeA != null ? _typeA.Icon : null;
        public Sprite IconB => _typeB != null ? _typeB.Icon : null;
        public Sprite IconC => _typeC != null ? _typeC.Icon : null;
        public Sprite IconD => _typeD != null ? _typeD.Icon : null;

        /// <summary>True when both types are assigned, different and have spawnable variants.</summary>
        public bool IsPlayable =>
            _typeA != null && _typeB != null && _typeA.Group != _typeB.Group &&
            _typeA.HasPlayableVariants && _typeB.HasPlayableVariants;

        /// <summary>True when all four assigned types are distinct and have spawnable variants.</summary>
        public bool IsFourWayPlayable =>
            IsPlayable && _typeC != null && _typeD != null &&
            _typeC.Group != _typeA.Group && _typeC.Group != _typeB.Group &&
            _typeD.Group != _typeA.Group && _typeD.Group != _typeB.Group && _typeD.Group != _typeC.Group &&
            _typeC.HasPlayableVariants && _typeD.HasPlayableVariants;

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
