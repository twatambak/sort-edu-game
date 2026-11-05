using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SQLite;
using UnityEngine;

public sealed class GameHistoryDatabase : IDisposable
{
    private readonly SQLiteConnection _connection;

    public GameHistoryDatabase()
    {
        string databasePath = Path.Combine(Application.persistentDataPath, "game-history.db3");
        SQLiteConnection connection = new SQLiteConnection(databasePath);
        try
        {
            connection.CreateTable<GameSessionRecord>();
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

    public List<GameSessionRecord> GetRecentSessions(int limit = 50)
    {
        if (limit <= 0)
            throw new ArgumentOutOfRangeException(nameof(limit), "The session limit must be greater than zero.");

        return _connection.Table<GameSessionRecord>()
            .OrderByDescending(session => session.FinishedAtUtcTicks)
            .ThenByDescending(session => session.Id)
            .Take(limit)
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

    public void Dispose()
    {
        _connection.Dispose();
    }
}
