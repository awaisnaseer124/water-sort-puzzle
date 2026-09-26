using System;

namespace ColorSort.Core.Progression
{
    public readonly struct StarThresholds
    {
        public const int MaxStars = 3;

        public StarThresholds(int threeStarMaxMoves, int twoStarMaxMoves)
        {
            if (threeStarMaxMoves <= 0)
                throw new ArgumentOutOfRangeException(nameof(threeStarMaxMoves), threeStarMaxMoves, "Must be positive.");
            if (twoStarMaxMoves < threeStarMaxMoves)
                throw new ArgumentException("Two-star limit cannot be below the three-star limit.", nameof(twoStarMaxMoves));

            ThreeStarMaxMoves = threeStarMaxMoves;
            TwoStarMaxMoves = twoStarMaxMoves;
        }

        public int ThreeStarMaxMoves { get; }
        public int TwoStarMaxMoves { get; }

        public int Rate(int moves)
        {
            if (moves <= ThreeStarMaxMoves)
                return 3;
            if (moves <= TwoStarMaxMoves)
                return 2;
            return 1;
        }

        public static StarThresholds FromOptimal(int optimalMoves)
        {
            if (optimalMoves <= 0)
                throw new ArgumentOutOfRangeException(nameof(optimalMoves), optimalMoves, "Must be positive.");

            int three = optimalMoves + Math.Max(1, optimalMoves / 10);
            int two = three + Math.Max(2, optimalMoves / 2);
            return new StarThresholds(three, two);
        }

        public override string ToString() => $"3*<={ThreeStarMaxMoves} 2*<={TwoStarMaxMoves}";
    }
}
