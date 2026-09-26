using System;

namespace ColorSort.Core.Progression
{
    public static class LevelBalancing
    {
        public const float MinTimeLimitSeconds = 30f;
        public const float MaxTimeLimitSeconds = 300f;
        public const float SecondsPerOptimalMove = 6f;

        public static float DefaultTimeLimitSeconds(int optimalMoves)
        {
            if (optimalMoves <= 0)
                throw new ArgumentOutOfRangeException(nameof(optimalMoves), optimalMoves, "Must be positive.");

            float raw = optimalMoves * SecondsPerOptimalMove;
            float rounded = (float)Math.Ceiling(raw / 5f) * 5f;
            return Math.Min(MaxTimeLimitSeconds, Math.Max(MinTimeLimitSeconds, rounded));
        }
    }
}
