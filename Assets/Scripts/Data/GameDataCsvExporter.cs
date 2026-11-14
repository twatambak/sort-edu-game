using System;
using System.Globalization;
using System.IO;
using OndeVai.Gameplay;

namespace OndeVai.Data
{
    /// <summary>Writes saved profiles, matches and attempts as one RFC 4180 CSV document.</summary>
    public static class GameDataCsvExporter
    {
        private enum Column
        {
            RecordType,
            ProfileId,
            ProfileName,
            UserCode,
            NameHidden,
            Age,
            AvatarIndex,
            ProfileCreatedAtUtc,
            SessionId,
            ChildProfileId,
            ChildAge,
            StartedAtUtc,
            FinishedAtUtc,
            DurationSeconds,
            MatchTimeSeconds,
            ErrorLimit,
            MaxItemsOnScreen,
            ItemsToWin,
            DelayBetweenSpawnsSeconds,
            ItemGroupA,
            ItemGroupB,
            Score,
            Errors,
            Won,
            FinishReason,
            GameMode,
            ScenarioId,
            TutorialItemCount,
            Stars,
            BestStreak,
            ActiveSeconds,
            PauseCount,
            TutorialErrors,
            AverageResponseSeconds,
            AppVersion,
            AttemptId,
            AttemptIndex,
            RetryIndex,
            ItemGroup,
            ChosenGroup,
            ItemName,
            Correct,
            ResponseSeconds,
            DuringTutorial,
            PickupCount,
            SecondsIntoMatch,
            OccurredAtUtc,
            Count
        }

        private static readonly string[] Headers =
        {
            "record_type",
            "profile_id",
            "profile_name",
            "user_code",
            "name_hidden",
            "age",
            "avatar_index",
            "profile_created_at_utc",
            "session_id",
            "child_profile_id",
            "child_age",
            "started_at_utc",
            "finished_at_utc",
            "duration_seconds",
            "match_time_seconds",
            "error_limit",
            "max_items_on_screen",
            "items_to_win",
            "delay_between_spawns_seconds",
            "item_group_a",
            "item_group_b",
            "score",
            "errors",
            "won",
            "finish_reason",
            "game_mode",
            "scenario_id",
            "tutorial_item_count",
            "stars",
            "best_streak",
            "active_seconds",
            "pause_count",
            "tutorial_errors",
            "average_response_seconds",
            "app_version",
            "attempt_id",
            "attempt_index",
            "retry_index",
            "item_group",
            "chosen_group",
            "item_name",
            "correct",
            "response_seconds",
            "during_tutorial",
            "pickup_count",
            "seconds_into_match",
            "occurred_at_utc"
        };

        /// <summary>Writes every stored profile, match and attempt, including guest matches.</summary>
        public static void Write(GameHistoryDatabase database, TextWriter writer)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            if (Headers.Length != (int)Column.Count)
                throw new InvalidOperationException("The CSV header does not match the record columns.");

            WriteRow(writer, Headers);

            foreach (ChildProfileRecord profile in database.GetChildProfiles())
            {
                string[] row = CreateRow("profile");
                Set(row, Column.ProfileId, Number(profile.Id));
                Set(row, Column.ProfileName, profile.DisplayName);
                Set(row, Column.UserCode, profile.UserCode);
                Set(row, Column.NameHidden, Boolean(profile.HideName));
                Set(row, Column.Age, Number(profile.Age));
                Set(row, Column.AvatarIndex, Number(profile.AvatarIndex));
                Set(row, Column.ProfileCreatedAtUtc, UtcTimestamp(profile.CreatedAtUtcTicks));
                WriteRow(writer, row);
            }

            foreach (GameSessionRecord session in database.GetAllSessionsForExport())
            {
                string[] row = CreateRow("session");
                Set(row, Column.SessionId, Number(session.Id));
                Set(row, Column.ChildProfileId, Number(session.ChildProfileId));
                Set(row, Column.ChildAge, Number(session.ChildAge));
                Set(row, Column.StartedAtUtc, UtcTimestamp(session.StartedAtUtcTicks));
                Set(row, Column.FinishedAtUtc, UtcTimestamp(session.FinishedAtUtcTicks));
                Set(row, Column.DurationSeconds, Number(session.DurationSeconds));
                Set(row, Column.MatchTimeSeconds, Number(session.MatchTimeSeconds));
                Set(row, Column.ErrorLimit, Number(session.ErrorLimit));
                Set(row, Column.MaxItemsOnScreen, Number(session.MaxItemsOnScreen));
                Set(row, Column.ItemsToWin, Number(session.ItemsToWin));
                Set(row, Column.DelayBetweenSpawnsSeconds, Number(session.DelayBetweenSpawnsSeconds));
                Set(row, Column.ItemGroupA, ((ItemGroup)session.ItemGroupA).ToString());
                Set(row, Column.ItemGroupB, ((ItemGroup)session.ItemGroupB).ToString());
                Set(row, Column.Score, Number(session.Score));
                Set(row, Column.Errors, Number(session.Errors));
                Set(row, Column.Won, Boolean(session.Won));
                Set(row, Column.FinishReason, session.FinishReason);
                Set(row, Column.GameMode, ((GameMode)session.GameMode).ToString());
                Set(row, Column.ScenarioId, session.ScenarioId);
                Set(row, Column.TutorialItemCount, Number(session.TutorialItemCount));
                Set(row, Column.Stars, Number(session.Stars));
                Set(row, Column.BestStreak, Number(session.BestStreak));
                Set(row, Column.ActiveSeconds, Number(session.ActiveSeconds));
                Set(row, Column.PauseCount, Number(session.PauseCount));
                Set(row, Column.TutorialErrors, Number(session.TutorialErrors));
                Set(row, Column.AverageResponseSeconds, Number(session.AverageResponseSeconds));
                Set(row, Column.AppVersion, session.AppVersion);
                WriteRow(writer, row);
            }

            foreach (ItemAttemptRecord attempt in database.GetAllAttemptsForExport())
            {
                string[] row = CreateRow("attempt");
                Set(row, Column.SessionId, Number(attempt.SessionId));
                Set(row, Column.ChildProfileId, Number(attempt.ChildProfileId));
                Set(row, Column.AttemptId, Number(attempt.Id));
                Set(row, Column.AttemptIndex, Number(attempt.AttemptIndex));
                Set(row, Column.RetryIndex, Number(attempt.RetryIndex));
                Set(row, Column.ItemGroup, ((ItemGroup)attempt.ItemGroup).ToString());
                Set(row, Column.ChosenGroup, ((ItemGroup)attempt.ChosenGroup).ToString());
                Set(row, Column.ItemName, attempt.ItemName);
                Set(row, Column.Correct, Boolean(attempt.Correct));
                Set(row, Column.ResponseSeconds, Number(attempt.ResponseSeconds));
                Set(row, Column.DuringTutorial, Boolean(attempt.DuringTutorial));
                Set(row, Column.PickupCount, Number(attempt.PickupCount));
                Set(row, Column.SecondsIntoMatch, Number(attempt.SecondsIntoMatch));
                Set(row, Column.OccurredAtUtc, UtcTimestamp(attempt.OccurredAtUtcTicks));
                WriteRow(writer, row);
            }
        }

        private static string[] CreateRow(string recordType)
        {
            string[] row = new string[Headers.Length];
            row[(int)Column.RecordType] = recordType;
            return row;
        }

        private static void Set(string[] row, Column column, string value)
        {
            row[(int)column] = value;
        }

        private static void WriteRow(TextWriter writer, string[] fields)
        {
            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0)
                    writer.Write(',');

                WriteEscapedField(writer, fields[i]);
            }

            writer.Write("\r\n");
        }

        private static void WriteEscapedField(TextWriter writer, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            bool quote = false;
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (character == ',' || character == '"' || character == '\r' || character == '\n')
                {
                    quote = true;
                    break;
                }
            }

            if (!quote)
            {
                writer.Write(value);
                return;
            }

            writer.Write('"');
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (character == '"')
                    writer.Write("\"\"");
                else
                    writer.Write(character);
            }

            writer.Write('"');
        }

        private static string UtcTimestamp(long ticks)
        {
            if (ticks <= DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
                return string.Empty;

            return new DateTime(ticks, DateTimeKind.Utc).ToString("O", CultureInfo.InvariantCulture);
        }

        private static string Number(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string Number(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string Number(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Number(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Boolean(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
