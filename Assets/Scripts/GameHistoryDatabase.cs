using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using OndeVai.Data;
using SQLite;
using UnityEngine;

/// <summary>
/// Local SQLite storage for child profiles, finished matches and item attempts.
/// The file lives in <see cref="Application.persistentDataPath"/> and never leaves the device.
/// </summary>
public sealed class GameHistoryDatabase : IDisposable
{
    public const string FileName = "game-history.db3";

    private readonly SQLiteConnection _connection;

    public GameHistoryDatabase() : this(Path.Combine(Application.persistentDataPath, FileName))
    {
    }

    /// <summary>Opens (or creates) the database at a custom path. Useful for tests.</summary>
    public GameHistoryDatabase(string databasePath)
    {
        SQLiteConnection connection = new SQLiteConnection(databasePath);
        try
        {
            connection.CreateTable<GameSessionRecord>();
            connection.CreateTable<ChildProfileRecord>();
            connection.CreateTable<ItemAttemptRecord>();

            // Rows saved before child profiles existed have NULL in the migrated column.
            connection.Execute($"UPDATE {nameof(GameSessionRecord)} SET {nameof(GameSessionRecord.ChildProfileId)} = 0 WHERE {nameof(GameSessionRecord.ChildProfileId)} IS NULL");
            _connection = connection;
            EnsureProfileUserCodes();
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    public int SaveCompletedGame(GameSessionRecord session)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        _connection.Insert(session);
        return session.Id;
    }

    /// <summary>
    /// Saves a match and its attempts atomically. Attempts receive the new session id.
    /// When <paramref name="replaceSessionId"/> is set, that earlier save of the same match
    /// (a checkpoint) is removed in the same transaction.
    /// </summary>
    public int SaveCompletedGame(GameSessionRecord session, IReadOnlyList<ItemAttemptRecord> attempts, int replaceSessionId = 0)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        _connection.RunInTransaction(() =>
        {
            if (replaceSessionId > 0)
                DeleteSessionInternal(replaceSessionId);

            _connection.Insert(session);
            if (attempts == null)
                return;

            foreach (ItemAttemptRecord attempt in attempts)
            {
                attempt.SessionId = session.Id;
                attempt.ChildProfileId = session.ChildProfileId;
                _connection.Insert(attempt);
            }
        });

        return session.Id;
    }

    public List<GameSessionRecord> GetRecentSessions(int limit = 50)
    {
        ValidateLimit(limit);

        return _connection.Table<GameSessionRecord>()
            .OrderByDescending(session => session.FinishedAtUtcTicks)
            .ThenByDescending(session => session.Id)
            .Take(limit)
            .ToList();
    }

    /// <summary>Returns every saved match, oldest first, for a complete data export.</summary>
    public List<GameSessionRecord> GetAllSessionsForExport()
    {
        return _connection.Table<GameSessionRecord>()
            .OrderBy(session => session.StartedAtUtcTicks)
            .ThenBy(session => session.Id)
            .ToList();
    }

    /// <summary>Returns every saved attempt, in database order, for a complete data export.</summary>
    public List<ItemAttemptRecord> GetAllAttemptsForExport()
    {
        return _connection.Table<ItemAttemptRecord>()
            .OrderBy(attempt => attempt.Id)
            .ToList();
    }

    /// <summary>Newest matches of one child first.</summary>
    public List<GameSessionRecord> GetRecentSessionsForChild(int childProfileId, int limit = 50)
    {
        ValidateLimit(limit);

        return _connection.Table<GameSessionRecord>()
            .Where(session => session.ChildProfileId == childProfileId)
            .OrderByDescending(session => session.FinishedAtUtcTicks)
            .ThenByDescending(session => session.Id)
            .Take(limit)
            .ToList();
    }

    public List<ItemAttemptRecord> GetAttemptsForChild(int childProfileId)
    {
        return _connection.Table<ItemAttemptRecord>()
            .Where(attempt => attempt.ChildProfileId == childProfileId)
            .ToList();
    }

    public GameSessionRecord GetSessionById(int id)
    {
        return _connection.Find<GameSessionRecord>(id);
    }

    public int GetCompletedSessionCount()
    {
        return _connection.Table<GameSessionRecord>().Count();
    }

    public List<ChildProfileRecord> GetChildProfiles()
    {
        return _connection.Table<ChildProfileRecord>()
            .OrderBy(profile => profile.Name)
            .ToList();
    }

    public ChildProfileRecord GetChildProfile(int id)
    {
        return _connection.Find<ChildProfileRecord>(id);
    }

    /// <summary>Inserts a new profile or updates an existing one, based on its id.</summary>
    public void SaveChildProfile(ChildProfileRecord profile)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));

        EnsureUserCode(profile);
        if (profile.Id == 0)
            _connection.Insert(profile);
        else
            _connection.Update(profile);
    }

    /// <summary>Saves a new profile, its matches and their attempts in one transaction.</summary>
    public void SaveChildProfileWithHistory(ChildProfileRecord profile, IReadOnlyList<SimulatedMatchData> matches)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));
        if (profile.Id != 0)
            throw new ArgumentException("Simulated history can only be saved for a new profile.", nameof(profile));
        if (matches == null)
            throw new ArgumentNullException(nameof(matches));

        _connection.RunInTransaction(() =>
        {
            EnsureUserCode(profile);
            _connection.Insert(profile);
            foreach (SimulatedMatchData match in matches)
            {
                if (match == null || match.Session == null || match.Attempts == null)
                    throw new ArgumentException("Simulated history contains an invalid match.", nameof(matches));

                GameSessionRecord session = match.Session;
                session.ChildProfileId = profile.Id;
                session.ChildAge = profile.Age;
                _connection.Insert(session);

                foreach (ItemAttemptRecord attempt in match.Attempts)
                {
                    attempt.SessionId = session.Id;
                    attempt.ChildProfileId = profile.Id;
                    _connection.Insert(attempt);
                }
            }
        });
    }

    /// <summary>Removes the profile together with every match and attempt recorded for it.</summary>
    public void DeleteChildProfile(int id)
    {
        _connection.RunInTransaction(() =>
        {
            DeleteChildHistoryInternal(id);
            _connection.Delete<ChildProfileRecord>(id);
        });
    }

    /// <summary>Deletes every saved child profile and its history, preserving guest match history.</summary>
    public void DeleteAllChildProfiles()
    {
        _connection.RunInTransaction(() =>
        {
            _connection.Execute(
                $"DELETE FROM {nameof(ItemAttemptRecord)} WHERE {nameof(ItemAttemptRecord.ChildProfileId)} IN " +
                $"(SELECT {nameof(ChildProfileRecord.Id)} FROM {nameof(ChildProfileRecord)})");
            _connection.Execute(
                $"DELETE FROM {nameof(GameSessionRecord)} WHERE {nameof(GameSessionRecord.ChildProfileId)} IN " +
                $"(SELECT {nameof(ChildProfileRecord.Id)} FROM {nameof(ChildProfileRecord)})");
            _connection.DeleteAll<ChildProfileRecord>();
        });
    }

    /// <summary>Removes every match and attempt of the child but keeps the profile.</summary>
    public void DeleteChildHistory(int id)
    {
        _connection.RunInTransaction(() => DeleteChildHistoryInternal(id));
    }

    /// <summary>Match count, totals and last play date of one child, computed by the database.</summary>
    public ChildQuickStats GetQuickStats(int childProfileId)
    {
        List<ChildQuickStats> rows = _connection.Query<ChildQuickStats>(
            $"SELECT COUNT(*) AS {nameof(ChildQuickStats.GamesPlayed)}, " +
            $"COALESCE(SUM({nameof(GameSessionRecord.Score)}), 0) AS {nameof(ChildQuickStats.TotalCorrect)}, " +
            $"COALESCE(SUM({nameof(GameSessionRecord.Errors)}), 0) AS {nameof(ChildQuickStats.TotalErrors)}, " +
            $"COALESCE(MAX({nameof(GameSessionRecord.FinishedAtUtcTicks)}), 0) AS {nameof(ChildQuickStats.LastPlayedUtcTicks)} " +
            $"FROM {nameof(GameSessionRecord)} WHERE {nameof(GameSessionRecord.ChildProfileId)} = ?",
            childProfileId);

        return rows.Count > 0 ? rows[0] : new ChildQuickStats();
    }

    private void DeleteSessionInternal(int sessionId)
    {
        _connection.Execute($"DELETE FROM {nameof(ItemAttemptRecord)} WHERE {nameof(ItemAttemptRecord.SessionId)} = ?", sessionId);
        _connection.Delete<GameSessionRecord>(sessionId);
    }

    private void EnsureProfileUserCodes()
    {
        _connection.RunInTransaction(() =>
        {
            List<ChildProfileRecord> profiles = _connection.Table<ChildProfileRecord>()
                .OrderBy(profile => profile.Id)
                .ToList();
            HashSet<string> usedCodes = new(StringComparer.OrdinalIgnoreCase);
            System.Random random = new(unchecked((int)DateTime.UtcNow.Ticks));

            foreach (ChildProfileRecord profile in profiles)
            {
                if (string.IsNullOrWhiteSpace(profile.UserCode) || !usedCodes.Add(profile.UserCode))
                {
                    profile.UserCode = GenerateUniqueUserCode(profile.Name, usedCodes, random);
                    usedCodes.Add(profile.UserCode);
                    _connection.Update(profile);
                }
            }
        });
    }

    private void EnsureUserCode(ChildProfileRecord profile)
    {
        if (profile.Id != 0 && !string.IsNullOrWhiteSpace(profile.UserCode))
            return;

        List<ChildProfileRecord> profiles = _connection.Table<ChildProfileRecord>().ToList();
        HashSet<string> usedCodes = new(
            profiles
                .Where(existing => existing.Id != profile.Id)
                .Select(existing => existing.UserCode)
                .Where(code => !string.IsNullOrWhiteSpace(code)),
            StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(profile.UserCode) || usedCodes.Contains(profile.UserCode))
            profile.UserCode = GenerateUniqueUserCode(profile.Name, usedCodes, new System.Random());
    }

    private static string GenerateUniqueUserCode(string name, HashSet<string> usedCodes, System.Random random)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A profile name is required to generate a user code.", nameof(name));

        char initial = char.ToUpperInvariant(name.Trim()[0]);
        int firstNumber = random.Next(0, 1000);
        for (int i = 0; i < 1000; i++)
        {
            int number = (firstNumber + i) % 1000;
            string code = initial + number.ToString("D3", CultureInfo.InvariantCulture);
            if (!usedCodes.Contains(code))
                return code;
        }

        throw new InvalidOperationException($"All three-digit user codes for initial '{initial}' are already in use.");
    }

    private void DeleteChildHistoryInternal(int id)
    {
        _connection.Execute($"DELETE FROM {nameof(ItemAttemptRecord)} WHERE {nameof(ItemAttemptRecord.ChildProfileId)} = ?", id);
        _connection.Execute($"DELETE FROM {nameof(GameSessionRecord)} WHERE {nameof(GameSessionRecord.ChildProfileId)} = ?", id);
    }

    private static void ValidateLimit(int limit)
    {
        if (limit <= 0)
            throw new ArgumentOutOfRangeException(nameof(limit), "The session limit must be greater than zero.");
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
