using TMPro;
using UnityEngine;
using OndeVai.Gameplay;

namespace OndeVai.UI
{
    /// <summary>Shows the category assigned to each swipe direction during swipe matches.</summary>
    public sealed class SwipeDirectionHints : MonoBehaviour
    {
        [SerializeField] private GameObject _leftRoot;
        [SerializeField] private GameObject _rightRoot;
        [SerializeField] private GameObject _upRoot;
        [SerializeField] private GameObject _downRoot;
        [SerializeField] private TMP_Text _leftLabel;
        [SerializeField] private TMP_Text _rightLabel;
        [SerializeField] private TMP_Text _upLabel;
        [SerializeField] private TMP_Text _downLabel;

        private GameController _controller;

        private void OnEnable()
        {
            _controller = GameController.Instance;
            if (_controller == null)
            {
                Debug.LogError("SwipeDirectionHints could not find the GameController.", this);
                return;
            }

            _controller.OnGameStarted += HandleGameStarted;
            if (_controller.Config != null)
                Refresh(_controller.Config);
        }

        private void OnDisable()
        {
            if (_controller != null)
                _controller.OnGameStarted -= HandleGameStarted;
        }

        private void HandleGameStarted(GameConfiguration config)
        {
            Refresh(config);
        }

        private void Refresh(GameConfiguration config)
        {
            bool swipeMode = config.Mode == GameMode.SwipeCard || config.Mode == GameMode.FourWaySwipe;
            bool fourWay = config.Mode == GameMode.FourWaySwipe;
            SetVisible(_leftRoot, swipeMode);
            SetVisible(_rightRoot, swipeMode);
            SetVisible(_upRoot, fourWay);
            SetVisible(_downRoot, fourWay);
            if (!swipeMode)
                return;

            GameDefinition definition = _controller.Definition;
            SetLabel(_leftLabel, SwipeDirection.Left, config, definition);
            SetLabel(_rightLabel, SwipeDirection.Right, config, definition);
            if (fourWay)
            {
                SetLabel(_upLabel, SwipeDirection.Up, config, definition);
                SetLabel(_downLabel, SwipeDirection.Down, config, definition);
            }
        }

        private static void SetLabel(TMP_Text label, SwipeDirection direction, GameConfiguration config, GameDefinition definition)
        {
            if (label == null || !SwipeDirectionUtility.TryGetGroup(config, direction, out ItemGroup group))
                return;

            string arrow = direction switch
            {
                SwipeDirection.Left => "←",
                SwipeDirection.Right => "→",
                SwipeDirection.Up => "↑",
                SwipeDirection.Down => "↓",
                _ => string.Empty
            };
            label.text = $"{arrow}\n{DisplayNames.Group(group, definition)}";
        }

        private static void SetVisible(GameObject target, bool visible)
        {
            if (target != null && target.activeSelf != visible)
                target.SetActive(visible);
        }
    }
}
