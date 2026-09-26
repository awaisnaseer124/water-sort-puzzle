using System;
using System.Collections.Generic;
using System.Linq;
using ColorSort.Core.Board;

namespace ColorSort.Core.Solving
{
    public sealed class LevelReport
    {
        internal LevelReport(IReadOnlyList<string> errors, IReadOnlyList<string> warnings, SolveResult solve)
        {
            Errors = errors;
            Warnings = warnings;
            Solve = solve;
        }

        public IReadOnlyList<string> Errors { get; }
        public IReadOnlyList<string> Warnings { get; }

        public SolveResult Solve { get; }

        public bool IsPlayable => Errors.Count == 0 && Solve != null && Solve.Status == SolveStatus.Solved;
        public int OptimalMoves => IsPlayable ? Solve.Moves.Count : 0;
    }

    public static class LevelValidator
    {
        public static LevelReport Validate(BoardState board, int stateLimit = Solver.DefaultStateLimit)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));

            var errors = new List<string>();
            var warnings = new List<string>();

            if (board.BottleCount < 2)
                errors.Add($"Needs at least 2 bottles, has {board.BottleCount}.");
            if (board.IsSolved)
                errors.Add("Board is already solved.");

            // Each colour must be able to end up filling whole bottles.
            var capacities = new HashSet<int>(board.Bottles.Select(b => b.Capacity));
            foreach (KeyValuePair<byte, int> colour in board.CountColors().OrderBy(c => c.Key))
            {
                if (!capacities.Contains(colour.Value))
                    warnings.Add($"Colour {colour.Key} has {colour.Value} units; bottle capacities are {string.Join("/", capacities.OrderBy(c => c))}.");
            }

            if (!board.Bottles.Any(b => b.IsEmpty))
                warnings.Add("No empty bottle; the first move must pour onto a matching colour.");

            SolveResult solve = errors.Count == 0 ? Solver.Solve(board, stateLimit) : null;
            if (solve != null && solve.Status == SolveStatus.Unsolvable)
                errors.Add("Unsolvable.");
            else if (solve != null && solve.Status == SolveStatus.LimitReached)
                errors.Add($"Solver gave up after {solve.ExploredStates:N0} states; solvability unknown.");

            return new LevelReport(errors, warnings, solve);
        }
    }
}
