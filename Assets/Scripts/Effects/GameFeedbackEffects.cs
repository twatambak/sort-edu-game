using DG.Tweening;
using OndeVai.Audio;
using OndeVai.Gameplay;
using UnityEngine;

namespace OndeVai.Effects
{
    /// <summary>
    /// Visual and haptic reactions to gameplay events: sparkles on spawn, a trail while an item is
    /// held, stars on correct answers, a soft shake and tears on mistakes, streak fireworks, timer
    /// ticks, a nudge when the child is idle and a confetti celebration when the match ends.
    /// The particle recipes live in <see cref="FeedbackBursts"/>.
    /// </summary>
    public sealed class GameFeedbackEffects : MonoBehaviour
    {
        private const int UrgentSeconds = 5;
        private const int FirstStreakMilestone = 3;
        private const int StreakMilestoneStep = 5;

        [SerializeField] private SpriteBurstPool _burstPool;
        [SerializeField] private Camera _camera;
        [SerializeField] private GameTimer _gameTimer;

        [Header("Correct")]
        [SerializeField] private Color[] _correctColors =
        {
            new(1f, 0.85f, 0.2f, 1f),
            new(1f, 0.6f, 0.2f, 1f),
            new(1f, 1f, 0.7f, 1f)
        };

        [Header("Wrong")]
        [SerializeField] private Color _wrongColor = new(0.75f, 0.75f, 0.8f, 0.9f);
        [SerializeField] private Color _tearColor = new(0.35f, 0.72f, 1f, 1f);
        [SerializeField, Min(0f)] private float _shakeStrength = 0.12f;

        [Header("Win")]
        [SerializeField] private Color[] _confettiColors =
        {
            new(0.98f, 0.36f, 0.36f, 1f),
            new(0.36f, 0.75f, 0.98f, 1f),
            new(0.45f, 0.9f, 0.5f, 1f),
            new(1f, 0.85f, 0.2f, 1f),
            new(0.8f, 0.5f, 0.95f, 1f)
        };

        [Header("Idle")]
        [Tooltip("Seconds without any interaction before an item hops to call the child's attention. 0 disables it.")]
        [SerializeField, Min(0f)] private float _idleNudgeSeconds = 6f;

        private readonly Color[] _categoryColors = { Color.white, Color.white };
        private readonly Color[] _glowColors = { new(1f, 1f, 0.8f, 0.55f) };
        private GameController _gameController;
        private HeldItemTrail _trail;
        private IdleNudge _idleNudge;
        private Vector3 _cameraRestPosition;
        private Quaternion _cameraRestRotation;
        private Color[] _wrongColors;
        private Color[] _tearColors;

        private void Start()
        {
            if (_camera == null)
                _camera = Camera.main;

            _wrongColors = new[] { _wrongColor };
            _tearColors = new[] { _tearColor };
            if (_camera != null)
            {
                _cameraRestPosition = _camera.transform.position;
                _cameraRestRotation = _camera.transform.rotation;
            }

            if (_burstPool == null)
                Debug.LogError("GameFeedbackEffects has no SpriteBurstPool assigned. Particle effects are disabled.", this);

            _trail = new HeldItemTrail(HandleTrailStep);
            _idleNudge = new IdleNudge(_idleNudgeSeconds, NudgeIdleItem);

            if (_gameTimer != null)
                _gameTimer.SecondElapsed += HandleSecondElapsed;

            _gameController = GameController.Instance;
            if (_gameController == null)
            {
                Debug.LogError("GameFeedbackEffects could not find the GameController.", this);
                return;
            }

            _gameController.OnGameStarted += HandleGameStarted;
            _gameController.OnItemSpawned += HandleItemSpawned;
            _gameController.OnItemPicked += HandleItemPicked;
            _gameController.OnItemReleased += HandleItemReleased;
            _gameController.OnItemSorted += HandleItemSorted;
            _gameController.OnGameWon += HandleGameWon;
            _gameController.OnGameLost += HandleGameLost;
            _gameController.OnGameAborted += StopMatchEffects;
        }

        private void HandleGameStarted(GameConfiguration config)
        {
            _trail.End();
            _idleNudge.Arm();
        }

        private void HandleItemSpawned(Item item)
        {
            if (item == null)
                return;

            Emit(item.transform.position, FeedbackBursts.SpawnSparkles(CategoryColors(item)));
            _idleNudge.Arm();
        }

        private void HandleItemPicked(Item item)
        {
            if (item == null)
                return;

            Emit(item.transform.position, FeedbackBursts.PickupRing(CategoryColors(item)));
            _trail.Begin(item);
            _idleNudge.Cancel();
        }

        private void HandleItemReleased(Item item)
        {
            _trail.End();
            _idleNudge.Arm();
        }

        private void HandleTrailStep(Item item, Vector3 position)
        {
            Emit(position, FeedbackBursts.DragTrail(CategoryColors(item)));
        }

        private void HandleItemSorted(ItemSortResult result)
        {
            _trail.End();
            _idleNudge.Arm();
            if (result.Correct)
                PlayCorrect(result);
            else
                PlayWrong(result);
        }

        private void PlayCorrect(ItemSortResult result)
        {
            Vector3 position = result.Storage != null ? result.Storage.DropPoint : result.Position;
            Emit(position, FeedbackBursts.Glow(_glowColors));
            Emit(position, FeedbackBursts.CorrectStars(_correctColors));
            Emit(position, FeedbackBursts.Ring(_correctColors, 0.9f));
            Emit(position + new Vector3(-0.55f, 0.45f, 0f), FeedbackBursts.Firework(_confettiColors));
            Emit(position + new Vector3(0.55f, 0.65f, 0f), FeedbackBursts.Firework(_confettiColors));

            if (IsStreakMilestone(result.Streak))
                PlayStreak(position);
        }

        private static bool IsStreakMilestone(int streak)
        {
            return streak == FirstStreakMilestone || (streak > 0 && streak % StreakMilestoneStep == 0);
        }

        /// <summary>Extra fireworks that reward several right answers in a row.</summary>
        private void PlayStreak(Vector3 position)
        {
            Emit(position, FeedbackBursts.Ring(_confettiColors, 1.6f));
            for (int i = 0; i < 3; i++)
            {
                float angle = (90f + (i - 1) * 55f) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 1.3f;
                Emit(position + offset, FeedbackBursts.Firework(_confettiColors));
            }
        }

        private void PlayWrong(ItemSortResult result)
        {
            Emit(result.Position, FeedbackBursts.WrongPuff(_wrongColors));

            if (!result.DuringTutorial)
                Emit(result.Position, FeedbackBursts.Tears(_tearColors, 3));

            ShakeCamera();
            Vibrate();
        }

        private void HandleGameWon()
        {
            StopMatchEffects();
            Celebrate();
        }

        private void HandleGameLost()
        {
            StopMatchEffects();
            if (_camera != null)
                Emit(_camera.transform.position + Vector3.up * 0.6f, FeedbackBursts.Tears(_tearColors, 14));

            Celebrate();
        }

        private void StopMatchEffects()
        {
            _trail.End();
            _idleNudge.Cancel();
        }

        private void HandleSecondElapsed(int secondsRemaining)
        {
            bool urgent = secondsRemaining <= UrgentSeconds;
            if (urgent && secondsRemaining > 0)
                GameAudio.Instance?.PlayTick();

            if (_camera == null || !_camera.orthographic)
                return;

            Vector3 position = _camera.transform.position + Vector3.up * (_camera.orthographicSize - 0.35f);
            Emit(position, FeedbackBursts.TimerTick(urgent ? _confettiColors : _correctColors, urgent));
        }

        /// <summary>Makes one waiting item hop when the child has not touched anything for a while.</summary>
        private void NudgeIdleItem()
        {
            _idleNudge.Arm();
            if (_gameController == null || !_gameController.CanAcceptInput || _gameController.IsTutorialActive)
                return;

            System.Collections.Generic.IReadOnlyList<Item> items = _gameController.ActiveItems;
            if (items.Count == 0)
                return;

            Item item = items[Random.Range(0, items.Count)];
            if (item == null || item.IsLifted || item.IsResolving)
                return;

            item.Punch(1.2f, 0.5f);
            Emit(item.transform.position, FeedbackBursts.SpawnSparkles(CategoryColors(item)));
        }

        private void Celebrate()
        {
            if (_camera == null || !_camera.orthographic)
                return;

            const int columns = 6;
            float halfWidth = _camera.orthographicSize * _camera.aspect;
            Vector3 top = _camera.transform.position + Vector3.up * (_camera.orthographicSize + 0.5f);
            for (int i = 0; i < columns; i++)
            {
                Vector3 position = top + Vector3.right * Mathf.Lerp(-halfWidth, halfWidth, (i + 0.5f) / columns);
                Emit(position, FeedbackBursts.Firework(_confettiColors));
                Emit(position, FeedbackBursts.Confetti(_confettiColors, i % 2 == 0));
            }
        }

        private void ShakeCamera()
        {
            if (_camera == null || _shakeStrength <= 0f)
                return;

            Transform cameraTransform = _camera.transform;
            cameraTransform.DOKill(true);
            cameraTransform.position = _cameraRestPosition;
            cameraTransform.rotation = _cameraRestRotation;
            Sequence shake = DOTween.Sequence();
            shake.Join(cameraTransform.DOShakePosition(0.25f, new Vector3(_shakeStrength, _shakeStrength, 0f), 20, 90f, false, true));
            shake.Join(cameraTransform.DOShakeRotation(0.25f, new Vector3(0f, 0f, 2.5f), 16, 90f, false));
            shake.SetLink(_camera.gameObject)
                .OnComplete(RestoreCamera)
                .OnKill(RestoreCamera);
        }

        private void RestoreCamera()
        {
            if (_camera == null)
                return;

            _camera.transform.position = _cameraRestPosition;
            _camera.transform.rotation = _cameraRestRotation;
        }

        private void Vibrate()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (_gameController != null && _gameController.Settings.Current.VibrationEnabled)
                Handheld.Vibrate();
#endif
        }

        /// <summary>The item's category color plus white, in a shared array to avoid allocations.</summary>
        private Color[] CategoryColors(Item item)
        {
            _categoryColors[0] = item != null && item.Type != null ? item.Type.Color : Color.white;
            return _categoryColors;
        }

        private void Emit(Vector3 position, BurstSettings settings)
        {
            position.z = 0f;
            if (_burstPool != null)
                _burstPool.Emit(position, settings);
        }

        private void OnDestroy()
        {
            _trail?.End();
            _idleNudge?.Cancel();

            if (_gameTimer != null)
                _gameTimer.SecondElapsed -= HandleSecondElapsed;

            if (_gameController == null)
                return;

            _gameController.OnGameStarted -= HandleGameStarted;
            _gameController.OnItemSpawned -= HandleItemSpawned;
            _gameController.OnItemPicked -= HandleItemPicked;
            _gameController.OnItemReleased -= HandleItemReleased;
            _gameController.OnItemSorted -= HandleItemSorted;
            _gameController.OnGameWon -= HandleGameWon;
            _gameController.OnGameLost -= HandleGameLost;
            _gameController.OnGameAborted -= StopMatchEffects;
        }
    }
}
