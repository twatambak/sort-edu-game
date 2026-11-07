# Game history database

All data is stored in the local SQLite database `game-history.db3` under
`Application.persistentDataPath`. It is created automatically, kept between app
launches, never synchronized and never sent anywhere.

## Tables

| Table | Content |
|---|---|
| `ChildProfileRecord` | Children registered on the device: name, age, avatar color, creation time. |
| `GameSessionRecord` | One row per match: UTC start/finish, duration, configuration, groups, score, errors, result, finish reason, child id, game mode, scenario id, tutorial item count. |
| `ItemAttemptRecord` | One row per sorting attempt: session id, child id, correct group, chosen group, item name, correct flag, response time, tutorial flag. |

`ChildProfileId = 0` means the match was played without a selected child.
Matches left early with at least one attempt are saved with
`FinishReason = "Abandoned"`. Columns added after the first release are migrated
automatically by SQLite-net.

## Access

Gameplay and UI use `GameController.Instance.Data` (`GameDataService`):

```csharp
GameDataService data = GameController.Instance.Data;
data.SetActiveChild(profile);
List<ChildProfileRecord> children = data.GetProfiles();
ChildProgressSummary summary = data.BuildSummary(childId);
List<GameSessionRecord> recent = data.GetRecentSessions(childId, 20);
data.DeleteHistory(profile);   // keeps the profile
data.DeleteProfile(profile);   // removes profile, matches and attempts
```

The legacy helpers on `GameController` still work:

```csharp
var recent = GameController.Instance.GetRecentGameSessions(20);
int completedGames = GameController.Instance.GetCompletedGameCount();
GameSessionRecord session = GameController.Instance.GetGameSessionById(1);
```

Settings and the privacy disclaimer acceptance are stored in PlayerPrefs, not in
this database.
