using System;

namespace ColorSort.Core.Session
{
    public sealed class TimeLimitRule : ILoseRule
    {
        public TimeLimitRule(float limitSeconds)
        {
            if (limitSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(limitSeconds), limitSeconds, "Time limit must be positive.");
            LimitSeconds = limitSeconds;
        }

        public float LimitSeconds { get; private set; }

        public float RemainingSeconds(GameSession session) => Math.Max(0f, LimitSeconds - session.ElapsedSeconds);

        public void Extend(float seconds)
        {
            if (seconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "Extension must be positive.");
            LimitSeconds += seconds;
        }

        public bool IsLost(GameSession session) => session.ElapsedSeconds >= LimitSeconds;
    }
}
