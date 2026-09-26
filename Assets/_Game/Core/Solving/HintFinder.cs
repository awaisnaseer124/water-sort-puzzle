using System;
using ColorSort.Core.Board;

namespace ColorSort.Core.Solving
{
    public enum HintKind
    {
        Move,
        DeadEnd,
        AlreadySolved,
        Unknown,
    }

    public readonly struct Hint
    {
        public Hint(HintKind kind, int from = -1, int to = -1)
        {
            Kind = kind;
            From = from;
            To = to;
        }

        public HintKind Kind { get; }
        public int From { get; }
        public int To { get; }

        public override string ToString() => Kind == HintKind.Move ? $"{From}->{To}" : Kind.ToString();
    }

    public static class HintFinder
    {
        public const int DefaultStateLimit = 200_000;

        public static Hint Find(BoardState board, int stateLimit = DefaultStateLimit)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));
            if (board.IsSolved)
                return new Hint(HintKind.AlreadySolved);

            SolveResult result = Solver.Solve(board, stateLimit);
            switch (result.Status)
            {
                case SolveStatus.Solved:
                    SolverMove first = result.Moves[0];
                    return new Hint(HintKind.Move, first.From, first.To);
                case SolveStatus.Unsolvable:
                    return new Hint(HintKind.DeadEnd);
                default:
                    return new Hint(HintKind.Unknown);
            }
        }
    }
}
