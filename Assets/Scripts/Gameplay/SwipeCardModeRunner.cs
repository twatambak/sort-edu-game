using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace OndeVai.Gameplay
{
    /// <summary>Runs a one-card swipe match using either two horizontal or four cardinal directions.</summary>
    public sealed class SwipeCardModeRunner : IGameModeRunner
    {
        private const float MinimumSwipeDistance = 70f;
        private const float ReturnDuration = 0.32f;
        private const float ExitDuration = 0.28f;
        private const float NextCardDelay = 0.3f;
        private const float CardScale = 1.5f;

        private readonly GameModeContext _context;
        private readonly List<ItemGroup> _groups = new(4);
        private GameConfiguration _config;
        private Item _current;
        private SwipeDirection _lastDirection;
        private bool _isAnswering;
        private Tween _pending;

        public SwipeCardModeRunner(GameModeContext context)
        {
            _context = context;
        }

        public bool EjectOnError => false;

        public Item FocusItem => _isAnswering ? null : _current;

        public void Begin(GameConfiguration config)
        {
            _config = config;
            _groups.Clear();
            for (int i = 0; i < config.GroupCount; i++)
                _groups.Add(config.GetGroup(i));

            SpawnCard();
        }

        public void End()
        {
            _pending?.Kill();
            _pending = null;
            DisableInput(_current);
            if (_current != null)
                _current.Despawn();

            _current = null;
            _isAnswering = false;
        }

        public void HandleStorageSelected(Storage storage)
        {
        }

        public void HandleItemResolved(Item item, bool correct)
        {
            if (item != _current)
                return;

            _isAnswering = true;
            if (correct)
                AnimateAccepted(item);
            else
                AnimateRetry(item);
        }

        private void HandleSwipe(Item item, SwipeDirection direction)
        {
            if (item != _current || _isAnswering || !_context.CanAcceptInput())
                return;

            if (!SwipeDirectionUtility.TryGetGroup(_config, direction, out ItemGroup group))
            {
                Debug.LogError($"Swipe direction {direction} is not available in {_config.Mode}.", item);
                return;
            }

            _lastDirection = direction;
            _isAnswering = true;
            DisableInput(item);
            _context.SubmitGroup(item, group);
        }

        private void SpawnCard()
        {
            Vector3 position = _context.QuestionPoint != null
                ? _context.QuestionPoint.position
                : _context.Spawner.DefaultSpawnPosition;
            _current = _context.Spawner.SpawnSingle(position, _groups);
            if (_current == null)
                return;

            _current.SetDraggable(false);
            _current.SetKinematic(true);
            _current.BaseScale = CardScale;

            SwipeCardInput input = _current.GetComponent<SwipeCardInput>();
            if (input == null)
                input = _current.gameObject.AddComponent<SwipeCardInput>();

            input.Configure(
                _config.Mode == GameMode.FourWaySwipe,
                MinimumSwipeDistance,
                HandleSwipe,
                item => _context.NotifyPicked?.Invoke(item),
                item => _context.NotifyReleased?.Invoke(item));
            _context.NotifySpawned?.Invoke(_current);
        }

        private void AnimateAccepted(Item item)
        {
            Vector3 destination = item.transform.position + (Vector3)SwipeDirectionUtility.Vector(_lastDirection) * 2.4f;
            Sequence sequence = DOTween.Sequence();
            sequence.Join(item.transform.DOMove(destination, ExitDuration).SetEase(Ease.InQuad));
            sequence.Join(item.transform.DORotate(new Vector3(0f, 0f, RotationFor(_lastDirection)), ExitDuration));
            sequence.Join(DOTween.To(() => item.PresentationScale, value => item.PresentationScale = value, 0f, ExitDuration));
            _pending = sequence.SetTarget(item).SetLink(item.gameObject).OnComplete(() =>
            {
                _pending = null;
                DisableInput(item);
                if (item != null)
                    item.Despawn();
                _current = null;
                ScheduleNextCard();
            });
        }

        private void AnimateRetry(Item item)
        {
            Vector3 origin = item.transform.position;
            Vector3 nudge = origin + (Vector3)SwipeDirectionUtility.Vector(_lastDirection) * 0.45f;
            Sequence sequence = DOTween.Sequence();
            sequence.Append(item.transform.DOMove(nudge, ReturnDuration * 0.35f).SetEase(Ease.OutQuad));
            sequence.Join(item.transform.DORotate(new Vector3(0f, 0f, RotationFor(_lastDirection)), ReturnDuration * 0.35f));
            sequence.Append(item.transform.DOMove(origin, ReturnDuration * 0.65f).SetEase(Ease.OutBack));
            sequence.Join(item.transform.DORotate(Vector3.zero, ReturnDuration * 0.65f));
            _pending = sequence.SetTarget(item).SetLink(item.gameObject).OnComplete(() =>
            {
                _pending = null;
                if (item == null)
                    return;

                item.RestartResponseTimer();
                _isAnswering = false;
                ConfigureInput(item);
            });
        }

        private void ScheduleNextCard()
        {
            _pending = DOVirtual.DelayedCall(NextCardDelay, () =>
            {
                _pending = null;
                if (_context.CanAcceptInput())
                    SpawnCard();
            });
        }

        private void ConfigureInput(Item item)
        {
            SwipeCardInput input = item.GetComponent<SwipeCardInput>();
            if (input == null)
                return;

            input.Configure(
                _config.Mode == GameMode.FourWaySwipe,
                MinimumSwipeDistance,
                HandleSwipe,
                card => _context.NotifyPicked?.Invoke(card),
                card => _context.NotifyReleased?.Invoke(card));
        }

        private static void DisableInput(Item item)
        {
            if (item != null && item.TryGetComponent(out SwipeCardInput input))
                input.enabled = false;
        }

        private static float RotationFor(SwipeDirection direction)
        {
            return direction switch
            {
                SwipeDirection.Left => 12f,
                SwipeDirection.Right => -12f,
                SwipeDirection.Up => 8f,
                SwipeDirection.Down => -8f,
                _ => 0f
            };
        }
    }
}
