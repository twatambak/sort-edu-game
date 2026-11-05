# Game history database

Completed games are stored in the local SQLite database `game-history.db3` under
`Application.persistentDataPath`. The database is created automatically and is
kept between app launches; it is local to the device and is not synchronized to
other devices.

Each completed game stores its UTC start and finish times, duration, selected
game configuration, item groups, score, error count, result, and finish reason.
Games that are still in progress are not recorded.

Use the public methods on `GameController.Instance` to query saved games:

```csharp
var recent =
    GameController.Instance.GetRecentGameSessions(20);

int completedGames = GameController.Instance.GetCompletedGameCount();
GameSessionRecord session = GameController.Instance.GetGameSessionById(1);
```

`GetRecentGameSessions` returns the newest records first and accepts a positive
maximum result count. `GetGameSessionById` returns `null` when the ID does not
exist. Each record's `Id` can be used to retrieve it again.
