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

    /// <summary>Builds an improving, imperfect play history for a new profile.</summary>
    public static class ProfileHistorySimulator
    {
        private static readonly GameMode[] Modes =
        {
            GameMode.DragAndDrop,
            GameMode.SwipeTwoDirections
        };

        /// <summary>Creates one to ten matches using configured scenarios and supported game modes.</summary>
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
            DateTime localNow = utcNow.ToLocalTime();
            int gameCount = random.Next(1, 11);
            List<RoundResult> results = CreateResults(gameCount, configuration, random);
            int matchTime = Math.Max(15, configuration.MatchTime);
            bool includeToday = localNow >= localNow.Date.AddHours(9).AddSeconds(matchTime + 20);
            List<int> daysAgo = CreatePlayDates(gameCount, includeToday, random);
            List<SortingScenarioDefinition> availableScenarios = new();
            if (definition != null)
            {
                foreach (SortingScenarioDefinition scenario in definition.Scenarios)
                {
                    if (scenario != null)
                        availableScenarios.Add(scenario);
                }
            }

            if (availableScenarios.Count == 0)
                availableScenarios.Add(SortingScenarioDefinition.Default);

            List<SortingScenarioDefinition> scenarioSequence = CreateSequence(availableScenarios, gameCount, random);
            List<GameMode> modeSequence = CreateSequence(Modes, gameCount, random);
            List<SimulatedMatchData> matches = new(gameCount);

            for (int i = 0; i < gameCount; i++)
            {
                SortingScenarioDefinition scenario = scenarioSequence[i];
                GameConfiguration matchConfiguration = CreateMatchConfiguration(configuration, scenario, modeSequence[i]);
                DateTime localDay = localNow.Date.AddDays(-daysAgo[i]);
                matches.Add(CreateMatch(definition, matchConfiguration, results[i], localDay, localNow, matchTime, random));
            }

            return matches;
        }

        private static List<RoundResult> CreateResults(int gameCount, GameConfiguration configuration, Random random)
        {
            List<RoundResult> results = new(gameCount);
            int singleGameScore = random.Next(2, 5);
            int singleGameErrors = random.Next(3, 6);

            for (int i = 0; i < gameCount; i++)
            {
                double progress = gameCount > 1 ? (double)i / (gameCount - 1) : 0;
                int score = gameCount > 1 ? 2 + i : singleGameScore;
                int errors = gameCount > 1
                    ? Math.Max(1, 5 - (int)Math.Round(progress * 4))
                    : singleGameErrors;
                bool won = gameCount >= 3 && i >= gameCount - 2;
                bool reachedErrorLimit = i == 0 && !won;
                string finishReason = won
                    ? GameSessionRecord.ReasonItemsCollected
                    : reachedErrorLimit
                        ? GameSessionRecord.ReasonErrorLimit
                        : GameSessionRecord.ReasonTimeExpired;
                int errorLimit = reachedErrorLimit
                    ? errors
                    : Math.Max(errors + 1, configuration.ErrorLimit);
                int itemsToWin = won ? score : Math.Max(score + 1, configuration.ItemsToWin);

                results.Add(new RoundResult(score, errors, errorLimit, itemsToWin, finishReason));
            }

            return results;
        }

        private static GameConfiguration CreateMatchConfiguration(
            GameConfiguration source,
            SortingScenarioDefinition scenario,
            GameMode mode)
        {
            return new GameConfiguration(
                source.MatchTime,
                source.ErrorLimit,
                source.MaxItemsOnScreen,
                source.ItemsToWin,
                source.DelayBetweenSpawns,
                scenario.GroupA,
                scenario.GroupB)
            {
                Mode = mode,
                ScenarioId = scenario.Id,
                TutorialItemCount = source.TutorialItemCount
            };
        }

        private static List<int> CreatePlayDates(int count, bool includeToday, Random random)
        {
            List<int> availableDates = new(includeToday ? 15 : 14);
            for (int daysAgo = 1; daysAgo <= 14; daysAgo++)
                availableDates.Add(daysAgo);
            if (includeToday)
                availableDates.Add(0);

            Shuffle(availableDates, random);
            List<int> dates = availableDates.GetRange(0, count);
            dates.Sort((left, right) => right.CompareTo(left));
            return dates;
        }

        private static List<T> CreateSequence<T>(IReadOnlyList<T> options, int count, Random random)
        {
            if (options == null || options.Count == 0)
                throw new ArgumentException("At least one option is required.", nameof(options));

            List<T> sequence = new(count);
            while (sequence.Count < count)
            {
                List<T> cycle = new(options);
                Shuffle(cycle, random);

                if (sequence.Count > 0 && cycle.Count > 1 &&
                    EqualityComparer<T>.Default.Equals(cycle[0], sequence[sequence.Count - 1]))
                {
                    for (int i = 1; i < cycle.Count; i++)
                    {
                        if (EqualityComparer<T>.Default.Equals(cycle[i], sequence[sequence.Count - 1]))
                            continue;

                        (cycle[0], cycle[i]) = (cycle[i], cycle[0]);
                        break;
                    }
                }

                int remaining = count - sequence.Count;
                int cycleCount = Math.Min(remaining, cycle.Count);
                for (int i = 0; i < cycleCount; i++)
                    sequence.Add(cycle[i]);
            }

            return sequence;
        }

        private static DateTime CreateFinishTime(DateTime localDay, DateTime localNow, double durationSeconds, Random random)
        {
            DateTime earliestFinish = localDay.AddHours(9).AddSeconds(durationSeconds);
            DateTime latestFinish = localDay.AddHours(20);
            if (localDay.Date == localNow.Date && localNow < latestFinish)
                latestFinish = localNow;

            int availableSeconds = (int)(latestFinish - earliestFinish).TotalSeconds;
            if (availableSeconds < 0)
                throw new InvalidOperationException("The simulated gameplay does not fit within the allowed daily time window.");

            return earliestFinish.AddSeconds(random.Next(availableSeconds + 1));
        }

        private static SimulatedMatchData CreateMatch(
            GameDefinition definition,
            GameConfiguration configuration,
            RoundResult result,
            DateTime localDay,
            DateTime localNow,
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
            DateTime finishedAtLocal = CreateFinishTime(localDay, localNow, durationSeconds, random);
            DateTime finishedAt = finishedAtLocal.ToUniversalTime();
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
