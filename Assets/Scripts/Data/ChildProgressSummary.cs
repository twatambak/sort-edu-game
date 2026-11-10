using System;
using System.Collections.Generic;
using OndeVai.Gameplay;

namespace OndeVai.Data
{
    /// <summary>Correct/total tally for one category of results.</summary>
    public struct AccuracyTally
    {
        public int Correct;
        public int Total;

        public int Errors => Total - Correct;

        /// <summary>0..1, or -1 when there is no data.</summary>
        public float Ratio => Total > 0 ? (float)Correct / Total : -1f;
    }

    /// <summary>Results of the attempts made on one item variant.</summary>
    public struct ItemTally
    {
        public string ItemName;
        public ItemGroup Group;
        public AccuracyTally Accuracy;
    }

    /// <summary>
    /// Totals of one child read straight from the database. Cheap enough to show on every row of
    /// the profile list. Properties are filled by SQLite-net through reflection.
    /// </summary>
    public sealed class ChildQuickStats
    {
        public int GamesPlayed { get; set; }
        public int TotalCorrect { get; set; }
        public int TotalErrors { get; set; }
        public long LastPlayedUtcTicks { get; set; }

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

    /// <summary>Aggregated statistics of a child, computed from the history database.</summary>
    public sealed class ChildProgressSummary
    {
        public int GamesPlayed;
        public int GamesFinished;
        public int Wins;
        public int TotalCorrect;
        public int TotalErrors;
        public int TotalStars;
        public int BestStreak;
        public int DaysPlayed;
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

        /// <summary>Results per item variant id (non-tutorial attempts only).</summary>
        public readonly Dictionary<string, ItemTally> ResultsByItem = new();

        /// <summary>Overall accuracy, 0..1, or -1 when there is no data.</summary>
        public float Accuracy
        {
            get
            {
                int total = TotalCorrect + TotalErrors;
                return total > 0 ? (float)TotalCorrect / total : -1f;
            }
        }

        /// <summary>
        /// Fills <paramref name="results"/> with the items the child misses most, worst first.
        /// Items need at least <paramref name="minimumAttempts"/> attempts and one error to count.
        /// </summary>
        public void GetHardestItems(List<ItemTally> results, int maximumCount, int minimumAttempts = 2)
        {
            results.Clear();
            foreach (ItemTally tally in ResultsByItem.Values)
            {
                if (tally.Accuracy.Total >= minimumAttempts && tally.Accuracy.Errors > 0)
                    results.Add(tally);
            }

            results.Sort(CompareByDifficulty);
            if (results.Count > maximumCount)
                results.RemoveRange(maximumCount, results.Count - maximumCount);
        }

        private static int CompareByDifficulty(ItemTally a, ItemTally b)
        {
            int byRatio = a.Accuracy.Ratio.CompareTo(b.Accuracy.Ratio);
            if (byRatio != 0)
                return byRatio;

            int byErrors = b.Accuracy.Errors.CompareTo(a.Accuracy.Errors);
            return byErrors != 0 ? byErrors : string.CompareOrdinal(a.ItemName, b.ItemName);
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

        /// <summary>Stars of a match. Rows saved before stars were stored are scored from their totals.</summary>
        public static int StarsOf(GameSessionRecord session)
        {
            return session.Stars > 0 ? session.Stars : MatchScoring.CalculateStars(session.Score, session.Errors);
        }

        /// <summary>Seconds played in a match. Rows saved before pauses were tracked fall back to the wall clock duration.</summary>
        public static double PlaySecondsOf(GameSessionRecord session)
        {
            return session.ActiveSeconds > 0 ? session.ActiveSeconds : session.DurationSeconds;
        }

        private static void AccumulateSessions(ChildProgressSummary summary, IReadOnlyList<GameSessionRecord> sessions)
        {
            if (sessions == null)
                return;

            long lastTicks = 0;
            HashSet<long> days = new();
            foreach (GameSessionRecord session in sessions)
            {
                summary.GamesPlayed++;
                if (session.FinishReason != GameSessionRecord.ReasonAbandoned)
                    summary.GamesFinished++;

                if (session.Won)
                    summary.Wins++;

                summary.TotalCorrect += session.Score;
                summary.TotalErrors += session.Errors;
                summary.TotalStars += StarsOf(session);
                summary.TotalPlaySeconds += PlaySecondsOf(session);
                summary.BestScore = Math.Max(summary.BestScore, session.Score);
                summary.BestStreak = Math.Max(summary.BestStreak, session.BestStreak);
                lastTicks = Math.Max(lastTicks, session.FinishedAtUtcTicks);
                days.Add(new DateTime(session.FinishedAtUtcTicks, DateTimeKind.Utc).ToLocalTime().Date.Ticks);

                string scenarioId = string.IsNullOrEmpty(session.ScenarioId) ? "?" : session.ScenarioId;
                Add(summary.AccuracyByScenario, scenarioId, session.Score, session.Score + session.Errors);
                Add(summary.AccuracyByMode, session.GameMode, session.Score, session.Score + session.Errors);
            }

            summary.DaysPlayed = days.Count;
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

                int correct = attempt.Correct ? 1 : 0;
                ItemGroup group = (ItemGroup)attempt.ItemGroup;
                Add(summary.AccuracyByGroup, group, correct, 1);
                AddItem(summary.ResultsByItem, attempt.ItemName, group, correct);
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

        private static void AddItem(Dictionary<string, ItemTally> map, string itemName, ItemGroup group, int correct)
        {
            if (string.IsNullOrEmpty(itemName))
                return;

            if (!map.TryGetValue(itemName, out ItemTally tally))
                tally = new ItemTally { ItemName = itemName, Group = group };

            tally.Accuracy.Correct += correct;
            tally.Accuracy.Total++;
            map[itemName] = tally;
        }
    }
}
