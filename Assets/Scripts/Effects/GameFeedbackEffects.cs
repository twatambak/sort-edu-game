using DG.Tweening;
using UnityEngine;

namespace OndeVai.Effects
{
    /// <summary>
    /// Visual and haptic reactions to gameplay events: sparkles on spawn, stars on correct answers,
    /// a soft shake on mistakes and confetti when the match is won.
    /// </summary>
    public sealed class GameFeedbackEffects : MonoBehaviour
    {
        [SerializeField] private SpriteBurstPool _burstPool;
        [SerializeField] private Camera _camera;

        [Header("Correct")]
        [SerializeField] private Color[] _correctColors =
        {
            new(1f, 0.85f, 0.2f, 1f),
            new(1f, 0.6f, 0.2f, 1f),
            new(1f, 1f, 0.7f, 1f)
        };

        [Header("Wrong")]
        [SerializeField] private Color _wrongColor = new(0.75f, 0.75f, 0.8f, 0.9f);
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

        private GameController _gameController;
        private Vector3 _cameraRestPosition;
        private Color[] _wrongColors;

        private void Start()
        {
            if (_camera == null)
                _camera = Camera.main;

            _wrongColors = new[] { _wrongColor };
            if (_camera != null)
                _cameraRestPosition = _camera.transform.position;

            if (_burstPool == null)
                Debug.LogError("GameFeedbackEffects has no SpriteBurstPool assigned. Particle effects are disabled.", this);

            _gameController = GameController.Instance;
            if (_gameController == null)
            {
                Debug.LogError("GameFeedbackEffects could not find the GameController.", this);
                return;
            }

            _gameController.OnItemSpawned += HandleItemSpawned;
            _gameController.OnItemSorted += HandleItemSorted;
            _gameController.OnGameWon += HandleGameWon;
        }

        private void HandleItemSpawned(Item item)
        {
            if (item == null)
                return;

            Emit(item.transform.position, new BurstSettings
            {
                Sprite = ProceduralSprites.Circle,
                Colors = _correctColors,
                Count = 6,
                MinSpeed = 1.2f,
                MaxSpeed = 2f,
                Lifetime = 0.35f,
                StartSize = 0.18f,
                AngleRange = new Vector2(0f, 360f)
            });
        }

        private void HandleItemSorted(ItemSortResult result)
        {
            if (result.Correct)
                PlayCorrect(result);
            else
                PlayWrong(result);
        }

        private void PlayCorrect(ItemSortResult result)
        {
            Vector3 position = result.Storage != null ? result.Storage.DropPoint : result.Position;
            Emit(position, new BurstSettings
            {
                Sprite = ProceduralSprites.Star,
                Colors = _correctColors,
                Count = 14,
                MinSpeed = 2.5f,
                MaxSpeed = 5f,
                Lifetime = 0.7f,
                Gravity = 6f,
                StartSize = 0.45f,
                Spin = 360f,
                AngleRange = new Vector2(20f, 160f)
            });

            Emit(position, new BurstSettings
            {
                Sprite = ProceduralSprites.Ring,
                Colors = _correctColors,
                Count = 1,
                Lifetime = 0.35f,
                StartSize = 1.6f,
                AngleRange = Vector2.zero
            });
        }

        private void PlayWrong(ItemSortResult result)
        {
            Emit(result.Position, new BurstSettings
            {
                Sprite = ProceduralSprites.Circle,
                Colors = _wrongColors,
                Count = 8,
                MinSpeed = 1f,
                MaxSpeed = 2.2f,
                Lifetime = 0.45f,
                Gravity = -1f,
                StartSize = 0.3f,
                AngleRange = new Vector2(0f, 360f)
            });

            ShakeCamera();
            Vibrate();
        }

        private void HandleGameWon()
        {
            if (_camera == null || !_camera.orthographic)
                return;

            float halfWidth = _camera.orthographicSize * _camera.aspect;
            Vector3 top = _camera.transform.position + Vector3.up * (_camera.orthographicSize + 0.5f);
            top.z = 0f;
            for (int i = 0; i < 6; i++)
            {
                Vector3 position = top + Vector3.right * Mathf.Lerp(-halfWidth, halfWidth, (i + 0.5f) / 6f);
                Emit(position, new BurstSettings
                {
                    Sprite = i % 2 == 0 ? ProceduralSprites.Star : ProceduralSprites.Circle,
                    Colors = _confettiColors,
                    Count = 12,
                    MinSpeed = 0.5f,
                    MaxSpeed = 2.5f,
                    Lifetime = 2.2f,
                    Gravity = 3.5f,
                    StartSize = 0.3f,
                    Spin = 540f,
                    AngleRange = new Vector2(200f, 340f)
                });
            }
        }

        private void ShakeCamera()
        {
            if (_camera == null || _shakeStrength <= 0f)
                return;

            Transform cameraTransform = _camera.transform;
            cameraTransform.DOKill(true);
            cameraTransform.position = _cameraRestPosition;
            cameraTransform.DOShakePosition(0.25f, new Vector3(_shakeStrength, _shakeStrength, 0f), 20, 90f, false, true)
                .SetLink(_camera.gameObject)
                .OnKill(() => cameraTransform.position = _cameraRestPosition);
        }

        private void Vibrate()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (_gameController != null && _gameController.Settings.Current.VibrationEnabled)
                Handheld.Vibrate();
#endif
        }

        private void Emit(Vector3 position, BurstSettings settings)
        {
            position.z = 0f;
            if (_burstPool != null)
                _burstPool.Emit(position, settings);
        }

        private void OnDestroy()
        {
            if (_gameController == null)
                return;

            _gameController.OnItemSpawned -= HandleItemSpawned;
            _gameController.OnItemSorted -= HandleItemSorted;
            _gameController.OnGameWon -= HandleGameWon;
        }
    }
}
