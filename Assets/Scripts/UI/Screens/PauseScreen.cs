using System;
using UnityEngine;
using UnityEngine.UI;

namespace OndeVai.UI
{
    /// <summary>
    /// Icon only pause overlay: resume, restart and back to the title screen.
    /// Opens automatically whenever the match is paused.
    /// </summary>
    public sealed class PauseScreen : UIScreen
    {
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _homeButton;

        public event Action HomeRequested;

        protected override void Awake()
        {
            base.Awake();
            Bind(_resumeButton, () => GameController.Instance?.SetPaused(false));
            Bind(_restartButton, Restart);
            Bind(_homeButton, () => HomeRequested?.Invoke());
        }

        private static void Restart()
        {
            GameController controller = GameController.Instance;
            if (controller == null)
                return;

            controller.SetPaused(false);
            controller.ResetGame();
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.AddListener(action);
        }
    }
}
