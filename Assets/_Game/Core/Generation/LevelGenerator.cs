using System;
using System.Collections.Generic;
using ColorSort.Core.Board;
using ColorSort.Core.Solving;

namespace ColorSort.Core.Generation
{
    public sealed class GeneratorSettings
    {
        public int Colours { get; set; } = 5;
        public int Capacity { get; set; } = 4;
        public int EmptyBottles { get; set; } = 2;

        public int MinOptimalMoves { get; set; } = 1;
        public int MaxOptimalMoves { get; set; } = int.MaxValue;

        public int? MaxInitialRun { get; set; }

        public int MaxAttempts { get; set; } = 200;
        public int SolverStateLimit { get; set; } = 200_000;

        internal int EffectiveMaxInitialRun => MaxInitialRun ?? Math.Max(1, Capacity - 2);

        public void Validate()
        {
            if (Colours < 2)
                throw new ArgumentOutOfRangeException(nameof(Colours), Colours, "Need at least 2 colours.");
            if (Capacity < 2)
                throw new ArgumentOutOfRangeException(nameof(Capacity), Capacity, "Capacity must be at least 2.");
            if (EmptyBottles < 0)
                throw new ArgumentOutOfRangeException(nameof(EmptyBottles), EmptyBottles, "Must not be negative.");
            if (MinOptimalMoves < 1 || MaxOptimalMoves < MinOptimalMoves)
                throw new ArgumentException($"Invalid optimal move range {MinOptimalMoves}..{MaxOptimalMoves}.");
            if (EffectiveMaxInitialRun < 1 || EffectiveMaxInitialRun >= Capacity)
                throw new ArgumentOutOfRangeException(nameof(MaxInitialRun), MaxInitialRun, "Must be between 1 and capacity - 1.");
            if (MaxAttempts < 1)
                throw new ArgumentOutOfRangeException(nameof(MaxAttempts), MaxAttempts, "Must be positive.");
        }

        public override string ToString() =>
            $"{Colours} colours x{Capacity}, {EmptyBottles} empty, optimal {MinOptimalMoves}..{MaxOptimalMoves}";
    }

    public sealed class GeneratedLevel
    {
        internal GeneratedLevel(BoardState board, int optimalMoves, int attempts)
        {
            Board = board;
            OptimalMoves = optimalMoves;
            Attempts = attempts;
        }

        public BoardState Board { get; }
        public int OptimalMoves { get; }

        public int Attempts { get; }
    }

    public static class LevelGenerator
    {
        public static bool TryGenerate(GeneratorSettings settings, Random random, out GeneratedLevel level, IReadOnlyList<byte> colourIds = null)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (random == null)
                throw new ArgumentNullException(nameof(random));
            settings.Validate();
            if (colourIds != null && colourIds.Count < settings.Colours)
                throw new ArgumentException($"Need {settings.Colours} colour ids, got {colourIds.Count}.", nameof(colourIds));

            var units = new byte[settings.Colours * settings.Capacity];

            for (int attempt = 1; attempt <= settings.MaxAttempts; attempt++)
            {
                for (int i = 0; i < units.Length; i++)
                {
                    int colour = i / settings.Capacity;
                    units[i] = colourIds != null ? colourIds[colour] : (byte)colour;
                }
                Shuffle(units, random);

                if (LongestRun(units, settings.Capacity) > settings.EffectiveMaxInitialRun)
                    continue;

                BoardState board = BuildBoard(units, settings);
                SolveResult result = Solver.Solve(board, settings.SolverStateLimit);
                if (result.Status != SolveStatus.Solved)
                    continue;

                int optimal = result.Moves.Count;
                if (optimal < settings.MinOptimalMoves || optimal > settings.MaxOptimalMoves)
                    continue;

                level = new GeneratedLevel(board, optimal, attempt);
                return true;
            }

            level = null;
            return false;
        }

        private static BoardState BuildBoard(byte[] units, GeneratorSettings settings)
        {
            var bottles = new List<Bottle>(settings.Colours + settings.EmptyBottles);
            for (int b = 0; b < settings.Colours; b++)
            {
                var layers = new byte[settings.Capacity];
                Array.Copy(units, b * settings.Capacity, layers, 0, settings.Capacity);
                bottles.Add(new Bottle(settings.Capacity, layers));
            }
            for (int e = 0; e < settings.EmptyBottles; e++)
                bottles.Add(new Bottle(settings.Capacity));
            return new BoardState(bottles);
        }

        private static int LongestRun(byte[] units, int capacity)
        {
            int longest = 0;
            for (int start = 0; start < units.Length; start += capacity)
            {
                int run = 1;
                for (int i = start + 1; i < start + capacity; i++)
                {
                    run = units[i] == units[i - 1] ? run + 1 : 1;
                    longest = Math.Max(longest, run);
                }
                longest = Math.Max(longest, 1);
            }
            return longest;
        }

        private static void Shuffle(byte[] items, Random random)
        {
            for (int i = items.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                byte tmp = items[i];
                items[i] = items[j];
                items[j] = tmp;
            }
        }
    }
}
