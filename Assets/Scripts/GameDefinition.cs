using System.Collections.Generic;
using UnityEngine;

namespace OndeVai.Gameplay
{
    /// <summary>
/// Content catalog for the game: the single item prefab, the item types (each with its variants)
/// and the sorting scenarios that pair two types.
/// </summary>
[CreateAssetMenu(fileName = "GameDefinition", menuName = "Onde Vai/Game Definition", order = -1)]
public class GameDefinition : ScriptableObject
{
    [Tooltip("The only item prefab. Its sprite, collider and physics are set from the item type data when spawned.")]
    [SerializeField] private Item _itemPrefab;

    [Tooltip("Every category of item. Each group should appear only once.")]
    [SerializeField] private List<ItemTypeDefinition> _itemTypes = new();

    [Tooltip("Scenarios offered in the options screen. Only scenarios whose two types have variants are playable.")]
    [SerializeField] private List<SortingScenarioDefinition> _scenarios = new();

    public Item ItemPrefab => _itemPrefab;
    public IReadOnlyList<ItemTypeDefinition> ItemTypes => _itemTypes;
    public IReadOnlyList<SortingScenarioDefinition> Scenarios => _scenarios;

    /// <summary>Returns the type registered for a group, or null.</summary>
    public ItemTypeDefinition GetItemType(ItemGroup group)
    {
        foreach (ItemTypeDefinition type in _itemTypes)
        {
            if (type != null && type.Group == group)
                return type;
        }

        return null;
    }

    /// <summary>True when the group has a type with at least one spawnable variant.</summary>
    public bool HasContent(ItemGroup group)
    {
        ItemTypeDefinition type = GetItemType(group);
        return type != null && type.HasPlayableVariants;
    }

    /// <summary>Picks a variant of the group by weight, avoiding <paramref name="avoid"/> when possible.</summary>
    public bool TryPickVariant(ItemGroup group, ItemVariant avoid, out ItemTypeDefinition type, out ItemVariant variant)
    {
        type = GetItemType(group);
        variant = null;
        return type != null && type.TryPickVariant(avoid, out variant);
    }

    /// <summary>
    /// Finds a playable scenario by id. Falls back to the first playable scenario, then to the built-in default.
    /// </summary>
    public SortingScenarioDefinition GetScenarioOrDefault(string scenarioId)
    {
        SortingScenarioDefinition firstPlayable = null;
        foreach (SortingScenarioDefinition scenario in _scenarios)
        {
            if (scenario == null || !scenario.IsPlayable)
                continue;

            if (scenario.Id == scenarioId)
                return scenario;

            if (firstPlayable == null)
                firstPlayable = scenario;
        }

        return firstPlayable != null ? firstPlayable : SortingScenarioDefinition.Default;
    }

    /// <summary>Fills <paramref name="results"/> with every playable scenario, or the default one when none is set up.</summary>
    public void GetPlayableScenarios(List<SortingScenarioDefinition> results)
    {
        results.Clear();
        foreach (SortingScenarioDefinition scenario in _scenarios)
        {
            if (scenario != null && scenario.IsPlayable)
                results.Add(scenario);
        }

        if (results.Count == 0)
            results.Add(SortingScenarioDefinition.Default);
    }

#if UNITY_EDITOR
    /// <summary>Editor only: registers a type if it is not in the list yet.</summary>
    public void AddItemType(ItemTypeDefinition type)
    {
        if (type != null && !_itemTypes.Contains(type))
            _itemTypes.Add(type);
    }

    /// <summary>Editor only: registers a scenario if it is not in the list yet.</summary>
    public void AddScenario(SortingScenarioDefinition scenario)
    {
        if (scenario != null && !_scenarios.Contains(scenario))
            _scenarios.Add(scenario);
    }
#endif
}

/// <summary>
/// Category of a sortable item. Values are serialized as integers in assets and in the
/// history database, so new values must only be appended.
/// </summary>
public enum ItemGroup
{
    Toy,
    Tool,
    Letter,
    Number,
    HealthyFood,
    UnhealthyFood,
    Vowel,
    Consonant,
    FarmAnimal,
    JungleAnimal
}

}

