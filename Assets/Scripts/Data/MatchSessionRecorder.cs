using System;
using System.Collections.Generic;
using OndeVai.Gameplay;

namespace OndeVai.Data
{
    /// <summary>One answer given by the child, as reported by the match.</summary>
    public struct SortAttempt
    {
        public ItemGroup ItemGroup;
        public ItemGroup ChosenGroup;
        public string ItemName;
        public bool Correct;
        public bool DuringTutorial;
        public float ResponseSeconds;
        public int RetryIndex;
        public int PickupCount;
    }

    /// <summary>
    /// Collects everything worth keeping about a match while it is played and turns it into the
    /// records stored in the history database. Plain C# with no Unity dependency: the caller
    /// supplies the clock values.
    /// </summary>
    public sealed class MatchSessionRecorder
    {
        private readonly List<ItemAttemptRecord> _attempts = new();
        private GameConfiguration _config;
        private DateTime _startedAtUtc;
        private float _startedAtPlayTime;
        private int _streak;
        private int _pauseCount;
        private int _tutorialErrors;
        private double _responseTotal;
        private int _responseCount;

        /// <summary>Attempts recorded since <see cref="Begin"/>, in order.</summary>
        public IReadOnlyList<ItemAttemptRecord> Attempts => _attempts;

        public bool HasAttempts => _attempts.Count > 0;

        /// <summary>Consecutive correct answers up to now.</summary>
        public int CurrentStreak => _streak;

        /// <summary>Longest run of consecutive correct answers in the match.</summary>
        public int BestStreak { get; private set; }

        /// <summary>
        /// Starts a new match. <paramref name="playTime"/> is a clock that stops while the game is paused.
        /// </summary>
        public void Begin(GameConfiguration config, DateTime utcNow, float playTime)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _attempts.Clear();
            _startedAtUtc = utcNow;
            _startedAtPlayTime = playTime;
            _streak = 0;
            _pauseCount = 0;
            _tutorialErrors = 0;
            _responseTotal = 0;
            _responseCount = 0;
            BestStreak = 0;
        }

        public void NotifyPaused()
        {
            _pauseCount++;
        }

        public void RecordAttempt(in SortAttempt attempt, DateTime utcNow, float playTime)
        {
            _attempts.Add(new ItemAttemptRecord
            {
                ItemGroup = (int)attempt.ItemGroup,
                ChosenGroup = (int)attempt.ChosenGroup,
                ItemName = attempt.ItemName,
                Correct = attempt.Correct,
                ResponseSeconds = Math.Max(0f, attempt.ResponseSeconds),
                DuringTutorial = attempt.DuringTutorial,
                AttemptIndex = _attempts.Count,
                RetryIndex = attempt.RetryIndex,
                PickupCount = attempt.PickupCount,
                SecondsIntoMatch = Math.Max(0f, playTime - _startedAtPlayTime),
                OccurredAtUtcTicks = utcNow.Ticks
            });

            if (attempt.Correct)
            {
                _streak++;
                BestStreak = Math.Max(BestStreak, _streak);
            }
            else
            {
                _streak = 0;
                if (attempt.DuringTutorial)
                    _tutorialErrors++;
            }

            if (attempt.DuringTutorial)
                return;

            _responseTotal += Math.Max(0f, attempt.ResponseSeconds);
            _responseCount++;
        }

        /// <summary>Builds the session row for the match in its current state.</summary>
        public GameSessionRecord BuildRecord(bool won, string finishReason, int score, int errors, DateTime utcNow, float playTime, string appVersion)
        {
            if (_config == null)
                throw new InvalidOperationException("MatchSessionRecorder.Begin must be called before building a record.");

            return new GameSessionRecord
            {
                StartedAtUtcTicks = _startedAtUtc.Ticks,
                FinishedAtUtcTicks = utcNow.Ticks,
                DurationSeconds = Math.Max(0, (utcNow - _startedAtUtc).TotalSeconds),
                ActiveSeconds = Math.Max(0f, playTime - _startedAtPlayTime),
                MatchTimeSeconds = _config.MatchTime,
                ErrorLimit = _config.ErrorLimit,
                MaxItemsOnScreen = _config.MaxItemsOnScreen,
                ItemsToWin = _config.ItemsToWin,
                DelayBetweenSpawnsSeconds = _config.DelayBetweenSpawns,
                ItemGroupA = (int)_config.ItemGroupA,
                ItemGroupB = (int)_config.ItemGroupB,
                Score = score,
                Errors = errors,
                Won = won,
                FinishReason = finishReason,
                GameMode = (int)_config.Mode,
                ScenarioId = _config.ScenarioId,
                TutorialItemCount = _config.TutorialItemCount,
                Stars = MatchScoring.CalculateStars(score, errors),
                BestStreak = BestStreak,
                PauseCount = _pauseCount,
                TutorialErrors = _tutorialErrors,
                AverageResponseSeconds = _responseCount > 0 ? (float)(_responseTotal / _responseCount) : 0f,
                AppVersion = appVersion
            };
        }
    }
}
