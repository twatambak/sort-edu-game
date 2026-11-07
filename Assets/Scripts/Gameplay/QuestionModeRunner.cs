using DG.Tweening;
using UnityEngine;

namespace OndeVai.Gameplay
{
    /// <summary>
    /// Game mode 2: one item at a time is presented at the question point and the child taps the
    /// storage it belongs to. The item flies to the chosen storage; wrong answers return it to the center.
    /// </summary>
    public sealed class QuestionModeRunner : IGameModeRunner
    {
        private const float FlyDuration = 0.35f;
        private const float ReturnDuration = 0.4f;
        private const float NextQuestionDelay = 0.45f;

        private readonly GameModeContext _context;
        private GameConfiguration _config;
        private Item _current;
        private bool _isAnswering;
        private Tween _pending;

        public QuestionModeRunner(GameModeContext context)
        {
            _context = context;
        }

        public bool EjectOnError => false;

        public Item FocusItem => _isAnswering ? null : _current;

        public void Begin(GameConfiguration config)
        {
            _config = config;
            if (_context.QuestionPoint == null)
                Debug.LogError("Question mode has no question point assigned. Items will appear at the world origin.");

            SpawnQuestion();
        }

        public void End()
        {
            _pending?.Kill();
            _pending = null;
            _isAnswering = false;
            _current = null;
        }

        public void HandleStorageSelected(Storage storage)
        {
            if (_current == null || _isAnswering || storage == null || !_context.CanAcceptInput())
                return;

            _isAnswering = true;
            Item item = _current;
            item.transform.DOKill();
            item.transform.rotation = Quaternion.identity;
            _pending = item.transform.DOMove(storage.DropPoint, FlyDuration)
                .SetEase(Ease.InOutQuad)
                .SetLink(item.gameObject)
                .OnComplete(() =>
                {
                    _pending = null;
                    _isAnswering = false;
                    if (item != null)
                        _context.Submit(item, storage);
                });
        }

        public void HandleItemResolved(Item item, bool correct)
        {
            if (item != _current)
                return;

            if (correct)
            {
                _current = null;
                _pending = DOVirtual.DelayedCall(NextQuestionDelay, () =>
                {
                    _pending = null;
                    SpawnQuestion();
                });
                return;
            }

            ReturnToCenter(item);
        }

        private void SpawnQuestion()
        {
            Vector3 position = _context.QuestionPoint != null ? _context.QuestionPoint.position : Vector3.zero;
            _current = _context.Spawner.SpawnSingle(position, _config.ItemGroupA, _config.ItemGroupB);
            if (_current == null)
                return;

            _current.SetDraggable(false);
            _current.SetKinematic(true);
            _current.BaseScale = _context.QuestionItemScale;
            PlayIdle(_current);
            _context.NotifySpawned?.Invoke(_current);
        }

        private void ReturnToCenter(Item item)
        {
            if (item == null)
                return;

            _isAnswering = true;
            Vector3 center = _context.QuestionPoint != null ? _context.QuestionPoint.position : Vector3.zero;
            _pending = item.transform.DOMove(center, ReturnDuration)
                .SetEase(Ease.OutBack)
                .SetLink(item.gameObject)
                .OnComplete(() =>
                {
                    _pending = null;
                    _isAnswering = false;
                    if (item == null)
                        return;

                    item.RestartResponseTimer();
                    PlayIdle(item);
                });
        }

        private static void PlayIdle(Item item)
        {
            item.transform.DOKill();
            item.transform.rotation = Quaternion.identity;
            item.transform.DORotate(new Vector3(0f, 0f, 6f), 0.8f)
                .From(new Vector3(0f, 0f, -6f))
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetLink(item.gameObject);
        }
    }
}
