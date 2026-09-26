using ColorSort.Core.Board;
using ColorSort.Core.Session;
using ColorSort.Core.Solving;
using NUnit.Framework;

namespace ColorSort.Core.Tests
{
    public class HintFinderTests
    {
        [Test]
        public void Hint_IsALegalFirstStepOfAnOptimalSolution()
        {
            BoardState board = TestBoards.Make(4, "RRBB", "BBRR", "");
            int optimal = Solver.Solve(board).Moves.Count;

            Hint hint = HintFinder.Find(board);

            Assert.AreEqual(HintKind.Move, hint.Kind);
            Assert.AreEqual(PourCheck.Allowed, board.CanPour(hint.From, hint.To));

            BoardState after = board.Clone();
            after.TryPour(hint.From, hint.To, out _);
            Assert.AreEqual(optimal - 1, Solver.Solve(after).Moves.Count, "Following the hint keeps the optimum");
        }

        [Test]
        public void FollowingHints_SolvesTheLevel()
        {
            var session = new GameSession(TestBoards.Make(4, "RGBR", "GBRG", "BRGB", "", ""));

            for (int guard = 0; guard < 50 && session.Status == SessionStatus.Playing; guard++)
            {
                Hint hint = HintFinder.Find(session.SnapshotBoard());
                Assert.AreEqual(HintKind.Move, hint.Kind);
                Assert.AreEqual(PourCheck.Allowed, session.TryPour(hint.From, hint.To));
            }

            Assert.AreEqual(SessionStatus.Won, session.Status);
        }

        [Test]
        public void DeadEnd_IsReported()
        {
            // Two full mixed bottles and nowhere to pour.
            Assert.AreEqual(HintKind.DeadEnd, HintFinder.Find(TestBoards.Make(2, "RB", "BR")).Kind);
        }

        [Test]
        public void SolvedBoard_ReportsAlreadySolved()
        {
            Assert.AreEqual(HintKind.AlreadySolved, HintFinder.Find(TestBoards.Make(4, "RRRR", "")).Kind);
        }

        [Test]
        public void TinyStateLimit_ReportsUnknown()
        {
            BoardState board = TestBoards.Make(4, "RGBY", "GBYR", "BYRG", "YRGB", "", "");

            Assert.AreEqual(HintKind.Unknown, HintFinder.Find(board, stateLimit: 1).Kind);
        }
    }
}
