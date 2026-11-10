namespace OndeVai.Gameplay
{
    /// <summary>
    /// How the child sorts items. Values are stored in settings and history.
    /// </summary>
    public enum GameMode
    {
        /// <summary>Several items on screen, dragged into the storages.</summary>
        DragAndDrop = 0,

        /// <summary>One item at a time, swiped right for group A or left for group B.</summary>
        SwipeTwoDirections = 2
    }

    /// <summary>Single place that knows which <see cref="GameMode"/> values can be played.</summary>
    public static class GameModes
    {
        /// <summary>False for values of removed modes that may still be found in old settings or history.</summary>
        public static bool IsSupported(GameMode mode)
        {
            return mode == GameMode.DragAndDrop || mode == GameMode.SwipeTwoDirections;
        }
    }
}
