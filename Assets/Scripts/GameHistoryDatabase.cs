using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        if (profile.Id == 0)
            _connection.Insert(profile);
        else
            _connection.Update(profile);
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
