using System;
using System.Collections.Generic;
using OndeVai.Gameplay;
using OndeVai.Settings;
using OndeVai.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OndeVai.UI
{
    /// <summary>
    /// Adult facing options: audio, vibration, tutorial, simulated profile history, game mode and match rules.
    /// Every change is saved immediately.
    /// </summary>
    public sealed class OptionsScreen : UIScreen
    {
        private static readonly GameMode[] AvailableModes =
        {
            GameMode.DragAndDrop,
            GameMode.SwipeTwoDirections
        };

        private static readonly int[] MatchTimes = { 15, 30, 45, 60, 90, 120 };
        private static readonly int[] ErrorLimits = { 1, 2, 3, 5, 10 };
        private static readonly int[] ItemsOnScreen = { 2, 3, 4, 5, 6 };
        private static readonly int[] TutorialItems = { 1, 2, 3, 5 };

        [Header("Audio and feedback")]
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Toggle _vibrationToggle;

        [Header("Tutorial")]
        [SerializeField] private Toggle _tutorialToggle;
        [SerializeField] private OptionSelector _tutorialItemsSelector;

        [Header("Simulated profile history")]
        [SerializeField] private Toggle _simulatedProfilesToggle;
        [Tooltip("Shows the simulated profile history setting in Options.")]
        [SerializeField] private bool _showSimulatedHistoryOption = true;

        [Header("Match")]
        [SerializeField] private OptionSelector _modeSelector;
        [SerializeField] private OptionSelector _scenarioSelector;
        [SerializeField] private OptionSelector _matchTimeSelector;
        [SerializeField] private OptionSelector _errorLimitSelector;
        [SerializeField] private OptionSelector _itemsOnScreenSelector;

        [Header("Navigation")]
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _privacyButton;
        [SerializeField] private HoldToConfirmButton _resetButton;

        [Header("Active profile data")]
        [SerializeField] private TMP_Text _profileDataStatus;
        [SerializeField] private HoldToConfirmButton _clearProgressButton;
        [SerializeField] private HoldToConfirmButton _deleteProfileButton;

        private readonly List<SortingScenarioDefinition> _scenarios = new();
        private readonly List<string> _labels = new();
        private GameSettingsService _settings;
        private bool _isRefreshing;

        public event Action Closed;
        public event Action PrivacyRequested;

        protected override void Awake()
        {
            base.Awake();
            if (_backButton != null)
                _backButton.onClick.AddListener(() => Closed?.Invoke());

            if (_privacyButton != null)
                _privacyButton.onClick.AddListener(() => PrivacyRequested?.Invoke());

            if (_resetButton != null)
                _resetButton.OnConfirmed.AddListener(ResetDefaults);

            if (_clearProgressButton != null)
                _clearProgressButton.OnConfirmed.AddListener(ClearActiveProfileProgress);

            if (_deleteProfileButton != null)
                _deleteProfileButton.OnConfirmed.AddListener(DeleteActiveProfile);

            BindSlider(_musicSlider, value => Apply(s => s.MusicVolume = value, false));
            BindSlider(_sfxSlider, value => Apply(s => s.SfxVolume = value, false));
            BindToggle(_vibrationToggle, value => Apply(s => s.VibrationEnabled = value));
            BindToggle(_tutorialToggle, value => Apply(s => s.TutorialEnabled = value));
            EnsureSimulatedProfilesToggle();
            BindToggle(_simulatedProfilesToggle, value => Apply(s => s.SimulateNewProfileHistory = value));
            BindSelector(_tutorialItemsSelector, index => Apply(s => s.TutorialItemCount = TutorialItems[index]));
            BindSelector(_modeSelector, HandleModeChanged);
            BindSelector(_scenarioSelector, index => Apply(s => s.ScenarioId = _scenarios[index].Id));
            BindSelector(_matchTimeSelector, index => Apply(s => s.MatchTimeSeconds = MatchTimes[index]));
            BindSelector(_errorLimitSelector, index => Apply(s => s.ErrorLimit = ErrorLimits[index]));
            BindSelector(_itemsOnScreenSelector, index => Apply(s => s.ItemsOnScreen = ItemsOnScreen[index]));
        }

        protected override void OnShown()
        {
            GameController controller = GameController.Instance;
            if (controller == null)
            {
                Debug.LogError("OptionsScreen could not find the GameController.", this);
                return;
            }

            _settings = controller.Settings;
            Refresh(_settings.Current, controller.Definition);
            RefreshProfileActions(controller.Data);
        }

        private void Refresh(GameSettings settings, GameDefinition definition)
        {
            _isRefreshing = true;

            if (_musicSlider != null)
                _musicSlider.SetValueWithoutNotify(settings.MusicVolume);

            if (_sfxSlider != null)
                _sfxSlider.SetValueWithoutNotify(settings.SfxVolume);

            if (_vibrationToggle != null)
                _vibrationToggle.SetIsOnWithoutNotify(settings.VibrationEnabled);

            if (_tutorialToggle != null)
                _tutorialToggle.SetIsOnWithoutNotify(settings.TutorialEnabled);

            if (_simulatedProfilesToggle != null)
            {
                _simulatedProfilesToggle.SetIsOnWithoutNotify(settings.SimulateNewProfileHistory);
                Transform row = _simulatedProfilesToggle.transform.parent != null
                    ? _simulatedProfilesToggle.transform.parent.parent
                    : null;
                if (row != null)
                    row.gameObject.SetActive(_showSimulatedHistoryOption);
            }

            SetNumberOptions(_tutorialItemsSelector, TutorialItems, settings.TutorialItemCount, value => value == 1 ? "1 objeto" : $"{value} objetos");
            SetNumberOptions(_matchTimeSelector, MatchTimes, settings.MatchTimeSeconds, value => $"{value} s");
            SetNumberOptions(_errorLimitSelector, ErrorLimits, settings.ErrorLimit, value => value.ToString());
            SetNumberOptions(_itemsOnScreenSelector, ItemsOnScreen, settings.ItemsOnScreen, value => value.ToString());

            if (definition != null)
                definition.GetPlayableScenarios(_scenarios);
            else
            {
                _scenarios.Clear();
                _scenarios.Add(SortingScenarioDefinition.Default);
            }

            if (_modeSelector != null)
            {
                _labels.Clear();
                _labels.Add("Arrastar e soltar");
                _labels.Add("Deslizar (2 direções)");
                int selectedMode = settings.Mode == GameMode.SwipeTwoDirections ? 1 : 0;
                _modeSelector.SetOptions(_labels, selectedMode);
            }

            if (_scenarioSelector != null)
            {
                SortingScenarioDefinition current = definition != null
                    ? definition.GetScenarioOrDefault(settings.ScenarioId)
                    : SortingScenarioDefinition.Default;

                _labels.Clear();
                int selected = 0;
                for (int i = 0; i < _scenarios.Count; i++)
                {
                    _labels.Add(_scenarios[i].DisplayName);
                    if (current != null && _scenarios[i].Id == current.Id)
                        selected = i;
                }

                _scenarioSelector.SetOptions(_labels, selected);
            }

            _isRefreshing = false;
        }

        private void EnsureSimulatedProfilesToggle()
        {
            if (_simulatedProfilesToggle != null)
                return;

            if (_tutorialToggle == null || _tutorialToggle.transform.parent == null ||
                _tutorialToggle.transform.parent.parent == null)
            {
                Debug.LogError("OptionsScreen could not create the simulated profile history option because the tutorial toggle is missing.", this);
                return;
            }

            Transform tutorialRow = _tutorialToggle.transform.parent.parent;
            GameObject row = Instantiate(tutorialRow.gameObject, tutorialRow.parent);
            row.name = "SimulatedProfiles";
            row.transform.SetSiblingIndex(tutorialRow.GetSiblingIndex() + 1);

            TMP_Text label = row.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = "Simular histórico em novos perfis";

            _simulatedProfilesToggle = row.GetComponentInChildren<Toggle>(true);
            if (_simulatedProfilesToggle == null)
            {
                Debug.LogError("OptionsScreen could not create the simulated profile history option because the tutorial row has no toggle.", this);
                row.SetActive(false);
                return;
            }

            _simulatedProfilesToggle.name = "SimulatedProfilesToggle";
            row.SetActive(_showSimulatedHistoryOption);
        }

        private void HandleModeChanged(int index)
        {
            if (_isRefreshing || _settings == null)
                return;

            if (index < 0 || index >= AvailableModes.Length)
            {
                Debug.LogError($"OptionsScreen received unsupported mode index {index}.", this);
                return;
            }

            GameMode mode = AvailableModes[index];
            _settings.Apply(settings => settings.Mode = mode);
            Refresh(_settings.Current, GameController.Instance != null ? GameController.Instance.Definition : null);
        }

        private void SetNumberOptions(OptionSelector selector, int[] values, int current, Func<int, string> format)
        {
            if (selector == null)
                return;

            _labels.Clear();
            int selected = 0;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < values.Length; i++)
            {
                _labels.Add(format(values[i]));
                int distance = Mathf.Abs(values[i] - current);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    selected = i;
                }
            }

            selector.SetOptions(_labels, selected);
        }

        private void ResetDefaults()
        {
            if (_settings == null)
                return;

            _settings.ResetToDefaults();
            Refresh(_settings.Current, GameController.Instance != null ? GameController.Instance.Definition : null);
        }

        private void ClearActiveProfileProgress()
        {
            GameDataService data = GameController.Instance != null ? GameController.Instance.Data : null;
            ChildProfileRecord profile = data != null ? data.ActiveChild : null;
            if (profile == null || !data.DeleteHistory(profile))
            {
                SetProfileDataStatus(profile == null
                    ? "Selecione um perfil para apagar o progresso."
                    : "Não foi possível apagar o progresso. Tente novamente.");
                return;
            }

            SetProfileDataStatus("Progresso do perfil apagado.");
        }

        private void DeleteActiveProfile()
        {
            GameDataService data = GameController.Instance != null ? GameController.Instance.Data : null;
            ChildProfileRecord profile = data != null ? data.ActiveChild : null;
            if (profile == null || !data.DeleteProfile(profile))
            {
                RefreshProfileActions(data);
                SetProfileDataStatus(profile == null
                    ? "Selecione um perfil para excluir."
                    : "Não foi possível excluir o perfil. Tente novamente.");
                return;
            }

            RefreshProfileActions(data);
            SetProfileDataStatus("Perfil excluído. A próxima partida será sem cadastro.");
        }

        private void RefreshProfileActions(GameDataService data)
        {
            bool canManageProfile = data != null && data.IsAvailable && data.ActiveChild != null;
            if (_clearProgressButton != null)
                _clearProgressButton.gameObject.SetActive(canManageProfile);

            if (_deleteProfileButton != null)
                _deleteProfileButton.gameObject.SetActive(canManageProfile);

            if (data == null || !data.IsAvailable)
                SetProfileDataStatus("Os dados deste aparelho não estão disponíveis.");
            else if (!canManageProfile)
                SetProfileDataStatus("Selecione uma criança para gerenciar os dados do perfil.");
            else
                SetProfileDataStatus("As ações abaixo afetam apenas o perfil ativo.");
        }

        private void SetProfileDataStatus(string message)
        {
            if (_profileDataStatus != null)
                _profileDataStatus.text = message;
        }

        protected override void OnHidden()
        {
            _settings?.Flush();
        }

        private void Apply(Action<GameSettings> modify, bool persist = true)
        {
            if (_isRefreshing || _settings == null)
                return;

            _settings.Apply(modify, persist);
        }

        private static void BindSlider(Slider slider, Action<float> onChanged)
        {
            if (slider != null)
                slider.onValueChanged.AddListener(value => onChanged(value));
        }

        private static void BindToggle(Toggle toggle, Action<bool> onChanged)
        {
            if (toggle != null)
                toggle.onValueChanged.AddListener(value => onChanged(value));
        }

        private static void BindSelector(OptionSelector selector, Action<int> onChanged)
        {
            if (selector != null)
                selector.Changed += onChanged;
        }
    }
}
