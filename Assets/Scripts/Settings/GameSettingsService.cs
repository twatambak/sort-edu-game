using System;
using UnityEngine;

namespace OndeVai.Settings
{
    /// <summary>
    /// Loads, stores and broadcasts <see cref="GameSettings"/>. Persists to PlayerPrefs so it
    /// works on mobile and on the web build without extra file access.
    /// </summary>
    public sealed class GameSettingsService
    {
        public const string SettingsPrefsKey = "ondevai.settings.v1";
        public const string DisclaimerPrefsKey = "ondevai.disclaimer.accepted.v1";

        /// <summary>Raised after any change is applied and saved.</summary>
        public event Action<GameSettings> Changed;

        /// <summary>Current settings. Treat as read-only; use <see cref="Apply"/> to change them.</summary>
        public GameSettings Current { get; private set; }

        private bool _isDirty;

        public GameSettingsService()
        {
            Current = Load();
        }

        /// <summary>True once the parent has accepted the local data disclaimer.</summary>
        public bool HasAcceptedDisclaimer => PlayerPrefs.GetInt(DisclaimerPrefsKey, 0) == 1;

        /// <summary>Records the disclaimer acceptance.</summary>
        public void AcceptDisclaimer()
        {
            PlayerPrefs.SetInt(DisclaimerPrefsKey, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Applies a modification to a copy of the current settings, saves it and raises <see cref="Changed"/>.
        /// Pass <paramref name="persist"/> false for rapid changes (sliders) and call <see cref="Flush"/> afterwards.
        /// </summary>
        public void Apply(Action<GameSettings> modify, bool persist = true)
        {
            if (modify == null)
                throw new ArgumentNullException(nameof(modify));

            GameSettings next = Current.Clone();
            modify(next);
            next.Sanitize();
            Current = next;
            _isDirty = !persist;
            if (persist)
                Save(next);

            Changed?.Invoke(Current);
        }

        /// <summary>Saves pending changes made with persist set to false.</summary>
        public void Flush()
        {
            if (!_isDirty)
                return;

            _isDirty = false;
            Save(Current);
        }

        /// <summary>Restores default values.</summary>
        public void ResetToDefaults()
        {
            Apply(settings =>
            {
                GameSettings defaults = new();
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(defaults), settings);
            });
        }

        private static GameSettings Load()
        {
            GameSettings settings = new();
            string json = PlayerPrefs.GetString(SettingsPrefsKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    JsonUtility.FromJsonOverwrite(json, settings);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Saved settings are corrupted and were reset to defaults. {exception.Message}");
                    settings = new GameSettings();
                }
            }

            settings.Sanitize();
            return settings;
        }

        private static void Save(GameSettings settings)
        {
            PlayerPrefs.SetString(SettingsPrefsKey, JsonUtility.ToJson(settings));
            PlayerPrefs.Save();
        }
    }
}
