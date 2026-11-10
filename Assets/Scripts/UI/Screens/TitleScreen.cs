using System;
using System.Collections.Generic;
using DG.Tweening;
using OndeVai.Data;
using OndeVai.Gameplay;
using OndeVai.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace OndeVai.UI
{
    /// <summary>
    /// Start screen for choosing a sorting scenario and game mode.
    /// Adult areas (options and progress) require holding the button as a light parental gate.
    /// </summary>
    public sealed class TitleScreen : UIScreen
    {
        private const string AnimalsScenarioId = "farm-jungle-animals";
        private const string LettersNumbersScenarioId = "letters-numbers";
        private const string ToysToolsScenarioId = "toys-tools";
        private const string FoodScenarioId = "foods";


        [SerializeField] private Button _playButton;
        [SerializeField] private Button _dragModeButton;
        [SerializeField] private Button _swipeModeButton;
        [SerializeField] private Button _animalsScenarioButton;
        [SerializeField] private Button _lettersNumbersScenarioButton;
        [SerializeField] private Button _toysToolsScenarioButton;
        [SerializeField] private Button _FoodScenarioButton;
        [SerializeField] private Image _dragModeHighlight;
        [SerializeField] private Image _swipeModeHighlight;
        [SerializeField] private Image _animalsScenarioHighlight;
        [SerializeField] private Image _lettersNumbersScenarioHighlight;
        [SerializeField] private Image _toysToolsScenarioHighlight;
        [SerializeField] private Image _foodScenarioHighlight;
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
        private readonly List<SortingScenarioDefinition> _playableScenarios = new();

        public event Action PlayRequested;
        public event Action ProfilesRequested;
        public event Action OptionsRequested;
        public event Action ProgressRequested;

        protected override void Awake()
        {
            base.Awake();
            Bind(_playButton, () => PlayRequested?.Invoke());
            Bind(_dragModeButton, () => SelectMode(GameMode.DragAndDrop));
            Bind(_swipeModeButton, () => SelectMode(GameMode.SwipeTwoDirections));
            Bind(_animalsScenarioButton, () => SelectScenario(AnimalsScenarioId));
            Bind(_lettersNumbersScenarioButton, () => SelectScenario(LettersNumbersScenarioId));
            Bind(_toysToolsScenarioButton, () => SelectScenario(ToysToolsScenarioId));
            Bind(_FoodScenarioButton, () => SelectScenario(FoodScenarioId));
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

            GameSettings settings = _gameController.Settings.Current;
            SetHighlight(_dragModeHighlight, settings.Mode == GameMode.DragAndDrop);
            SetHighlight(_swipeModeHighlight, settings.Mode == GameMode.SwipeTwoDirections);
            SetHighlight(_animalsScenarioHighlight, settings.ScenarioId == AnimalsScenarioId);
            SetHighlight(_lettersNumbersScenarioHighlight, settings.ScenarioId == LettersNumbersScenarioId);
            SetHighlight(_toysToolsScenarioHighlight, settings.ScenarioId == ToysToolsScenarioId);
            SetHighlight(_foodScenarioHighlight, settings.ScenarioId == FoodScenarioId);

        }

        private void SelectMode(GameMode mode)
        {
            if (_gameController == null)
                return;

            if (mode != GameMode.DragAndDrop && mode != GameMode.SwipeTwoDirections)
            {
                Debug.LogError($"TitleScreen cannot select unsupported mode {mode}.", this);
                return;
            }

            _gameController.Settings.Apply(settings => settings.Mode = mode);
            Refresh();
        }

        private void SelectScenario(string scenarioId)
        {
            if (_gameController == null || _gameController.Definition == null)
            {
                Debug.LogError("TitleScreen cannot select a scenario because GameDefinition is not assigned.", this);
                return;
            }

            bool isPlayable = false;
            _gameController.Definition.GetPlayableScenarios(_playableScenarios);
            foreach (SortingScenarioDefinition scenario in _playableScenarios)
            {
                if (scenario.Id != scenarioId)
                    continue;

                isPlayable = true;
                break;
            }

            if (!isPlayable)
            {
                Debug.LogError($"TitleScreen scenario '{scenarioId}' is missing or has no playable content.", this);
                return;
            }

            _gameController.Settings.Apply(settings =>
            {
                settings.ScenarioId = scenarioId;
            });
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

        private static void SetHighlight(Image highlight, bool selected)
        {
            if (highlight != null && highlight.gameObject.activeSelf != selected)
                highlight.gameObject.SetActive(selected);
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
