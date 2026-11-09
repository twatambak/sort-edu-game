using System;
using DG.Tweening;
using OndeVai.Data;
using OndeVai.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace OndeVai.UI
{
    /// <summary>
    /// Start screen. Everything a child touches is an icon: play, the two game modes and their own avatar.
    /// Adult areas (options and progress) require holding the button as a light parental gate.
    /// </summary>
    public sealed class TitleScreen : UIScreen
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _dragModeButton;
        [SerializeField] private Button _questionModeButton;
        [SerializeField] private Button _swipeModeButton;
        [SerializeField] private Button _fourWaySwipeModeButton;
        [SerializeField] private Button _profileButton;
        [SerializeField] private AvatarBadge _profileBadge;
        [SerializeField] private HoldToConfirmButton _optionsButton;
        [SerializeField] private HoldToConfirmButton _progressButton;
        [SerializeField] private RectTransform _logo;
        [Tooltip("Parent of the play button that receives the idle pulse, so it does not fight the press animation.")]
        [SerializeField] private RectTransform _playPulseTarget;

        private GameController _gameController;
        private Tween _playPulse;
        private Tween _logoFloat;

        public event Action PlayRequested;
        public event Action ProfilesRequested;
        public event Action OptionsRequested;
        public event Action ProgressRequested;

        protected override void Awake()
        {
            base.Awake();
            Bind(_playButton, () => PlayRequested?.Invoke());
            Bind(_dragModeButton, () => SelectMode(GameMode.DragAndDrop));
            Bind(_swipeModeButton, () => SelectMode(GameMode.SwipeCard));
/*             Bind(_questionModeButton, () => SelectMode(GameMode.Question));
            Bind(_fourWaySwipeModeButton, () => SelectMode(GameMode.FourWaySwipe)); */
            Bind(_profileButton, () => ProfilesRequested?.Invoke());
            Bind(_optionsButton, () => OptionsRequested?.Invoke());
            Bind(_progressButton, () => ProgressRequested?.Invoke());
        }

        protected override void OnShown()
        {
            _gameController = GameController.Instance;
            Refresh();
            StartIdleAnimations();
        }

        protected override void OnHidden()
        {
            _playPulse?.Rewind();
            _playPulse?.Kill();
            _logoFloat?.Rewind();
            _logoFloat?.Kill();
        }

        private void Refresh()
        {
            if (_gameController == null)
                return;

            ChildProfileRecord child = _gameController.Data.ActiveChild;
            if (_profileBadge != null)
                _profileBadge.Show(child);
        }

        private void SelectMode(GameMode mode)
        {
            if (_gameController == null)
                return;

            _gameController.Settings.Apply(settings => settings.Mode = mode);
            Refresh();
        }

        private void StartIdleAnimations()
        {
            _playPulse?.Rewind();
            _playPulse?.Kill();
            if (_playPulseTarget != null)
            {
                _playPulse = _playPulseTarget.DOScale(1.08f, 0.6f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .SetUpdate(true)
                    .SetLink(_playPulseTarget.gameObject);
            }

            _logoFloat?.Rewind();
            _logoFloat?.Kill();
            if (_logo != null)
            {
                _logoFloat = _logo.DOAnchorPosY(_logo.anchoredPosition.y + 20f, 1.4f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .SetUpdate(true)
                    .SetLink(_logo.gameObject);
            }
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.AddListener(action);
        }

        private static void Bind(HoldToConfirmButton button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.OnConfirmed.AddListener(action);
        }
    }
}
