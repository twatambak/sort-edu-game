using System;
using System.Collections.Generic;
using DG.Tweening;
using OndeVai.Gameplay;
using OndeVai.Settings;
using UnityEngine;
using UnityEngine.Serialization;
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

        private const float IntroDuration = 0.3f;
        private const float IntroStagger = 0.05f;

        [SerializeField] private Button _playButton;
        [SerializeField] private Button _dragModeButton;
        [SerializeField] private Button _swipeModeButton;
        [SerializeField] private Button _animalsScenarioButton;
        [SerializeField] private Button _lettersNumbersScenarioButton;
        [SerializeField] private Button _toysToolsScenarioButton;
        [FormerlySerializedAs("_FoodScenarioButton")]
        [SerializeField] private Button _foodScenarioButton;
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

        /// <summary>A selectable card: the button, its selection frame and the value it stands for.</summary>
        private readonly struct Choice<T>
        {
            public readonly T Value;
            public readonly Button Button;
            public readonly Image Highlight;

            public Choice(T value, Button button, Image highlight)
            {
                Value = value;
                Button = button;
                Highlight = highlight;
            }

            /// <summary>The layout slot that holds the button and its highlight.</summary>
            public Transform Slot => Button != null ? Button.transform.parent : null;
        }

        private readonly List<SortingScenarioDefinition> _playableScenarios = new();
        private Choice<string>[] _scenarioChoices;
        private Choice<GameMode>[] _modeChoices;
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
            _scenarioChoices = new[]
            {
                new Choice<string>(AnimalsScenarioId, _animalsScenarioButton, _animalsScenarioHighlight),
                new Choice<string>(LettersNumbersScenarioId, _lettersNumbersScenarioButton, _lettersNumbersScenarioHighlight),
                new Choice<string>(ToysToolsScenarioId, _toysToolsScenarioButton, _toysToolsScenarioHighlight),
                new Choice<string>(FoodScenarioId, _foodScenarioButton, _foodScenarioHighlight)
            };

            _modeChoices = new[]
            {
                new Choice<GameMode>(GameMode.DragAndDrop, _dragModeButton, _dragModeHighlight),
                new Choice<GameMode>(GameMode.SwipeTwoDirections, _swipeModeButton, _swipeModeHighlight)
            };

            foreach (Choice<string> choice in _scenarioChoices)
            {
                Choice<string> captured = choice;
                Bind(choice.Button, () => SelectScenario(captured));
            }

            foreach (Choice<GameMode> choice in _modeChoices)
            {
                Choice<GameMode> captured = choice;
                Bind(choice.Button, () => SelectMode(captured));
            }

            Bind(_playButton, () => PlayRequested?.Invoke());
            Bind(_profileButton, () => ProfilesRequested?.Invoke());
            Bind(_optionsButton, () => OptionsRequested?.Invoke());
            Bind(_progressButton, () => ProgressRequested?.Invoke());
        }

        protected override void OnShown()
        {
            _gameController = GameController.Instance;
            Refresh();
            StartIdleAnimations();
            PlayIntro();
        }

        protected override void OnHidden()
        {
            StopTween(ref _playPulse);
            StopTween(ref _logoFloat);
        }

        private void Refresh()
        {
            if (_gameController == null)
                return;

            if (_profileBadge != null)
                _profileBadge.Show(_gameController.Data.ActiveChild);

            GameSettings settings = _gameController.Settings.Current;
            foreach (Choice<GameMode> choice in _modeChoices)
                SetHighlight(choice.Highlight, settings.Mode == choice.Value);

            RefreshScenarios(settings);
        }

        /// <summary>
        /// Highlights the scenario the next match will really use and disables the cards whose
        /// scenario has no playable content.
        /// </summary>
        private void RefreshScenarios(GameSettings settings)
        {
            GameDefinition definition = _gameController.Definition;
            string activeId = settings.ScenarioId;
            _playableScenarios.Clear();
            if (definition != null)
            {
                definition.GetPlayableScenarios(_playableScenarios);
                activeId = definition.GetScenarioOrDefault(settings.ScenarioId).Id;
            }

            foreach (Choice<string> choice in _scenarioChoices)
            {
                SetHighlight(choice.Highlight, choice.Value == activeId);
                if (choice.Button != null)
                    choice.Button.interactable = IsPlayable(choice.Value);
            }
        }

        private bool IsPlayable(string scenarioId)
        {
            foreach (SortingScenarioDefinition scenario in _playableScenarios)
            {
                if (scenario.Id == scenarioId)
                    return true;
            }

            return false;
        }

        private void SelectMode(Choice<GameMode> choice)
        {
            if (_gameController == null)
                return;

            if (!GameModes.IsSupported(choice.Value))
            {
                Debug.LogError($"TitleScreen cannot select unsupported mode {choice.Value}.", this);
                return;
            }

            GameMode mode = choice.Value;
            _gameController.Settings.Apply(settings => settings.Mode = mode);
            Refresh();
            Punch(choice.Slot);
        }

        private void SelectScenario(Choice<string> choice)
        {
            if (_gameController == null || _gameController.Definition == null)
            {
                Debug.LogError("TitleScreen cannot select a scenario because GameDefinition is not assigned.", this);
                return;
            }

            string scenarioId = choice.Value;
            _gameController.Definition.GetPlayableScenarios(_playableScenarios);
            if (!IsPlayable(scenarioId))
            {
                Debug.LogError($"TitleScreen scenario '{scenarioId}' is missing or has no playable content.", this);
                return;
            }

            _gameController.Settings.Apply(settings => settings.ScenarioId = scenarioId);
            Refresh();
            Punch(choice.Slot);
        }

        private void StartIdleAnimations()
        {
            StopTween(ref _playPulse);
            if (_playPulseTarget != null)
            {
                _playPulse = _playPulseTarget.DOScale(1.08f, 0.6f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .SetUpdate(true)
                    .SetLink(_playPulseTarget.gameObject);
            }

            StopTween(ref _logoFloat);
            if (_logo != null)
            {
                _logoFloat = _logo.DOAnchorPosY(_logo.anchoredPosition.y + 20f, 1.4f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .SetUpdate(true)
                    .SetLink(_logo.gameObject);
            }
        }

        /// <summary>Pops the scenario and mode cards in one after the other when the screen opens.</summary>
        private void PlayIntro()
        {
            int index = 0;
            foreach (Choice<string> choice in _scenarioChoices)
                PopIn(choice.Slot, index++);

            foreach (Choice<GameMode> choice in _modeChoices)
                PopIn(choice.Slot, index++);
        }

        private static void PopIn(Transform slot, int index)
        {
            if (slot == null)
                return;

            slot.DOKill();
            slot.localScale = Vector3.one * 0.6f;
            slot.DOScale(1f, IntroDuration)
                .SetDelay(index * IntroStagger)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .SetLink(slot.gameObject);
        }

        private static void Punch(Transform slot)
        {
            if (slot == null)
                return;

            slot.DOKill();
            slot.localScale = Vector3.one;
            slot.DOPunchScale(Vector3.one * 0.12f, 0.3f, 6, 0.6f).SetUpdate(true).SetLink(slot.gameObject);
        }

        private static void StopTween(ref Tween tween)
        {
            if (tween == null)
                return;

            tween.Rewind();
            tween.Kill();
            tween = null;
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
