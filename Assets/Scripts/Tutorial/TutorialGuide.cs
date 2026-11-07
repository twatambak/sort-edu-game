using OndeVai.Effects;
using OndeVai.Gameplay;
using UnityEngine;

namespace OndeVai.Tutorial
{
    /// <summary>
    /// Shows a pointer that demonstrates the expected action for the first items of a match:
    /// dragging the item to its storage in drag mode, or tapping the right storage in question mode.
    /// Only enabled while the tutorial is active, so its Update costs nothing during normal play.
    /// </summary>
    public sealed class TutorialGuide : MonoBehaviour
    {
        private const float StartDelay = 0.6f;
        private const float PressDuration = 0.3f;
        private const float MoveDuration = 1.0f;
        private const float ReleaseDuration = 0.35f;
        private const float RestDuration = 0.35f;
        private const float CycleDuration = PressDuration + MoveDuration + ReleaseDuration + RestDuration;

        [Tooltip("Pointer visual. A hand sprite works best; a soft circle is generated when empty.")]
        [SerializeField] private SpriteRenderer _pointer;
        [SerializeField] private SpriteRenderer _ring;
        [SerializeField] private Vector3 _pointerOffset = new(0.25f, -0.35f, 0f);
        [SerializeField, Min(0.1f)] private float _pointerSize = 0.9f;
        [SerializeField] private int _sortingOrder = 900;

        private GameController _gameController;
        private Item _targetItem;
        private Storage _targetStorage;
        private float _cycleTime;
        private bool _itemHeld;

        private void Awake()
        {
            EnsureVisuals();
            SetVisible(false);
        }

        private void Start()
        {
            _gameController = GameController.Instance;
            if (_gameController == null)
            {
                Debug.LogError("TutorialGuide could not find the GameController.", this);
                enabled = false;
                return;
            }

            _gameController.RegisterTutorialPresenter();
            enabled = _gameController.IsTutorialActive;

            _gameController.OnGameStarted += HandleGameStarted;
            _gameController.OnItemSpawned += HandleItemSpawned;
            _gameController.OnItemPicked += HandleItemPicked;
            _gameController.OnItemReleased += HandleItemReleased;
            _gameController.OnItemSorted += HandleItemSorted;
            _gameController.OnTutorialCompleted += Stop;
            _gameController.OnGameWon += Stop;
            _gameController.OnGameLost += Stop;
            _gameController.OnGameAborted += Stop;
        }

        private void Update()
        {
            if (_targetItem == null || _targetItem.IsResolving)
            {
                Retarget();
                return;
            }

            if (_targetStorage == null)
                return;

            _cycleTime += Time.deltaTime;
            if (_cycleTime < 0f || _itemHeld)
            {
                SetVisible(false);
                return;
            }

            float time = Mathf.Repeat(_cycleTime, CycleDuration);
            if (_gameController.Config.Mode == GameMode.Question)
                AnimateTap(time);
            else
                AnimateDrag(time);
        }

        private void HandleGameStarted(GameConfiguration config)
        {
            if (config.TutorialItemCount > 0)
                Retarget();
            else
                Stop();
        }

        private void HandleItemSpawned(Item item)
        {
            if (_gameController.IsTutorialActive && _targetItem == null)
                Retarget();
        }

        private void HandleItemPicked(Item item)
        {
            _itemHeld = true;
        }

        private void HandleItemReleased(Item item)
        {
            _itemHeld = false;
            _cycleTime = -StartDelay;
        }

        private void HandleItemSorted(ItemSortResult result)
        {
            _itemHeld = false;
            if (_gameController.IsTutorialActive)
                Retarget();
        }

        private void Retarget()
        {
            ClearStorageHighlight();
            if (_gameController == null || !_gameController.IsTutorialActive)
            {
                Stop();
                return;
            }

            _targetItem = _gameController.FocusItem;
            _targetStorage = null;
            _cycleTime = -StartDelay;
            _itemHeld = false;
            enabled = true;
            SetVisible(false);

            if (_targetItem == null)
                return;

            if (_gameController.TryGetStorageFor(_targetItem.ItemType, out _targetStorage))
                _targetStorage.SetHighlighted(true);
            else
                Debug.LogError($"Tutorial found no storage for group {_targetItem.ItemType}.", this);
        }

        private void Stop()
        {
            ClearStorageHighlight();
            _targetItem = null;
            _targetStorage = null;
            SetVisible(false);
            enabled = false;
        }

        private void AnimateDrag(float time)
        {
            Vector3 from = _targetItem.transform.position;
            Vector3 to = _targetStorage.DropPoint;

            if (time < PressDuration)
            {
                float t = time / PressDuration;
                ShowPointer(from, 1f - 0.2f * t, t);
                ShowRing(from, t);
                return;
            }

            time -= PressDuration;
            if (time < MoveDuration)
            {
                float t = Mathf.SmoothStep(0f, 1f, time / MoveDuration);
                ShowPointer(Vector3.Lerp(from, to, t), 0.8f, 1f);
                HideRing();
                return;
            }

            time -= MoveDuration;
            if (time < ReleaseDuration)
            {
                float t = time / ReleaseDuration;
                ShowPointer(to, 0.8f + 0.2f * t, 1f - t);
                ShowRing(to, t);
                return;
            }

            SetVisible(false);
        }

        private void AnimateTap(float time)
        {
            Vector3 target = _targetStorage.DropPoint;
            float tapPhase = Mathf.PingPong(time * 2f / CycleDuration * 2f, 1f);
            ShowPointer(target, 1f - 0.2f * tapPhase, 1f);
            ShowRing(target, Mathf.Repeat(time * 2f / CycleDuration, 1f));
        }

        private void ShowPointer(Vector3 position, float scale, float alpha)
        {
            _pointer.enabled = true;
            position.z = 0f;
            _pointer.transform.position = position + _pointerOffset;
            _pointer.transform.localScale = Vector3.one * (_pointerSize * scale);
            Color color = _pointer.color;
            color.a = alpha;
            _pointer.color = color;
        }

        private void ShowRing(Vector3 position, float progress)
        {
            if (_ring == null)
                return;

            _ring.enabled = true;
            position.z = 0f;
            _ring.transform.position = position;
            _ring.transform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.4f, progress);
            Color color = _ring.color;
            color.a = 1f - progress;
            _ring.color = color;
        }

        private void HideRing()
        {
            if (_ring != null)
                _ring.enabled = false;
        }

        private void SetVisible(bool visible)
        {
            if (_pointer != null)
                _pointer.enabled = visible;

            if (_ring != null)
                _ring.enabled = visible;
        }

        private void ClearStorageHighlight()
        {
            if (_targetStorage != null)
                _targetStorage.SetHighlighted(false);
        }

        private void EnsureVisuals()
        {
            if (_pointer == null)
                _pointer = CreateRenderer("Pointer", ProceduralSprites.Circle, new Color(1f, 1f, 1f, 0.95f));
            else if (_pointer.sprite == null)
                _pointer.sprite = ProceduralSprites.Circle;

            if (_ring == null)
                _ring = CreateRenderer("Ring", ProceduralSprites.Ring, Color.white);
            else if (_ring.sprite == null)
                _ring.sprite = ProceduralSprites.Ring;

            _pointer.sortingOrder = _sortingOrder + 1;
            _ring.sortingOrder = _sortingOrder;
        }

        private SpriteRenderer CreateRenderer(string objectName, Sprite sprite, Color color)
        {
            GameObject child = new(objectName);
            child.transform.SetParent(transform, false);
            SpriteRenderer spriteRenderer = child.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            return spriteRenderer;
        }

        private void OnDestroy()
        {
            if (_gameController == null)
                return;

            _gameController.OnGameStarted -= HandleGameStarted;
            _gameController.OnItemSpawned -= HandleItemSpawned;
            _gameController.OnItemPicked -= HandleItemPicked;
            _gameController.OnItemReleased -= HandleItemReleased;
            _gameController.OnItemSorted -= HandleItemSorted;
            _gameController.OnTutorialCompleted -= Stop;
            _gameController.OnGameWon -= Stop;
            _gameController.OnGameLost -= Stop;
            _gameController.OnGameAborted -= Stop;
        }
    }
}
