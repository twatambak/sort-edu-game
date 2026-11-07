using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OndeVai.UI
{
    /// <summary>
    /// Drives navigation between the app screens: disclaimer, title, profiles, options, progress,
    /// gameplay, pause and results.
    /// </summary>
    public sealed class GameFlowController : MonoBehaviour
    {
        [SerializeField] private DisclaimerScreen _disclaimerScreen;
        [SerializeField] private TitleScreen _titleScreen;
        [SerializeField] private ChildProfileScreen _profileScreen;
        [SerializeField] private OptionsScreen _optionsScreen;
        [SerializeField] private ProgressScreen _progressScreen;
        [SerializeField] private PauseScreen _pauseScreen;
        [SerializeField] private GameStatusScreen _statusScreen;

        [Tooltip("In-game HUD elements owned by the flow (e.g. the pause button). Shown only during matches.")]
        [SerializeField] private GameObject _gameplayHud;
        [SerializeField] private Button _pauseButton;

        private GameController _gameController;
        private bool _disclaimerFromOptions;

        // Android delivers the system back button (and back gesture) as the Escape key.
        private InputAction _backAction;

        private void OnEnable()
        {
            _backAction ??= new InputAction("Back", InputActionType.Button, "<Keyboard>/escape");
            _backAction.performed += HandleBackPerformed;
            _backAction.Enable();
        }

        private void OnDisable()
        {
            if (_backAction == null)
                return;

            _backAction.performed -= HandleBackPerformed;
            _backAction.Disable();
        }

        private void HandleBackPerformed(InputAction.CallbackContext context)
        {
            HandleBack();
        }

        /// <summary>
        /// Back navigation: closes the open dialog or screen, pauses or resumes a match, and leaves the
        /// results screen. Does nothing on the title and disclaimer screens so a child cannot close the app by accident.
        /// </summary>
        public void HandleBack()
        {
            if (_gameController == null || IsVisible(_disclaimerScreen))
                return;

            if (IsVisible(_pauseScreen))
            {
                _gameController.SetPaused(false);
                return;
            }

            if (_gameController.IsGameStarted && !_gameController.IsGameOver)
            {
                _gameController.SetPaused(true);
                return;
            }

            if (_gameController.IsGameStarted && _gameController.IsGameOver)
            {
                ReturnToTitle();
                return;
            }

            UIScreen current = CurrentSubScreen();
            if (current != null && !current.HandleBack())
                ShowTitle();
        }

        private UIScreen CurrentSubScreen()
        {
            if (IsVisible(_profileScreen))
                return _profileScreen;

            if (IsVisible(_optionsScreen))
                return _optionsScreen;

            return IsVisible(_progressScreen) ? _progressScreen : null;
        }

        private static bool IsVisible(UIScreen screen)
        {
            return screen != null && screen.IsVisible;
        }

        private void Start()
        {
            _gameController = GameController.Instance;
            if (_gameController == null)
            {
                Debug.LogError("GameFlowController could not find the GameController.", this);
                return;
            }

            Subscribe();
            HideAll();

            if (_gameController.Settings.HasAcceptedDisclaimer)
                ShowTitle();
            else
                Show(_disclaimerScreen);
        }

        private void Subscribe()
        {
            _gameController.OnPauseChanged += HandlePauseChanged;
            _gameController.OnGameStarted += HandleGameStarted;
            _gameController.OnGameEnded += HandleGameEnded;

            if (_disclaimerScreen != null)
                _disclaimerScreen.Accepted += HandleDisclaimerAccepted;

            if (_titleScreen != null)
            {
                _titleScreen.PlayRequested += StartMatch;
                _titleScreen.ProfilesRequested += () => Show(_profileScreen);
                _titleScreen.OptionsRequested += () => Show(_optionsScreen);
                _titleScreen.ProgressRequested += () => Show(_progressScreen);
            }

            if (_profileScreen != null)
                _profileScreen.Closed += ShowTitle;

            if (_optionsScreen != null)
            {
                _optionsScreen.Closed += ShowTitle;
                _optionsScreen.PrivacyRequested += ShowDisclaimerFromOptions;
            }

            if (_progressScreen != null)
                _progressScreen.Closed += ShowTitle;

            if (_pauseScreen != null)
                _pauseScreen.HomeRequested += ReturnToTitle;

            if (_statusScreen != null)
                _statusScreen.HomeRequested += ReturnToTitle;

            if (_pauseButton != null)
                _pauseButton.onClick.AddListener(() => _gameController.SetPaused(true));
        }

        private void HandleDisclaimerAccepted()
        {
            _gameController.Settings.AcceptDisclaimer();
            if (_disclaimerFromOptions)
            {
                _disclaimerFromOptions = false;
                Show(_optionsScreen);
                return;
            }

            ShowTitle();
        }

        private void ShowDisclaimerFromOptions()
        {
            _disclaimerFromOptions = true;
            Show(_disclaimerScreen);
        }

        private void StartMatch()
        {
            HideAll();
            SetHudVisible(true);
            _gameController.BeginGameFromSettings();
        }

        private void ReturnToTitle()
        {
            _gameController.AbortGame();
            if (_statusScreen != null)
                _statusScreen.Hide();

            ShowTitle();
        }

        private void HandleGameStarted(GameConfiguration config)
        {
            SetHudVisible(true);
        }

        private void HandleGameEnded(GameSessionRecord record)
        {
            SetHudVisible(false);
        }

        private void HandlePauseChanged(bool paused)
        {
            if (_pauseScreen == null)
                return;

            if (paused)
                _pauseScreen.Show();
            else
                _pauseScreen.Hide();
        }

        private void ShowTitle()
        {
            Show(_titleScreen);
        }

        private void Show(UIScreen screen)
        {
            if (screen == null)
            {
                Debug.LogError("GameFlowController tried to open a screen that is not assigned.", this);
                return;
            }

            HideAll();
            SetHudVisible(false);
            screen.Show();
        }

        private void HideAll()
        {
            HideInstant(_disclaimerScreen);
            HideInstant(_titleScreen);
            HideInstant(_profileScreen);
            HideInstant(_optionsScreen);
            HideInstant(_progressScreen);
            HideInstant(_pauseScreen);
        }

        private void SetHudVisible(bool visible)
        {
            if (_gameplayHud != null)
                _gameplayHud.SetActive(visible);
        }

        private static void HideInstant(UIScreen screen)
        {
            if (screen != null && screen.IsVisible)
                screen.Hide();
            else if (screen != null)
                screen.HideInstant();
        }

        private void OnDestroy()
        {
            _backAction?.Dispose();
            _backAction = null;
            if (_gameController == null)
                return;

            _gameController.OnPauseChanged -= HandlePauseChanged;
            _gameController.OnGameStarted -= HandleGameStarted;
            _gameController.OnGameEnded -= HandleGameEnded;
        }
    }
}
