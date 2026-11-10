using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace OndeVai.Gameplay
{
    /// <summary>
    /// Runs a one-card match using left and right swipes. The card follows the finger and tilts,
    /// the side it leans toward lights up, and releasing past the swipe distance answers.
    /// </summary>
    public sealed class SwipeCardModeRunner : IGameModeRunner, ISwipeCardListener
    {
        // Fraction of the screen width, so the gesture feels the same on every resolution.
        private const float SwipeDistanceScreenFraction = 0.08f;
        private const float ReturnDuration = 0.3f;
        private const float ExitDuration = 0.28f;
        private const float ExitDistance = 5f;
        private const float ExitTilt = 35f;
        private const float NextCardDelay = 0.3f;
        private const float CardScale = 2f;
        private const float MaxDragX = 1.8f;
        private const float MaxDragY = 0.35f;
        private const float VerticalFollow = 0.2f;
        private const float MaxTilt = 14f;
        private const float FallbackUnitsPerPixel = 0.005f;

        private readonly GameModeContext _context;
        private readonly List<ItemGroup> _groups = new(2);
        private GameConfiguration _config;
        private Camera _camera;
        private SortingSourcePresentation _leftPanel;
        private SortingSourcePresentation _rightPanel;
        private SwipeHintArrows _arrows;
        private Item _current;
        private Vector3 _home;
        private float _swipeDistancePixels;
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
            _camera = Camera.main;
            _groups.Clear();
            for (int i = 0; i < 2; i++)
                _groups.Add(config.GetGroup(i));

            _home = _context.QuestionPoint != null
                ? _context.QuestionPoint.position
                : _context.Spawner.DefaultSpawnPosition;

            if (_context.QuestionPoint != null)
                _context.QuestionPoint.gameObject.SetActive(true);

            _rightPanel = FindPanel(config.ItemGroupA);
            _leftPanel = FindPanel(config.ItemGroupB);
            _arrows = new SwipeHintArrows(
                _context.QuestionPoint != null ? _context.QuestionPoint.parent : null,
                _home,
                _leftPanel != null ? _leftPanel.CategoryColor : Color.white,
                _rightPanel != null ? _rightPanel.CategoryColor : Color.white);

            SpawnCard();
        }

        public void End()
        {
            _pending?.Kill();
            _pending = null;

            // Clearing the card first makes the input callbacks raised while it is disabled no-ops.
            Item card = _current;
            _current = null;
            _isAnswering = false;
            SetLean(0f);

            if (card != null)
            {
                DisableInput(card);
                card.Despawn();
            }

            _arrows?.Dispose();
            _arrows = null;
        }

        public void HandleStorageSelected(SortingSource storage)
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

        void ISwipeCardListener.OnSwipeStarted(Item item)
        {
            if (item != _current || _isAnswering)
                return;

            _pending?.Kill();
            _pending = null;
            item.SetLifted(true);
            _arrows?.SetVisible(false);
            _context.NotifyPicked?.Invoke(item);
        }

        void ISwipeCardListener.OnSwipeMoved(Item item, Vector2 screenDelta)
        {
            if (item != _current || _isAnswering)
                return;

            float unitsPerPixel = UnitsPerPixel();
            float x = Mathf.Clamp(screenDelta.x * unitsPerPixel, -MaxDragX, MaxDragX);
            float y = Mathf.Clamp(screenDelta.y * unitsPerPixel * VerticalFollow, -MaxDragY, MaxDragY);
            item.transform.SetPositionAndRotation(
                _home + new Vector3(x, y, 0f),
                Quaternion.Euler(0f, 0f, -x / MaxDragX * MaxTilt));

            SetLean(Mathf.Clamp(screenDelta.x / _swipeDistancePixels, -1f, 1f));
        }

        void ISwipeCardListener.OnSwipeEnded(Item item, SwipeDirection? direction)
        {
            if (item != _current || _isAnswering)
                return;

            item.SetLifted(false);
            _context.NotifyReleased?.Invoke(item);
            SetLean(0f);

            if (direction.HasValue && TrySubmit(item, direction.Value))
                return;

            ReturnHome(item);
        }

        private bool TrySubmit(Item item, SwipeDirection direction)
        {
            if (!_context.CanAcceptInput())
                return false;

            if (!SwipeDirectionUtility.TryGetGroup(_config, direction, out ItemGroup group))
            {
                Debug.LogError($"Swipe direction {direction} is not available in {_config.Mode}.", item);
                return false;
            }

            _lastDirection = direction;
            _isAnswering = true;
            DisableInput(item);
            _context.SubmitGroup(item, group);
            return true;
        }

        private void SpawnCard()
        {
            _isAnswering = false;
            _current = _context.Spawner.SpawnSingle(_home, _groups);
            if (_current == null)
                return;

            _current.SetDraggable(false);
            _current.SetKinematic(true);
            _current.BaseScale = CardScale;

            if (!_current.TryGetComponent(out SwipeCardInput _))
                _current.gameObject.AddComponent<SwipeCardInput>();

            EnableInput(_current);
            _arrows?.SetVisible(true);
            PunchCardSlot();
            _context.NotifySpawned?.Invoke(_current);
        }

        private void AnimateAccepted(Item item)
        {
            Vector3 side = SwipeDirectionUtility.Vector(_lastDirection);
            Vector3 destination = _home + side * ExitDistance + Vector3.up * 0.4f;
            Sequence sequence = DOTween.Sequence();
            sequence.Join(item.transform.DOMove(destination, ExitDuration).SetEase(Ease.InQuad));
            sequence.Join(item.transform.DORotate(new Vector3(0f, 0f, -side.x * ExitTilt), ExitDuration));
            sequence.Join(DOTween.To(() => item.PresentationScale, value => item.PresentationScale = value, 0.4f, ExitDuration).SetEase(Ease.InQuad));
            _pending = sequence.SetTarget(item).SetLink(item.gameObject).OnComplete(() =>
            {
                _pending = null;
                if (item != null)
                    item.Despawn();

                _current = null;
                ScheduleNextCard();
            });
        }

        private void AnimateRetry(Item item)
        {
            Sequence sequence = DOTween.Sequence();
            sequence.Append(item.transform.DOMove(_home, ReturnDuration * 0.7f).SetEase(Ease.OutQuad));
            sequence.Join(item.transform.DORotate(Vector3.zero, ReturnDuration * 0.7f));
            sequence.Append(item.transform.DOShakePosition(ReturnDuration, new Vector3(0.2f, 0f, 0f), 18, 0f, false, true));
            _pending = sequence.SetTarget(item).SetLink(item.gameObject).OnComplete(() =>
            {
                _pending = null;
                if (item == null || item != _current)
                    return;

                item.transform.position = _home;
                item.RestartResponseTimer();
                _isAnswering = false;
                EnableInput(item);
                _arrows?.SetVisible(true);
            });
        }

        private void ReturnHome(Item item)
        {
            Sequence sequence = DOTween.Sequence();
            sequence.Join(item.transform.DOMove(_home, ReturnDuration).SetEase(Ease.OutBack));
            sequence.Join(item.transform.DORotate(Vector3.zero, ReturnDuration));
            _pending = sequence.SetTarget(item).SetLink(item.gameObject).OnComplete(() =>
            {
                _pending = null;
                _arrows?.SetVisible(true);
            });
        }

        private void ScheduleNextCard()
        {
            // Scaled time: with unscaled time the delay could run out during a pause and the next
            // card would never appear.
            _pending = DOVirtual.DelayedCall(NextCardDelay, () =>
            {
                _pending = null;
                SpawnCard();
            }, false);
        }

        private void PunchCardSlot()
        {
            Transform slot = _context.QuestionPoint;
            if (slot == null)
                return;

            slot.DOKill(true);
            slot.DOPunchScale(slot.localScale * 0.08f, 0.3f, 6, 0.6f).SetLink(slot.gameObject);
        }

        private void SetLean(float lean)
        {
            if (_rightPanel != null)
                _rightPanel.SetLean(lean);

            if (_leftPanel != null)
                _leftPanel.SetLean(-lean);
        }

        private SortingSourcePresentation FindPanel(ItemGroup group)
        {
            SortingSource storage = _context.FindStorage?.Invoke(group);
            return storage != null ? storage.Presentation : null;
        }

        private float UnitsPerPixel()
        {
            if (_camera == null || !_camera.orthographic || Screen.height <= 0)
                return FallbackUnitsPerPixel;

            return _camera.orthographicSize * 2f / Screen.height;
        }

        private void EnableInput(Item item)
        {
            if (!item.TryGetComponent(out SwipeCardInput input))
                return;

            _swipeDistancePixels = Mathf.Max(1f, Screen.width * SwipeDistanceScreenFraction);
            input.Configure(_swipeDistancePixels, this);
        }

        private static void DisableInput(Item item)
        {
            if (item != null && item.TryGetComponent(out SwipeCardInput input))
                input.enabled = false;
        }
    }
}
