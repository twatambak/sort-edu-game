namespace OndeVai.Gameplay
{
    /// <summary>
    /// How the child sorts items. Serialized as an integer in settings and history, append only.
    /// </summary>
    public enum GameMode
    {
        /// <summary>Several items on screen, dragged into the storages.</summary>
        DragAndDrop = 0,

        /// <summary>One item at a time, the child taps the storage it belongs to.</summary>
        Question = 1,

        /// <summary>One card at a time, swiped right for group A or left for group B.</summary>
        SwipeCard = 2,

        /// <summary>One card at a time, sorted using all four swipe directions.</summary>
        FourWaySwipe = 3
    }
}
