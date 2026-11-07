using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace OndeVai.Gameplay
{
    /// <summary>
    /// Creates, tracks and recycles the items of a match. Every item comes from the single prefab in
    /// <see cref="GameDefinition.ItemPrefab"/> and gets its look from an <see cref="ItemTypeDefinition"/> variant.
    /// Plain C# so it can be shared by every game mode.
    /// </summary>
    public sealed class ItemSpawner
    {
        private const float SpawnPopDuration = 0.35f;

        private readonly GameDefinition _definition;
        private readonly Transform[] _spawnPoints;
        private readonly Transform _poolRoot;
        private readonly List<Item> _activeItems = new();
        private readonly Stack<Item> _pool = new();
        private readonly List<Transform> _availableSpawnPoints = new();
        private readonly List<Collider2D> _activeColliders = new();
        private readonly List<ItemGroup> _waveGroups = new();
        private readonly Dictionary<ItemGroup, ItemVariant> _lastVariantByGroup = new();
        private readonly System.Action<Item> _releaseHandler;

        public ItemSpawner(GameDefinition definition, Transform[] spawnPoints, Transform poolRoot)
        {
            _definition = definition;
            _spawnPoints = spawnPoints ?? System.Array.Empty<Transform>();
            _poolRoot = poolRoot;
            _releaseHandler = Release;

            if (_definition == null)
                Debug.LogError("ItemSpawner has no GameDefinition assigned. No items will be spawned.");
            else if (_definition.ItemPrefab == null)
                Debug.LogError("GameDefinition has no Item prefab assigned. No items will be spawned.", _definition);
        }

        public IReadOnlyList<Item> ActiveItems => _activeItems;

        public int ActiveCount => _activeItems.Count;

        /// <summary>
        /// Spawns <paramref name="count"/> items at distinct spawn points. When the wave has two or more
        /// items it always contains both groups, so every wave asks the child to really sort.
        /// </summary>
        public void SpawnWave(int count, ItemGroup groupA, ItemGroup groupB, System.Action<Item> onSpawned)
        {
            RefillSpawnPoints();
            BuildWaveGroups(count, groupA, groupB);

            foreach (ItemGroup group in _waveGroups)
            {
                Item item = Spawn(group, TakeSpawnPoint());
                if (item != null)
                    onSpawned?.Invoke(item);
            }
        }

        /// <summary>Spawns one item of a random group of the pair at <paramref name="position"/>.</summary>
        public Item SpawnSingle(Vector3 position, ItemGroup groupA, ItemGroup groupB)
        {
            ItemGroup group = Random.value < 0.5f ? groupA : groupB;
            return Spawn(group, position);
        }

        /// <summary>Stops tracking an item. Does not despawn it.</summary>
        public void Remove(Item item)
        {
            if (item == null || !_activeItems.Remove(item))
                return;

            foreach (Collider2D itemCollider in item.GetComponentsInChildren<Collider2D>())
                _activeColliders.Remove(itemCollider);
        }

        /// <summary>Returns every tracked item to the pool.</summary>
        public void Clear()
        {
            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                Item item = _activeItems[i];
                if (item != null)
                    item.Despawn();
            }

            _activeItems.Clear();
            _activeColliders.Clear();
        }

        private Item Spawn(ItemGroup group, Vector3 position)
        {
            if (_definition == null || _definition.ItemPrefab == null)
                return null;

            _lastVariantByGroup.TryGetValue(group, out ItemVariant lastVariant);
            if (!_definition.TryPickVariant(group, lastVariant, out ItemTypeDefinition type, out ItemVariant variant))
            {
                Debug.LogError($"GameDefinition has no playable item type or variant for group {group}.", _definition);
                return null;
            }

            _lastVariantByGroup[group] = variant;
            Item item = Rent();
            Transform itemTransform = item.transform;
            itemTransform.position = position;
            item.Initialize(type, variant);
            item.gameObject.SetActive(true);

            IgnoreCollisionsWithActiveItems(item);
            _activeItems.Add(item);
            PlaySpawnPop(item);
            return item;
        }

        private Item Rent()
        {
            while (_pool.Count > 0)
            {
                Item pooled = _pool.Pop();
                if (pooled != null)
                    return pooled;
            }

            Item created = Object.Instantiate(_definition.ItemPrefab, _poolRoot);
            created.gameObject.SetActive(false);
            created.SetReleaseHandler(_releaseHandler);
            return created;
        }

        private void Release(Item item)
        {
            if (item == null)
                return;

            Remove(item);
            item.gameObject.SetActive(false);
            _pool.Push(item);
        }

        private void IgnoreCollisionsWithActiveItems(Item item)
        {
            Collider2D[] newColliders = item.GetComponentsInChildren<Collider2D>();
            for (int i = _activeColliders.Count - 1; i >= 0; i--)
            {
                Collider2D activeCollider = _activeColliders[i];
                if (activeCollider == null)
                {
                    _activeColliders.RemoveAt(i);
                    continue;
                }

                foreach (Collider2D newCollider in newColliders)
                    Physics2D.IgnoreCollision(activeCollider, newCollider);
            }

            _activeColliders.AddRange(newColliders);
        }

        private static void PlaySpawnPop(Item item)
        {
            item.PresentationScale = 0.2f;
            DOTween.To(() => item.PresentationScale, value => item.PresentationScale = value, 1f, SpawnPopDuration)
                .SetEase(Ease.OutBack)
                .SetTarget(item)
                .SetLink(item.gameObject);
        }

        private void BuildWaveGroups(int count, ItemGroup groupA, ItemGroup groupB)
        {
            _waveGroups.Clear();
            for (int i = 0; i < count; i++)
            {
                if (i < 2)
                    _waveGroups.Add(i == 0 ? groupA : groupB);
                else
                    _waveGroups.Add(Random.value < 0.5f ? groupA : groupB);
            }

            for (int i = _waveGroups.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (_waveGroups[i], _waveGroups[swapIndex]) = (_waveGroups[swapIndex], _waveGroups[i]);
            }
        }

        private void RefillSpawnPoints()
        {
            _availableSpawnPoints.Clear();
            foreach (Transform spawnPoint in _spawnPoints)
            {
                if (spawnPoint != null)
                    _availableSpawnPoints.Add(spawnPoint);
            }
        }

        private Vector3 TakeSpawnPoint()
        {
            if (_availableSpawnPoints.Count == 0)
                RefillSpawnPoints();

            if (_availableSpawnPoints.Count == 0)
                return Vector3.zero;

            int randomIndex = Random.Range(0, _availableSpawnPoints.Count);
            Transform selected = _availableSpawnPoints[randomIndex];
            _availableSpawnPoints.RemoveAt(randomIndex);
            return selected.position;
        }
    }
}
