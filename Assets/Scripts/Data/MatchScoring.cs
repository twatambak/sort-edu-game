namespace OndeVai.Data
{
    /// <summary>Pure scoring rules shared by the results screen, the history database and the progress report.</summary>
    public static class MatchScoring
    {
        public const int MaxStars = 3;

        /// <summary>
        /// Three stars at 90% accuracy or more, two at 60% or more, otherwise one.
        /// No stars when nothing was sorted.
        /// </summary>
        public static int CalculateStars(int score, int errors)
        {
            if (score <= 0)
                return 0;

            float accuracy = (float)score / (score + errors);
            if (accuracy >= 0.9f)
                return 3;

            return accuracy >= 0.6f ? 2 : 1;
        }
    }
}
