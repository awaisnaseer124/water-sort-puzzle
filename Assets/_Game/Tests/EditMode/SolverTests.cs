using System;
using System.Collections.Generic;
using System.Linq;
using ColorSort.Core.Board;
using ColorSort.Core.Session;
using ColorSort.Core.Solving;
using NUnit.Framework;

namespace ColorSort.Core.Tests
{
    public class SolverTests
    {
        [Test]
        public void Solves_KnownBoard_Optimally()
        {
            SolveResult result = Solver.Solve(TestBoards.Make(4, "RRBB", "BBRR", ""));

            Assert.AreEqual(SolveStatus.Solved, result.Status);
            Assert.AreEqual(3, result.Moves.Count);
        }

        [Test]
        public void AlreadySolved_ReturnsNoMoves()
        {
            SolveResult result = Solver.Solve(TestBoards.Make(4, "RRRR", ""));

            Assert.AreEqual(SolveStatus.Solved, result.Status);
            Assert.AreEqual(0, result.Moves.Count);
        }

        [Test]
        public void NoLegalMoves_IsUnsolvable()
        {
            SolveResult result = Solver.Solve(TestBoards.Make(2, "RB", "BR"));

            Assert.AreEqual(SolveStatus.Unsolvable, result.Status);
            Assert.IsEmpty(result.Moves);
        }

        [Test]
        public void StateLimit_StopsSearch()
        {
            SolveResult result = Solver.Solve(TestBoards.Make(4, "RGBY", "GBYR", "BYRG", "YRGB", "", ""), stateLimit: 1);

            Assert.AreEqual(SolveStatus.LimitReached, result.Status);
        }

        [Test]
        public void MixedCapacities_Solve()
        {
            var board = new BoardState(new[]
            {
                TestBoards.Bottle(4, "RRRB"),
                TestBoards.Bottle(3, "BBR"),
                TestBoards.Bottle(4, ""),
            });

            SolveResult result = Solver.Solve(board);

            Assert.AreEqual(SolveStatus.Solved, result.Status);
            AssertSolutionReplays(board, result);
        }

        [Test]
        public void CanSolveFromMidGameSnapshot()
        {
            var session = new GameSession(TestBoards.Make(4, "RGBR", "GBRG", "BRGB", "", ""));
            session.TryPour(0, 3);

            SolveResult result = Solver.Solve(session.SnapshotBoard());

            Assert.AreEqual(SolveStatus.Solved, result.Status);
            foreach (SolverMove move in result.Moves)
                Assert.AreEqual(PourCheck.Allowed, session.TryPour(move.From, move.To));
            Assert.AreEqual(SessionStatus.Won, session.Status);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void RandomBoards_SolutionsReplayAndMatchBruteForce(int seed)
        {
            var random = new Random(seed);
            for (int i = 0; i < 10; i++)
            {
                BoardState board = RandomBoard(random, colours: 3, capacity: 3, empties: 2);

                SolveResult result = Solver.Solve(board);
                int? bruteForce = BreadthFirstOptimum(board);

                Assert.AreEqual(bruteForce.HasValue, result.Status == SolveStatus.Solved, board.ToString());
                if (!bruteForce.HasValue)
                    continue;

                Assert.AreEqual(bruteForce.Value, result.Moves.Count, "Not optimal for " + board);
                AssertSolutionReplays(board, result);
            }
        }

        [Test]
        public void Fingerprint_IgnoresBottleOrder()
        {
            string a = TestBoards.Make(4, "RB", "BR", "").Fingerprint();
            string b = TestBoards.Make(4, "", "BR", "RB").Fingerprint();
            string c = TestBoards.Make(4, "RB", "RB", "").Fingerprint();

            Assert.AreEqual(a, b);
            Assert.AreNotEqual(a, c);
        }

        private static void AssertSolutionReplays(BoardState board, SolveResult result)
        {
            var session = new GameSession(board);
            foreach (SolverMove move in result.Moves)
                Assert.AreEqual(PourCheck.Allowed, session.TryPour(move.From, move.To), $"Move {move} on {board}");
            Assert.AreEqual(SessionStatus.Won, session.Status);
        }

        private static BoardState RandomBoard(Random random, int colours, int capacity, int empties)
        {
            while (true)
            {
                List<char> units = Enumerable.Range(0, colours)
                    .SelectMany(c => Enumerable.Repeat((char)('A' + c), capacity))
                    .OrderBy(_ => random.Next())
                    .ToList();

                var bottles = new List<string>();
                for (int b = 0; b < colours; b++)
                    bottles.Add(new string(units.Skip(b * capacity).Take(capacity).ToArray()));
                for (int e = 0; e < empties; e++)
                    bottles.Add("");

                BoardState board = TestBoards.Make(capacity, bottles.ToArray());
                if (!board.IsSolved)
                    return board;
            }
        }

        private static int? BreadthFirstOptimum(BoardState start)
        {
            var seen = new HashSet<string> { start.Fingerprint() };
            var frontier = new List<BoardState> { start };

            for (int depth = 0; frontier.Count > 0; depth++)
            {
                var next = new List<BoardState>();
                foreach (BoardState board in frontier)
                {
                    if (board.IsSolved)
                        return depth;

                    for (int from = 0; from < board.BottleCount; from++)
                    for (int to = 0; to < board.BottleCount; to++)
                    {
                        BoardState child = board.Clone();
                        if (child.TryPour(from, to, out _) == PourCheck.Allowed && seen.Add(child.Fingerprint()))
                            next.Add(child);
                    }
                }
                frontier = next;
            }
            return null;
        }
    }
}
