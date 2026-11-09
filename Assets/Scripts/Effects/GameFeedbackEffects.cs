using DG.Tweening;
using UnityEngine;
using OndeVai.Gameplay;

namespace OndeVai.Effects
{
    /// <summary>
    /// Visual and haptic reactions to gameplay events: sparkles on spawn, stars on correct answers,
    /// a soft shake and tears on mistakes, timer ticks, and a confetti celebration when the match ends.
    /// </summary>
    public sealed class GameFeedbackEffects : MonoBehaviour
    {
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

        private GameController _gameController;
        private Vector3 _cameraRestPosition;
        private Quaternion _cameraRestRotation;
        private Color[] _wrongColors;
        private Color[] _tearColors;

        private void Start()
        {
            if (_camera == null)
                _camera = Camera.main;

            _wrongColors = new[] { _wrongColor };
            if (_camera != null)
            {
                _cameraRestPosition = _camera.transform.position;
                _cameraRestRotation = _camera.transform.rotation;
            }

            if (_burstPool == null)
                Debug.LogError("GameFeedbackEffects has no SpriteBurstPool assigned. Particle effects are disabled.", this);

            if (_gameTimer != null)
                _gameTimer.SecondElapsed += HandleSecondElapsed;

            _gameController = GameController.Instance;
            if (_gameController == null)
            {
                Debug.LogError("GameFeedbackEffects could not find the GameController.", this);
                return;
            }

            _gameController.OnItemSpawned += HandleItemSpawned;
            _gameController.OnItemSorted += HandleItemSorted;
            _gameController.OnGameWon += HandleGameWon;
            _gameController.OnGameLost += HandleGameLost;
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

            EmitFirework(position + new Vector3(-0.55f, 0.45f, 0f));
            EmitFirework(position + new Vector3(0.55f, 0.65f, 0f));
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

            if (!result.DuringTutorial)
                EmitTears(result.Position, 3);

            ShakeCamera();
            Vibrate();
        }

        private void HandleGameWon()
        {
            Celebrate();
        }

        private void HandleGameLost()
        {
            if (_camera != null)
                EmitTears(_camera.transform.position + Vector3.up * 0.6f, 14);

            Celebrate();
        }

        private void HandleSecondElapsed(int secondsRemaining)
        {
            if (_camera == null || !_camera.orthographic)
                return;

            Vector3 position = _camera.transform.position + Vector3.up * (_camera.orthographicSize - 0.35f);
            Color[] colors = secondsRemaining <= 5 ? _confettiColors : _correctColors;
            Emit(position, new BurstSettings
            {
                Sprite = secondsRemaining <= 5 ? ProceduralSprites.Star : ProceduralSprites.Circle,
                Colors = colors,
                Count = secondsRemaining <= 5 ? 5 : 2,
                MinSpeed = 0.3f,
                MaxSpeed = 0.9f,
                Lifetime = 0.45f,
                StartSize = 0.16f,
                AngleRange = new Vector2(235f, 305f)
            });
        }

        private void EmitFirework(Vector3 position)
        {
            Emit(position, new BurstSettings
            {
                Sprite = ProceduralSprites.Star,
                Colors = _confettiColors,
                Count = 10,
                MinSpeed = 1.8f,
                MaxSpeed = 3.4f,
                Lifetime = 0.9f,
                Gravity = 1.2f,
                StartSize = 0.24f,
                Spin = 540f,
                AngleRange = new Vector2(0f, 360f)
            });
        }

        private void EmitTears(Vector3 position, int count)
        {
            _tearColors ??= new[] { _tearColor };
            Emit(position, new BurstSettings
            {
                Sprite = ProceduralSprites.Tear,
                Colors = _tearColors,
                Count = count,
                MinSpeed = 0.7f,
                MaxSpeed = 1.5f,
                Lifetime = 0.8f,
                Gravity = 2.8f,
                StartSize = 0.28f,
                Spin = 18f,
                AngleRange = new Vector2(230f, 310f)
            });
        }

        private void Celebrate()
        {
            if (_camera == null || !_camera.orthographic)
                return;

            float halfWidth = _camera.orthographicSize * _camera.aspect;
            Vector3 top = _camera.transform.position + Vector3.up * (_camera.orthographicSize + 0.5f);
            top.z = 0f;
            for (int i = 0; i < 6; i++)
            {
                Vector3 position = top + Vector3.right * Mathf.Lerp(-halfWidth, halfWidth, (i + 0.5f) / 6f);
                EmitFirework(position);
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

        private void Emit(Vector3 position, BurstSettings settings)
        {
            position.z = 0f;
            if (_burstPool != null)
                _burstPool.Emit(position, settings);
        }

        private void OnDestroy()
        {
            if (_gameTimer != null)
                _gameTimer.SecondElapsed -= HandleSecondElapsed;

            if (_gameController == null)
                return;

            _gameController.OnItemSpawned -= HandleItemSpawned;
            _gameController.OnItemSorted -= HandleItemSorted;
            _gameController.OnGameWon -= HandleGameWon;
            _gameController.OnGameLost -= HandleGameLost;
        }
    }
}
