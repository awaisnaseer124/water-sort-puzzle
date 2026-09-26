using System;
using ColorSort.Core.Board;
using NUnit.Framework;

namespace ColorSort.Core.Tests
{
    public class BoardStateTests
    {
        [TestCase(0, 0, PourCheck.SameBottle)]
        [TestCase(-1, 0, PourCheck.InvalidIndex)]
        [TestCase(0, 9, PourCheck.InvalidIndex)]
        [TestCase(4, 0, PourCheck.SourceEmpty)]
        [TestCase(3, 4, PourCheck.SourceComplete)]
        [TestCase(0, 2, PourCheck.TargetFull)]
        [TestCase(0, 1, PourCheck.ColorMismatch)]
        [TestCase(1, 0, PourCheck.ColorMismatch)]
        [TestCase(0, 4, PourCheck.Allowed)]
        [TestCase(5, 0, PourCheck.Allowed)]
        public void CanPour_ReportsReason(int from, int to, PourCheck expected)
        {
            BoardState board = TestBoards.Make(4,
                "RB",   // 0
                "BR",   // 1
                "GBGR", // 2 full, mixed
                "YYYY", // 3 complete
                "",     // 4 empty
                "GB");  // 5

            Assert.AreEqual(expected, board.CanPour(from, to));
        }

        [Test]
        public void TryPour_IntoEmpty_MovesWholeTopRun()
        {
            BoardState board = TestBoards.Make(4, "BRRR", "");

            Assert.AreEqual(PourCheck.Allowed, board.TryPour(0, 1, out Pour pour));

            CollectionAssert.AreEqual(new[] { "B", "RRR" }, TestBoards.Describe(board));
            Assert.AreEqual(new Pour(0, 1, (byte)'R', 3).ToString(), pour.ToString());
        }

        [Test]
        public void TryPour_OntoSameColour_IsLimitedByFreeSpace()
        {
            BoardState board = TestBoards.Make(4, "BRRR", "GGR");

            board.TryPour(0, 1, out Pour pour);

            Assert.AreEqual(1, pour.Amount);
            CollectionAssert.AreEqual(new[] { "BRR", "GGRR" }, TestBoards.Describe(board));
        }

        [Test]
        public void TryPour_Rejected_LeavesBoardUntouched()
        {
            BoardState board = TestBoards.Make(4, "RB", "BR");

            Assert.AreEqual(PourCheck.ColorMismatch, board.TryPour(0, 1, out Pour pour));

            Assert.AreEqual(default(Pour).ToString(), pour.ToString());
            CollectionAssert.AreEqual(new[] { "RB", "BR" }, TestBoards.Describe(board));
        }

        [Test]
        public void TryPour_BetweenDifferentCapacities()
        {
            var board = new BoardState(new[]
            {
                TestBoards.Bottle(4, "RRRB"),
                TestBoards.Bottle(3, "B"),
            });

            board.TryPour(0, 1, out _);

            CollectionAssert.AreEqual(new[] { "RRR", "BB" }, TestBoards.Describe(board));
        }

        [TestCase(new[] { "RRRR", "BBBB", "" }, true)]
        [TestCase(new[] { "", "" }, true)]
        [TestCase(new[] { "RRRR", "BBB", "B" }, false)]
        [TestCase(new[] { "RRRB", "BBBR" }, false)]
        public void IsSolved_EveryBottleEmptyOrComplete(string[] bottles, bool expected)
        {
            Assert.AreEqual(expected, TestBoards.Make(4, bottles).IsSolved);
        }

        [Test]
        public void Revert_RestoresPreviousState()
        {
            BoardState board = TestBoards.Make(4, "BRRR", "GR", "");
            board.TryPour(0, 1, out Pour first);
            board.TryPour(0, 2, out Pour second);

            board.Revert(second);
            board.Revert(first);

            CollectionAssert.AreEqual(new[] { "BRRR", "GR", "" }, TestBoards.Describe(board));
        }

        [Test]
        public void Revert_RejectsPourThatIsNotTheLatestChange()
        {
            BoardState board = TestBoards.Make(4, "BR", "", "");
            board.TryPour(0, 1, out Pour first);
            board.TryPour(1, 2, out _);

            Assert.Throws<InvalidOperationException>(() => board.Revert(first));
        }

        [Test]
        public void Revert_RejectsForeignIndices()
        {
            BoardState board = TestBoards.Make(4, "R", "");

            Assert.Throws<ArgumentException>(() => board.Revert(new Pour(0, 7, (byte)'R', 1)));
        }

        [Test]
        public void Constructor_DoesNotAliasCallerBottles()
        {
            Bottle source = TestBoards.Bottle(4, "R");
            var board = new BoardState(new[] { source, new Bottle(4) });

            board.TryPour(0, 1, out _);

            Assert.AreEqual(1, source.Count);
        }

        [Test]
        public void Clone_IsIndependent()
        {
            BoardState board = TestBoards.Make(4, "R", "");
            BoardState clone = board.Clone();

            board.TryPour(0, 1, out _);

            CollectionAssert.AreEqual(new[] { "R", "" }, TestBoards.Describe(clone));
        }

        [Test]
        public void AddEmptyBottle_AppendsAndReturnsIndex()
        {
            BoardState board = TestBoards.Make(4, "RB", "BR");

            int index = board.AddEmptyBottle(4);

            Assert.AreEqual(2, index);
            Assert.AreEqual(PourCheck.Allowed, board.CanPour(0, index));
        }

        [Test]
        public void CountColors_TotalsUnitsPerColour()
        {
            var counts = TestBoards.Make(4, "RRB", "BR", "").CountColors();

            Assert.AreEqual(3, counts[(byte)'R']);
            Assert.AreEqual(2, counts[(byte)'B']);
            Assert.AreEqual(2, counts.Count);
        }
    }
}
