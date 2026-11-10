using DG.Tweening;

namespace OndeVai.Gameplay
{
    /// <summary>
    /// Game mode 1: waves of items the child drags into the storages. A new wave appears when the
    /// current one is fully sorted.
    /// </summary>
    public sealed class DragAndDropModeRunner : IGameModeRunner
    {
        private readonly GameModeContext _context;
        private GameConfiguration _config;
        private Tween _pendingWave;

        public DragAndDropModeRunner(GameModeContext context)
        {
            _context = context;
        }

        public bool EjectOnError => true;

        public Item FocusItem
        {
            get
            {
                foreach (Item item in _context.Spawner.ActiveItems)
                {
                    if (item != null && !item.IsResolving)
                        return item;
                }

                return null;
            }
        }

        public void Begin(GameConfiguration config)
        {
            _config = config;
            if (_context.QuestionPoint != null)
                _context.QuestionPoint.gameObject.SetActive(false);

            SpawnWave();
        }

        public void End()
        {
            _pendingWave?.Kill();
            _pendingWave = null;
        }

        public void HandleStorageSelected(SortingSource storage)
        {
        }

        public void HandleItemResolved(Item item, bool correct)
        {
            if (!correct || _context.Spawner.ActiveCount > 0 || _pendingWave != null)
                return;

            if (_config.DelayBetweenSpawns <= 0)
            {
                SpawnWave();
                return;
            }

            // Scaled time, so the delay does not run out while the match is paused.
            _pendingWave = DOVirtual.DelayedCall(_config.DelayBetweenSpawns, () =>
            {
                _pendingWave = null;
                SpawnWave();
            }, false);
        }

        private void SpawnWave()
        {
            _context.Spawner.SpawnWave(_config.MaxItemsOnScreen, _config.ItemGroupA, _config.ItemGroupB, item =>
            {
                item.SetDraggable(true);
                _context.NotifySpawned?.Invoke(item);
            });
        }
    }
}
