using System;
using System.Collections.Generic;
using OndeVai.Gameplay;
using UnityEngine;
using Random = System.Random;

namespace OndeVai.Data
{
    /// <summary>A simulated match and its item attempts, ready to be saved together.</summary>
    public sealed class SimulatedMatchData
    {
        public GameSessionRecord Session { get; }
        public IReadOnlyList<ItemAttemptRecord> Attempts { get; }

        public SimulatedMatchData(GameSessionRecord session, IReadOnlyList<ItemAttemptRecord> attempts)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            Attempts = attempts ?? throw new ArgumentNullException(nameof(attempts));
        }
    }

    /// <summary>Builds a short, improving but imperfect play history for a new profile.</summary>
    public static class ProfileHistorySimulator
    {
        private static readonly RoundResult[] Results =
        {
            new(3, 4, 4, 6, GameSessionRecord.ReasonErrorLimit),
            new(4, 3, 3, 7, GameSessionRecord.ReasonErrorLimit),
            new(5, 3, 4, 8, GameSessionRecord.ReasonTimeExpired),
            new(7, 2, 3, 7, GameSessionRecord.ReasonItemsCollected),
            new(8, 2, 3, 8, GameSessionRecord.ReasonItemsCollected),
            new(9, 1, 2, 9, GameSessionRecord.ReasonItemsCollected)
        };

        /// <summary>Creates six matches with unique play dates spanning the last fourteen days.</summary>
        public static List<SimulatedMatchData> Generate(
            GameDefinition definition,
            GameConfiguration configuration,
            DateTime nowUtc,
            Random random)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            DateTime utcNow = nowUtc.Kind == DateTimeKind.Local
                ? nowUtc.ToUniversalTime()
                : DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
            List<int> daysAgo = CreatePlayDates(random);
            List<SimulatedMatchData> matches = new(Results.Length);
            int matchTime = Math.Max(15, configuration.MatchTime);

            for (int i = 0; i < Results.Length; i++)
            {
                DateTime finishedAt = CreateFinishTime(utcNow, daysAgo[i], random);
                matches.Add(CreateMatch(definition, configuration, Results[i], finishedAt, matchTime, random));
            }

            return matches;
        }

        private static List<int> CreatePlayDates(Random random)
        {
            List<int> dates = new() { 14, 0 };
            while (dates.Count < Results.Length)
            {
                int candidate = random.Next(1, 14);
                if (!dates.Contains(candidate))
                    dates.Add(candidate);
            }

            dates.Sort((left, right) => right.CompareTo(left));
            return dates;
        }

        private static DateTime CreateFinishTime(DateTime utcNow, int daysAgo, Random random)
        {
            DateTime day = utcNow.Date.AddDays(-daysAgo);
            int latestSecond = daysAgo == 0
                ? (int)(utcNow - day).TotalSeconds
                : 86399;
            int seconds = random.Next(0, Math.Max(0, latestSecond) + 1);
            return day.AddSeconds(seconds);
        }

        private static SimulatedMatchData CreateMatch(
            GameDefinition definition,
            GameConfiguration configuration,
            RoundResult result,
            DateTime finishedAt,
            int matchTime,
            Random random)
        {
            int tutorialCount = Math.Min(Math.Min(configuration.TutorialItemCount, result.Score), 2);
            List<AttemptPlan> plans = CreateAttemptPlans(definition, configuration, result, tutorialCount, random);
            int pauseCount = random.Next(0, 4) == 0 ? 1 : 0;
            double pauseSeconds = pauseCount > 0 ? random.Next(5, 21) : 0;
            double activeSeconds = result.FinishReason == GameSessionRecord.ReasonTimeExpired
                ? matchTime
                : Math.Max(5, Math.Round(matchTime * RandomRange(random, 0.4f, 0.8f)));
            double durationSeconds = activeSeconds + pauseSeconds;
            DateTime startedAt = finishedAt.AddSeconds(-durationSeconds);

            List<ItemAttemptRecord> attempts = BuildAttempts(plans, startedAt, activeSeconds, out int bestStreak, out float averageResponse);
            GameSessionRecord session = new()
            {
                StartedAtUtcTicks = startedAt.Ticks,
                FinishedAtUtcTicks = finishedAt.Ticks,
                DurationSeconds = durationSeconds,
                ActiveSeconds = activeSeconds,
                MatchTimeSeconds = matchTime,
                ErrorLimit = result.ErrorLimit,
                MaxItemsOnScreen = configuration.MaxItemsOnScreen,
                ItemsToWin = result.ItemsToWin,
                DelayBetweenSpawnsSeconds = configuration.DelayBetweenSpawns,
                ItemGroupA = (int)configuration.ItemGroupA,
                ItemGroupB = (int)configuration.ItemGroupB,
                Score = result.Score,
                Errors = result.Errors,
                Won = result.FinishReason == GameSessionRecord.ReasonItemsCollected,
                FinishReason = result.FinishReason,
                GameMode = (int)configuration.Mode,
                ScenarioId = configuration.ScenarioId,
                TutorialItemCount = tutorialCount,
                Stars = MatchScoring.CalculateStars(result.Score, result.Errors),
                BestStreak = bestStreak,
                PauseCount = pauseCount,
                TutorialErrors = 0,
                AverageResponseSeconds = averageResponse,
                AppVersion = Application.version
            };

            return new SimulatedMatchData(session, attempts);
        }

        private static List<AttemptPlan> CreateAttemptPlans(
            GameDefinition definition,
            GameConfiguration configuration,
            RoundResult result,
            int tutorialCount,
            Random random)
        {
            List<AttemptPlan> plans = new(result.Score + result.Errors);
            for (int i = 0; i < tutorialCount; i++)
            {
                ItemGroup group = PickGroup(configuration, random);
                plans.Add(CreatePlan(definition, group, group, true, true, 0, 1, random));
            }

            int regularCorrect = result.Score - tutorialCount;
            int recoveredErrors = Math.Min(result.Errors, regularCorrect);
            List<List<AttemptPlan>> blocks = new(result.Errors + regularCorrect);

            for (int i = 0; i < result.Errors; i++)
            {
                ItemGroup group = PickGroup(configuration, random);
                ItemGroup wrongGroup = group == configuration.ItemGroupA
                    ? configuration.ItemGroupB
                    : configuration.ItemGroupA;
                string itemName = PickItemName(definition, group, random);
                List<AttemptPlan> block = new()
                {
                    new AttemptPlan(group, wrongGroup, itemName, false, false, 0, random.Next(1, 3), RandomRange(random, 1f, 4f))
                };

                if (i < recoveredErrors)
                    block.Add(new AttemptPlan(group, group, itemName, true, false, 1, random.Next(1, 3), RandomRange(random, 0.7f, 3f)));

                blocks.Add(block);
            }

            for (int i = recoveredErrors; i < regularCorrect; i++)
            {
                ItemGroup group = PickGroup(configuration, random);
                blocks.Add(new List<AttemptPlan>
                {
                    CreatePlan(definition, group, group, true, false, 0, 1, random)
                });
            }

            Shuffle(blocks, random);
            foreach (List<AttemptPlan> block in blocks)
                plans.AddRange(block);

            return plans;
        }

        private static AttemptPlan CreatePlan(
            GameDefinition definition,
            ItemGroup group,
            ItemGroup chosenGroup,
            bool correct,
            bool duringTutorial,
            int retryIndex,
            int pickupCount,
            Random random)
        {
            return new AttemptPlan(
                group,
                chosenGroup,
                PickItemName(definition, group, random),
                correct,
                duringTutorial,
                retryIndex,
                pickupCount,
                RandomRange(random, 0.7f, 3f));
        }

        private static List<ItemAttemptRecord> BuildAttempts(
            List<AttemptPlan> plans,
            DateTime startedAt,
            double activeSeconds,
            out int bestStreak,
            out float averageResponse)
        {
            List<ItemAttemptRecord> attempts = new(plans.Count);
            double responseTotal = 0;
            int responseCount = 0;
            int streak = 0;
            bestStreak = 0;

            for (int i = 0; i < plans.Count; i++)
            {
                AttemptPlan plan = plans[i];
                float secondsIntoMatch = (float)(activeSeconds * (i + 1) / plans.Count);
                attempts.Add(new ItemAttemptRecord
                {
                    ItemGroup = (int)plan.Group,
                    ChosenGroup = (int)plan.ChosenGroup,
                    ItemName = plan.ItemName,
                    Correct = plan.Correct,
                    ResponseSeconds = plan.ResponseSeconds,
                    DuringTutorial = plan.DuringTutorial,
                    AttemptIndex = i,
                    RetryIndex = plan.RetryIndex,
                    PickupCount = plan.PickupCount,
                    SecondsIntoMatch = secondsIntoMatch,
                    OccurredAtUtcTicks = startedAt.AddSeconds(secondsIntoMatch).Ticks
                });

                streak = plan.Correct ? streak + 1 : 0;
                bestStreak = Math.Max(bestStreak, streak);
                if (!plan.DuringTutorial)
                {
                    responseTotal += plan.ResponseSeconds;
                    responseCount++;
                }
            }

            averageResponse = responseCount > 0 ? (float)(responseTotal / responseCount) : 0f;
            return attempts;
        }

        private static ItemGroup PickGroup(GameConfiguration configuration, Random random)
        {
            return random.Next(0, 2) == 0 ? configuration.ItemGroupA : configuration.ItemGroupB;
        }

        private static string PickItemName(GameDefinition definition, ItemGroup group, Random random)
        {
            ItemTypeDefinition type = definition != null ? definition.GetItemType(group) : null;
            if (type != null)
            {
                List<string> itemNames = new();
                foreach (ItemVariant variant in type.Variants)
                {
                    if (variant == null || !variant.IsPlayable)
                        continue;

                    string itemName = string.IsNullOrWhiteSpace(variant.Id) ? variant.Sprite.name : variant.Id;
                    itemNames.Add(itemName);
                }

                if (itemNames.Count > 0)
                    return itemNames[random.Next(itemNames.Count)];
            }

            return $"{group.ToString().ToLowerInvariant()}-{random.Next(1, 5)}";
        }

        private static float RandomRange(Random random, float minimum, float maximum)
        {
            return minimum + (float)random.NextDouble() * (maximum - minimum);
        }

        private static void Shuffle<T>(IList<T> values, Random random)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (values[i], values[swapIndex]) = (values[swapIndex], values[i]);
            }
        }

        private readonly struct RoundResult
        {
            public readonly int Score;
            public readonly int Errors;
            public readonly int ErrorLimit;
            public readonly int ItemsToWin;
            public readonly string FinishReason;

            public RoundResult(int score, int errors, int errorLimit, int itemsToWin, string finishReason)
            {
                Score = score;
                Errors = errors;
                ErrorLimit = errorLimit;
                ItemsToWin = itemsToWin;
                FinishReason = finishReason;
            }
        }

        private readonly struct AttemptPlan
        {
            public readonly ItemGroup Group;
            public readonly ItemGroup ChosenGroup;
            public readonly string ItemName;
            public readonly bool Correct;
            public readonly bool DuringTutorial;
            public readonly int RetryIndex;
            public readonly int PickupCount;
            public readonly float ResponseSeconds;

            public AttemptPlan(
                ItemGroup group,
                ItemGroup chosenGroup,
                string itemName,
                bool correct,
                bool duringTutorial,
                int retryIndex,
                int pickupCount,
                float responseSeconds)
            {
                Group = group;
                ChosenGroup = chosenGroup;
                ItemName = itemName;
                Correct = correct;
                DuringTutorial = duringTutorial;
                RetryIndex = retryIndex;
                PickupCount = pickupCount;
                ResponseSeconds = responseSeconds;
            }
        }
    }
}
