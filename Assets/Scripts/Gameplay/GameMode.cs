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
}
