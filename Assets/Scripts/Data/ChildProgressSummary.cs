using System;
using System.Collections.Generic;

namespace OndeVai.Data
{
    /// <summary>Correct/total tally for one category of results.</summary>
    public struct AccuracyTally
    {
        public int Correct;
        public int Total;

        /// <summary>0..1, or -1 when there is no data.</summary>
        public float Ratio => Total > 0 ? (float)Correct / Total : -1f;
    }

    /// <summary>Aggregated statistics of a child, computed from the history database.</summary>
    public sealed class ChildProgressSummary
    {
        public int GamesPlayed;
        public int GamesFinished;
        public int Wins;
        public int TotalCorrect;
        public int TotalErrors;
        public double TotalPlaySeconds;
        public float AverageResponseSeconds;
        public DateTime? LastPlayedUtc;
        public int BestScore;

        /// <summary>Accuracy per <see cref="ItemGroup"/> (non-tutorial attempts only).</summary>
        public readonly Dictionary<ItemGroup, AccuracyTally> AccuracyByGroup = new();

        /// <summary>Accuracy per scenario id.</summary>
        public readonly Dictionary<string, AccuracyTally> AccuracyByScenario = new();

        /// <summary>Accuracy per game mode, keyed by the integer value of the mode.</summary>
        public readonly Dictionary<int, AccuracyTally> AccuracyByMode = new();

        /// <summary>Overall accuracy, 0..1, or -1 when there is no data.</summary>
        public float Accuracy
        {
            get
            {
                int total = TotalCorrect + TotalErrors;
                return total > 0 ? (float)TotalCorrect / total : -1f;
            }
        }
    }

    /// <summary>
    /// Pure functions that turn raw records into a <see cref="ChildProgressSummary"/>. No Unity dependency.
    /// </summary>
    public static class ProgressCalculator
    {
        public static ChildProgressSummary Build(IReadOnlyList<GameSessionRecord> sessions, IReadOnlyList<ItemAttemptRecord> attempts)
        {
            ChildProgressSummary summary = new();
            AccumulateSessions(summary, sessions);
            AccumulateAttempts(summary, attempts);
            return summary;
        }

        private static void AccumulateSessions(ChildProgressSummary summary, IReadOnlyList<GameSessionRecord> sessions)
        {
            if (sessions == null)
                return;

            long lastTicks = 0;
            foreach (GameSessionRecord session in sessions)
            {
                summary.GamesPlayed++;
                if (session.FinishReason != GameSessionRecord.ReasonAbandoned)
                    summary.GamesFinished++;

                if (session.Won)
                    summary.Wins++;

                summary.TotalCorrect += session.Score;
                summary.TotalErrors += session.Errors;
                summary.TotalPlaySeconds += session.DurationSeconds;
                summary.BestScore = Math.Max(summary.BestScore, session.Score);
                lastTicks = Math.Max(lastTicks, session.FinishedAtUtcTicks);

                string scenarioId = string.IsNullOrEmpty(session.ScenarioId) ? "?" : session.ScenarioId;
                Add(summary.AccuracyByScenario, scenarioId, session.Score, session.Score + session.Errors);
                Add(summary.AccuracyByMode, session.GameMode, session.Score, session.Score + session.Errors);
            }

            if (lastTicks > 0)
                summary.LastPlayedUtc = new DateTime(lastTicks, DateTimeKind.Utc);
        }

        private static void AccumulateAttempts(ChildProgressSummary summary, IReadOnlyList<ItemAttemptRecord> attempts)
        {
            if (attempts == null)
                return;

            double responseTotal = 0;
            int responseCount = 0;
            foreach (ItemAttemptRecord attempt in attempts)
            {
                if (attempt.DuringTutorial)
                    continue;

                Add(summary.AccuracyByGroup, (ItemGroup)attempt.ItemGroup, attempt.Correct ? 1 : 0, 1);
                responseTotal += attempt.ResponseSeconds;
                responseCount++;
            }

            summary.AverageResponseSeconds = responseCount > 0 ? (float)(responseTotal / responseCount) : 0f;
        }

        private static void Add<TKey>(Dictionary<TKey, AccuracyTally> map, TKey key, int correct, int total)
        {
            map.TryGetValue(key, out AccuracyTally tally);
            tally.Correct += correct;
            tally.Total += total;
            map[key] = tally;
        }
    }
}
