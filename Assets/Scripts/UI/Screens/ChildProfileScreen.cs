using System;
using System.Collections.Generic;
using DG.Tweening;
using OndeVai.Data;
using OndeVai.Gameplay;
using OndeVai.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OndeVai.UI
{
    /// <summary>
    /// Lists the children registered on the device and lets an adult add, edit, select or remove them.
    /// </summary>
    public sealed class ChildProfileScreen : UIScreen
    {
        private const int MinSelectableAge = 2;
        private const int MaxSelectableAge = 8;
        private const float RowEntranceDuration = 0.25f;
        private const float RowEntranceStagger = 0.05f;

        [Header("List")]
        [SerializeField] private RectTransform _listContent;
        [Tooltip("Inactive row cloned for each registered child.")]
        [SerializeField] private ChildProfileRow _rowTemplate;
        [SerializeField] private GameObject _emptyListMessage;
        [SerializeField] private Button _addButton;
        [SerializeField] private Button _guestButton;
        [SerializeField] private Button _backButton;

        [Header("Form")]
        [SerializeField] private GameObject _formRoot;
        [SerializeField] private TMP_Text _formTitle;
        [SerializeField] private TMP_InputField _nameInput;
        [SerializeField] private OptionSelector _ageSelector;
        [SerializeField] private Toggle _hideNameToggle;
        [SerializeField] private RectTransform _avatarOptionsParent;
        [SerializeField] private Button _avatarOptionTemplate;
        [SerializeField] private AvatarBadge _formPreview;
        [SerializeField] private TMP_Text _formError;
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private HoldToConfirmButton _deleteButton;

        private readonly List<ChildProfileRow> _rows = new();
        private readonly List<Button> _avatarOptions = new();
        private readonly List<string> _ageLabels = new();
        private ChildProfileRecord _editing;
        private int _selectedAvatar;

        /// <summary>Raised when the screen should close.</summary>
        public event Action Closed;

        private GameDataService Data => GameController.Instance != null ? GameController.Instance.Data : null;

        protected override void Awake()
        {
            base.Awake();
            if (_rowTemplate != null)
                _rowTemplate.gameObject.SetActive(false);

            for (int age = MinSelectableAge; age <= MaxSelectableAge; age++)
                _ageLabels.Add(age == MaxSelectableAge ? $"{age}+ anos" : $"{age} anos");

            Bind(_addButton, () => OpenForm(null));
            Bind(_guestButton, PlayAsGuest);
            Bind(_backButton, () => Closed?.Invoke());
            Bind(_saveButton, SaveForm);
            Bind(_cancelButton, CloseForm);

            if (_deleteButton != null)
                _deleteButton.OnConfirmed.AddListener(DeleteEditing);

            if (_nameInput != null)
            {
                _nameInput.characterLimit = ChildProfileRecord.MaxNameLength;
                _nameInput.onValueChanged.AddListener(_ => RefreshPreview());
            }

            EnsureHideNameToggle();
            BuildAvatarOptions();
        }

        protected override void OnShown()
        {
            CloseForm();
            RefreshList();
        }

        private void RefreshList()
        {
            GameDataService data = Data;
            List<ChildProfileRecord> profiles = data != null ? data.GetProfiles() : new List<ChildProfileRecord>();
            int activeId = data?.ActiveChild?.Id ?? 0;

            EnsureRowCount(profiles.Count);
            for (int i = 0; i < _rows.Count; i++)
            {
                bool used = i < profiles.Count;
                _rows[i].gameObject.SetActive(used);
                if (!used)
                    continue;

                ChildProfileRecord profile = profiles[i];
                _rows[i].Bind(profile, data.GetQuickStats(profile.Id), profile.Id == activeId, SelectProfile, OpenForm);
                PlayRowEntrance(_rows[i].transform, i);
            }

            if (_emptyListMessage != null)
                _emptyListMessage.SetActive(profiles.Count == 0);
        }

        private static void PlayRowEntrance(Transform row, int index)
        {
            row.DOKill();
            row.localScale = new Vector3(1f, 0f, 1f);
            row.DOScaleY(1f, RowEntranceDuration)
                .SetDelay(index * RowEntranceStagger)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .SetLink(row.gameObject);
        }

        private void EnsureRowCount(int count)
        {
            if (_rowTemplate == null || _listContent == null)
                return;

            while (_rows.Count < count)
            {
                ChildProfileRow row = Instantiate(_rowTemplate, _listContent);
                row.name = $"Row {_rows.Count}";
                _rows.Add(row);
            }
        }

        private void SelectProfile(ChildProfileRecord profile)
        {
            Data?.SetActiveChild(profile);
            Closed?.Invoke();
        }

        private void PlayAsGuest()
        {
            Data?.SetActiveChild(null);
            Closed?.Invoke();
        }

        private void OpenForm(ChildProfileRecord profile)
        {
            _editing = profile;
            if (_formRoot != null)
                _formRoot.SetActive(true);

            if (_formTitle != null)
                _formTitle.text = profile == null ? "Nova criança" : "Editar criança";

            if (_nameInput != null)
                _nameInput.SetTextWithoutNotify(profile?.Name ?? string.Empty);

            int age = profile?.Age ?? 4;
            if (_ageSelector != null)
                _ageSelector.SetOptions(_ageLabels, Mathf.Clamp(age, MinSelectableAge, MaxSelectableAge) - MinSelectableAge);

            if (_hideNameToggle != null)
                _hideNameToggle.SetIsOnWithoutNotify(profile != null && profile.HideName);

            _selectedAvatar = profile?.AvatarIndex ?? UnityEngine.Random.Range(0, AvatarPalette.Count);
            if (_deleteButton != null)
                _deleteButton.gameObject.SetActive(profile != null);

            SetError(string.Empty);
            RefreshAvatarOptions();
            RefreshPreview();
        }

        /// <summary>Back closes the add/edit form before closing the screen.</summary>
        public override bool HandleBack()
        {
            if (_formRoot == null || !_formRoot.activeSelf)
                return false;

            CloseForm();
            return true;
        }

        private void CloseForm()
        {
            _editing = null;
            if (_formRoot != null)
                _formRoot.SetActive(false);
        }

        private void SaveForm()
        {
            GameDataService data = Data;
            if (data == null || !data.IsAvailable)
            {
                SetError("Não foi possível acessar os dados deste aparelho.");
                return;
            }

            string childName = GameDataService.SanitizeName(_nameInput != null ? _nameInput.text : string.Empty);
            if (string.IsNullOrEmpty(childName))
            {
                SetError("Digite o nome da criança.");
                return;
            }

            ChildProfileRecord profile = _editing ?? new ChildProfileRecord();
            profile.Name = childName;
            profile.Age = (_ageSelector != null ? _ageSelector.Index : 2) + MinSelectableAge;
            profile.AvatarIndex = _selectedAvatar;
            profile.HideName = _hideNameToggle != null && _hideNameToggle.isOn;

            bool isNew = _editing == null;
            GameController controller = GameController.Instance;
            bool simulateHistory = isNew && controller != null && controller.Settings.Current.SimulateNewProfileHistory;
            bool saved;
            if (simulateHistory)
            {
                GameSettings settings = controller.Settings.Current;
                SortingScenarioDefinition scenario = controller.Definition != null
                    ? controller.Definition.GetScenarioOrDefault(settings.ScenarioId)
                    : SortingScenarioDefinition.Default;
                GameConfiguration configuration = GameConfiguration.FromSettings(settings, scenario);
                List<SimulatedMatchData> matches = ProfileHistorySimulator.Generate(
                    controller.Definition,
                    configuration,
                    profile.Age,
                    DateTime.UtcNow,
                    new System.Random(Guid.NewGuid().GetHashCode()));
                saved = data.SaveProfileWithSimulatedHistory(profile, matches);
            }
            else
            {
                saved = data.SaveProfile(profile);
            }

            if (!saved)
            {
                SetError("Não foi possível salvar. Tente novamente.");
                return;
            }

            CloseForm();
            if (isNew)
                SelectProfile(profile);
            else
                RefreshList();
        }

        private void DeleteEditing()
        {
            if (_editing == null)
                return;

            GameDataService data = Data;
            if (data == null || !data.IsAvailable)
            {
                SetError("Não foi possível acessar os dados deste aparelho.");
                return;
            }

            if (!data.DeleteProfile(_editing))
            {
                SetError("Não foi possível excluir o perfil. Tente novamente.");
                return;
            }

            CloseForm();
            RefreshList();
        }

        private void BuildAvatarOptions()
        {
            if (_avatarOptionTemplate == null || _avatarOptionsParent == null)
                return;

            _avatarOptionTemplate.gameObject.SetActive(false);
            for (int i = 0; i < AvatarPalette.Count; i++)
            {
                int index = i;
                Button option = Instantiate(_avatarOptionTemplate, _avatarOptionsParent);
                option.name = $"Avatar {i}";
                option.gameObject.SetActive(true);
                if (option.targetGraphic != null)
                    option.targetGraphic.color = AvatarPalette.Get(i);

                option.onClick.AddListener(() =>
                {
                    _selectedAvatar = index;
                    RefreshAvatarOptions();
                    RefreshPreview();
                });
                _avatarOptions.Add(option);
            }
        }

        private void EnsureHideNameToggle()
        {
            if (_hideNameToggle != null)
                return;

            if (_formRoot == null || _ageSelector == null || _formTitle == null)
                return;

            Transform panel = _ageSelector.transform.parent;
            GameObject row = new("HideNameOption", typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(panel, false);
            row.transform.SetSiblingIndex(_ageSelector.transform.GetSiblingIndex() + 1);
            RectTransform rowRect = (RectTransform)row.transform;
            LayoutElement rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.preferredHeight = 90f;
            rowLayout.flexibleWidth = 1f;
            HorizontalLayoutGroup rowLayoutGroup = row.GetComponent<HorizontalLayoutGroup>();
            rowLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
            rowLayoutGroup.spacing = 20f;
            rowLayoutGroup.childControlWidth = true;
            rowLayoutGroup.childControlHeight = true;
            rowLayoutGroup.childForceExpandWidth = false;
            rowLayoutGroup.childForceExpandHeight = false;
            rowRect.localScale = Vector3.one;

            GameObject labelObject = new("Label", typeof(RectTransform));
            labelObject.transform.SetParent(row.transform, false);
            TMP_Text label = labelObject.AddComponent<TextMeshProUGUI>();
            label.font = _formTitle.font;
            label.fontSize = 34f;
            label.color = _formTitle.color;
            label.text = "Manter nome em sigilo";
            label.alignment = TextAlignmentOptions.MidlineLeft;
            LayoutElement labelLayout = labelObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;

            GameObject toggleObject = new("Toggle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle));
            toggleObject.transform.SetParent(row.transform, false);
            RectTransform toggleRect = (RectTransform)toggleObject.transform;
            toggleRect.sizeDelta = new Vector2(76f, 76f);
            LayoutElement toggleLayout = toggleObject.AddComponent<LayoutElement>();
            toggleLayout.preferredWidth = 76f;
            toggleLayout.preferredHeight = 76f;
            Image background = toggleObject.GetComponent<Image>();
            background.color = Color.white;

            GameObject checkObject = new("Check", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            checkObject.transform.SetParent(toggleObject.transform, false);
            RectTransform checkRect = (RectTransform)checkObject.transform;
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.offsetMin = new Vector2(18f, 18f);
            checkRect.offsetMax = new Vector2(-18f, -18f);
            Image check = checkObject.GetComponent<Image>();
            check.color = new Color(0.25f, 0.65f, 0.25f, 1f);
            check.raycastTarget = false;

            _hideNameToggle = toggleObject.GetComponent<Toggle>();
            _hideNameToggle.targetGraphic = background;
            _hideNameToggle.graphic = check;
            _hideNameToggle.isOn = false;
        }

        private void RefreshAvatarOptions()
        {
            for (int i = 0; i < _avatarOptions.Count; i++)
                _avatarOptions[i].transform.localScale = Vector3.one * (i == _selectedAvatar ? 1.25f : 1f);
        }

        private void RefreshPreview()
        {
            if (_formPreview != null)
                _formPreview.Show(_nameInput != null ? _nameInput.text : string.Empty, _selectedAvatar);
        }

        private void SetError(string message)
        {
            if (_formError == null)
                return;

            _formError.text = message;
            _formError.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.AddListener(action);
        }
    }
}
