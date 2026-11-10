using System;
using OndeVai.Gameplay;
using UnityEngine;

namespace OndeVai.Settings
{
    /// <summary>
    /// Player preferences persisted between sessions. Plain data, serialized with <see cref="JsonUtility"/>.
    /// </summary>
    [Serializable]
    public sealed class GameSettings
    {
        public const int MinMatchTime = 15;
        public const int MaxMatchTime = 300;
        public const int MinItemsOnScreen = 2;
        public const int MaxItemsOnScreen = 8;
        public const int MaxErrorLimit = 10;
        public const int MaxTutorialItems = 5;

        public float MusicVolume = 0.5f;
        public float SfxVolume = 0.8f;
        public bool VibrationEnabled = true;
        public bool TutorialEnabled = true;
        public int TutorialItemCount = 2;
        public GameMode Mode = GameMode.DragAndDrop;
        public string ScenarioId = SortingScenarioDefinition.DefaultScenarioId;
        public int MatchTimeSeconds = 30;
        public int ErrorLimit = 3;
        public int ItemsOnScreen = 5;
        public int ItemsToWin = 99;

        /// <summary>Clamps every value into its supported range. Call after loading untrusted data.</summary>
        public void Sanitize()
        {
            MusicVolume = Mathf.Clamp01(MusicVolume);
            SfxVolume = Mathf.Clamp01(SfxVolume);
            TutorialItemCount = Mathf.Clamp(TutorialItemCount, 1, MaxTutorialItems);
            MatchTimeSeconds = Mathf.Clamp(MatchTimeSeconds, MinMatchTime, MaxMatchTime);
            ErrorLimit = Mathf.Clamp(ErrorLimit, 1, MaxErrorLimit);
            ItemsOnScreen = Mathf.Clamp(ItemsOnScreen, MinItemsOnScreen, MaxItemsOnScreen);
            ItemsToWin = Mathf.Max(1, ItemsToWin);

            if (Mode != GameMode.DragAndDrop && Mode != GameMode.SwipeTwoDirections)
                Mode = GameMode.DragAndDrop;

            if (string.IsNullOrEmpty(ScenarioId))
                ScenarioId = SortingScenarioDefinition.DefaultScenarioId;
        }

        /// <summary>Creates an independent copy.</summary>
        public GameSettings Clone()
        {
            return (GameSettings)MemberwiseClone();
        }
    }
}
