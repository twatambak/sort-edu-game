# Game history database

All data is stored in the local SQLite database `game-history.db3` under
`Application.persistentDataPath`. It is created automatically, kept between app
launches, never synchronized and never sent anywhere.

## Tables

| Table | Content |
|---|---|
| `ChildProfileRecord` | Children registered on the device: name, age, avatar color, creation time. |
| `GameSessionRecord` | One row per match. |
| `ItemAttemptRecord` | One row per sorting attempt. |

Columns added after the first release are migrated automatically by SQLite-net.
Rows saved before a column existed hold 0 (or null for text) in it.

### GameSessionRecord

| Column | Meaning |
|---|---|
| `StartedAtUtcTicks`, `FinishedAtUtcTicks` | UTC start and finish. |
| `DurationSeconds` | Wall clock duration, pauses included. |
| `ActiveSeconds` | Time actually played, pauses excluded. |
| `PauseCount` | How many times the match was paused. |
| `MatchTimeSeconds`, `ErrorLimit`, `MaxItemsOnScreen`, `ItemsToWin`, `DelayBetweenSpawnsSeconds`, `TutorialItemCount` | Difficulty settings of the match. |
| `GameMode`, `ScenarioId`, `ItemGroupA`, `ItemGroupB` | What was played. |
| `Score`, `Errors` | Correct answers and counted mistakes. |
| `TutorialErrors` | Mistakes made while the tutorial was guiding. Not part of `Errors`. |
| `Stars` | Stars shown on the results screen, 0 to 3. |
| `BestStreak` | Longest run of consecutive correct answers. |
| `AverageResponseSeconds` | Mean response time outside the tutorial. |
| `Won`, `FinishReason` | Result. `FinishReason` is `ItemsCollected`, `ErrorLimitReached`, `TimeExpired` or `Abandoned`. |
| `ChildProfileId` | Child that played. 0 means no child was selected. |
| `ChildAge` | Age of the child when the match was played. |
| `AppVersion` | Application version that recorded the match. |

### ItemAttemptRecord

| Column | Meaning |
|---|---|
| `SessionId`, `ChildProfileId` | Match and child the attempt belongs to. |
| `ItemGroup`, `ChosenGroup` | Correct category and the category the child chose. |
| `ItemName` | Id of the item variant shown. |
| `Correct` | Whether the answer was right. |
| `ResponseSeconds` | Time between the item becoming available and the answer. |
| `DuringTutorial` | True when the tutorial pointer was guiding. |
| `AttemptIndex` | Order of the attempt inside the match, starting at 0. |
| `RetryIndex` | Answers already given for the same item. 0 is the first try. |
| `PickupCount` | Times the item was picked up before the answer. More than one suggests hesitation. |
| `SecondsIntoMatch` | Played seconds between the start of the match and the attempt. |
| `OccurredAtUtcTicks` | UTC time of the attempt. |

## When a match is saved

- When it ends by time, error limit or item goal.
- With `FinishReason = "Abandoned"` when the child leaves or restarts it after at
  least one attempt, or when the application quits. Matches with no attempts are
  not saved.
- As a checkpoint when the app goes to the background, because mobile systems may
  kill it there without notice. A checkpoint is an `Abandoned` row that the next
  save of the same match replaces, so a match is never stored twice.

`MatchSessionRecorder` collects these values during the match and builds the
rows. It has no Unity dependency.

## Access

Gameplay and UI use `GameController.Instance.Data` (`GameDataService`):

```csharp
GameDataService data = GameController.Instance.Data;
data.SetActiveChild(profile);
List<ChildProfileRecord> children = data.GetProfiles();
ChildQuickStats quick = data.GetQuickStats(childId);      // totals for list rows
ChildProgressSummary summary = data.BuildSummary(childId); // full report
List<GameSessionRecord> recent = data.GetRecentSessions(childId, 20);
data.DeleteHistory(profile);   // keeps the profile
data.DeleteProfile(profile);   // removes profile, matches and attempts
```

`ChildProgressSummary` holds accuracy per category, scenario, mode and item,
total stars, best streak, days played and play time. `ProgressCalculator` builds
it from raw rows and handles rows that predate the newer columns.

The legacy helpers on `GameController` still work:

```csharp
var recent = GameController.Instance.GetRecentGameSessions(20);
int completedGames = GameController.Instance.GetCompletedGameCount();
GameSessionRecord session = GameController.Instance.GetGameSessionById(1);
```

Types that SQLite-net fills through reflection are listed in `Assets/link.xml`
so IL2CPP does not strip them. Add new record or query result types there.

Settings and the privacy disclaimer acceptance are stored in PlayerPrefs, not in
this database.

## Simulated profile history

When **Simular histórico em novos perfis** is enabled in Options, every newly
created profile receives six imperfect matches on unique dates spanning the
previous fourteen days through today. Scores improve over those matches, and
each includes realistic attempt records and play duration. The profile,
matches and attempts are committed in a single transaction. The
`OptionsScreen` inspector option **Show Simulated History Option** controls
whether this preference appears in the Options screen; hiding it does not
disable a previously saved preference.
