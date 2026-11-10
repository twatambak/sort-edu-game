using System;
using System.Collections.Generic;
using UnityEngine;

namespace OndeVai.Data
{
    /// <summary>
    /// Facade over <see cref="GameHistoryDatabase"/> used by gameplay and UI. Tracks the active
    /// child and degrades gracefully (logging loudly) when the database could not be opened.
    /// </summary>
    public sealed class GameDataService : IDisposable
    {
        private const string ActiveChildKey = "ondevai.activeChild";

        private readonly GameHistoryDatabase _database;

        /// <summary>Raised when profiles are created, edited or removed.</summary>
        public event Action ProfilesChanged;

        /// <summary>Raised when the active child changes. The argument may be null.</summary>
        public event Action<ChildProfileRecord> ActiveChildChanged;

        /// <summary>Raised after a match is saved.</summary>
        public event Action<GameSessionRecord> SessionSaved;

        public GameDataService(GameHistoryDatabase database)
        {
            _database = database;
            if (_database == null)
                Debug.LogError("GameDataService created without a database. Progress will not be saved.");

            RestoreActiveChild();
        }

        /// <summary>False when the SQLite database failed to open.</summary>
        public bool IsAvailable => _database != null;

        /// <summary>Child currently playing, or null.</summary>
        public ChildProfileRecord ActiveChild { get; private set; }

        /// <summary>Direct access for legacy callers. Throws when the database is unavailable.</summary>
        public GameHistoryDatabase Database =>
            _database ?? throw new InvalidOperationException("The SQLite game history database is unavailable.");

        public void SetActiveChild(ChildProfileRecord profile)
        {
            ActiveChild = profile;
            PlayerPrefs.SetInt(ActiveChildKey, profile?.Id ?? 0);
            PlayerPrefs.Save();
            ActiveChildChanged?.Invoke(profile);
        }

        public List<ChildProfileRecord> GetProfiles()
        {
            return IsAvailable ? _database.GetChildProfiles() : new List<ChildProfileRecord>();
        }

        /// <summary>Creates or updates a profile. Returns false when the data is invalid or could not be stored.</summary>
        public bool SaveProfile(ChildProfileRecord profile)
        {
            if (!IsAvailable || profile == null)
                return false;

            profile.Name = SanitizeName(profile.Name);
            if (string.IsNullOrEmpty(profile.Name))
                return false;

            profile.Age = Mathf.Clamp(profile.Age, ChildProfileRecord.MinAge, ChildProfileRecord.MaxAge);
            if (profile.CreatedAtUtcTicks == 0)
                profile.CreatedAtUtcTicks = DateTime.UtcNow.Ticks;

            if (!TryRun(() => _database.SaveChildProfile(profile)))
                return false;

            ProfilesChanged?.Invoke();
            if (ActiveChild != null && ActiveChild.Id == profile.Id)
                SetActiveChild(profile);

            return true;
        }

        /// <summary>Saves a new profile and its simulated matches and attempts atomically.</summary>
        public bool SaveProfileWithSimulatedHistory(ChildProfileRecord profile, IReadOnlyList<SimulatedMatchData> matches)
        {
            if (!IsAvailable || profile == null || profile.Id != 0 || matches == null || matches.Count == 0)
                return false;

            profile.Name = SanitizeName(profile.Name);
            if (string.IsNullOrEmpty(profile.Name))
                return false;

            profile.Age = Mathf.Clamp(profile.Age, ChildProfileRecord.MinAge, ChildProfileRecord.MaxAge);
            if (profile.CreatedAtUtcTicks == 0)
                profile.CreatedAtUtcTicks = DateTime.UtcNow.Ticks;

            if (!TryRun(() => _database.SaveChildProfileWithHistory(profile, matches)))
                return false;

            ProfilesChanged?.Invoke();
            return true;
        }

        public bool DeleteProfile(ChildProfileRecord profile)
        {
            if (!IsAvailable || profile == null)
                return false;

            if (!TryRun(() => _database.DeleteChildProfile(profile.Id)))
                return false;

            if (ActiveChild != null && ActiveChild.Id == profile.Id)
                SetActiveChild(null);

            ProfilesChanged?.Invoke();
            return true;
        }

        public bool DeleteHistory(ChildProfileRecord profile)
        {
            if (!IsAvailable || profile == null)
                return false;

            bool deleted = TryRun(() => _database.DeleteChildHistory(profile.Id));
            if (deleted)
                ProfilesChanged?.Invoke();

            return deleted;
        }

        /// <summary>
        /// Saves a match for the active child. Returns the stored id, or 0 on failure.
        /// <paramref name="replaceSessionId"/> removes an earlier checkpoint of the same match.
        /// Checkpoints (<paramref name="isCheckpoint"/>) do not raise <see cref="SessionSaved"/>.
        /// </summary>
        public int SaveSession(GameSessionRecord session, IReadOnlyList<ItemAttemptRecord> attempts, int replaceSessionId = 0, bool isCheckpoint = false)
        {
            if (!IsAvailable)
            {
                Debug.LogError("Game history was not saved because the SQLite database is unavailable.");
                return 0;
            }

            session.ChildProfileId = ActiveChild?.Id ?? 0;
            session.ChildAge = ActiveChild?.Age ?? 0;
            if (!TryRun(() => _database.SaveCompletedGame(session, attempts, replaceSessionId)))
                return 0;

            if (!isCheckpoint)
                SessionSaved?.Invoke(session);
            return session.Id;
        }

        public List<GameSessionRecord> GetRecentSessions(int childProfileId, int limit)
        {
            return IsAvailable ? _database.GetRecentSessionsForChild(childProfileId, limit) : new List<GameSessionRecord>();
        }

        /// <summary>Totals of a child for list views. Empty when the database is unavailable or the query fails.</summary>
        public ChildQuickStats GetQuickStats(int childProfileId)
        {
            ChildQuickStats stats = null;
            if (IsAvailable)
                TryRun(() => stats = _database.GetQuickStats(childProfileId));

            return stats ?? new ChildQuickStats();
        }

        /// <summary>Builds the aggregated progress of a child.</summary>
        public ChildProgressSummary BuildSummary(int childProfileId)
        {
            if (!IsAvailable)
                return new ChildProgressSummary();

            List<GameSessionRecord> sessions = _database.GetRecentSessionsForChild(childProfileId, int.MaxValue);
            List<ItemAttemptRecord> attempts = _database.GetAttemptsForChild(childProfileId);
            return ProgressCalculator.Build(sessions, attempts);
        }

        /// <summary>Trims, removes control characters and limits the length of a child's name.</summary>
        public static string SanitizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            System.Text.StringBuilder builder = new(name.Length);
            foreach (char character in name.Trim())
            {
                if (!char.IsControl(character))
                    builder.Append(character);
            }

            string result = builder.ToString();
            return result.Length > ChildProfileRecord.MaxNameLength
                ? result.Substring(0, ChildProfileRecord.MaxNameLength)
                : result;
        }

        private void RestoreActiveChild()
        {
            int id = PlayerPrefs.GetInt(ActiveChildKey, 0);
            if (id == 0 || !IsAvailable)
                return;

            ChildProfileRecord profile = null;
            TryRun(() => profile = _database.GetChildProfile(id));
            ActiveChild = profile;
        }

        private static bool TryRun(Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
        }

        public void Dispose()
        {
            _database?.Dispose();
        }
    }
}
