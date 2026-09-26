using System.Linq;
using ColorSort.Game.Board;
using NUnit.Framework;
using UnityEngine;

namespace ColorSort.Game.Tests
{
    public class BoardLayoutTests
    {
        private static readonly Vector2 Spacing = new Vector2(130f, 533f);

        [TestCase(0, new int[0])]
        [TestCase(3, new[] { 3 })]
        [TestCase(5, new[] { 5 })]
        [TestCase(6, new[] { 3, 3 })]
        [TestCase(7, new[] { 4, 3 })]
        [TestCase(9, new[] { 5, 4 })]
        [TestCase(10, new[] { 5, 5 })]
        [TestCase(11, new[] { 4, 4, 3 })]
        public void Arrange_BalancesRows(int count, int[] expectedRowSizes)
        {
            Vector2[] positions = BoardLayout.Arrange(count, 5, Spacing);

            int[] rowSizes = positions
                .GroupBy(p => p.y)
                .OrderByDescending(g => g.Key)
                .Select(g => g.Count())
                .ToArray();
            CollectionAssert.AreEqual(expectedRowSizes, rowSizes);
        }

        [TestCase(1)]
        [TestCase(4)]
        [TestCase(9)]
        public void Arrange_EachRowIsCentred(int count)
        {
            foreach (var row in BoardLayout.Arrange(count, 5, Spacing).GroupBy(p => p.y))
                Assert.AreEqual(0f, row.Sum(p => p.x), 0.001f);
        }

        [Test]
        public void Arrange_RowsAreCentredVertically_AndUseSpacing()
        {
            Vector2[] positions = BoardLayout.Arrange(9, 5, Spacing);

            Assert.AreEqual(Spacing.y / 2f, positions[0].y, 0.001f);
            Assert.AreEqual(-Spacing.y / 2f, positions[8].y, 0.001f);
            Assert.AreEqual(Spacing.x, positions[1].x - positions[0].x, 0.001f);
        }

        [Test]
        public void Arrange_KeepsIndexOrder_LeftToRightTopToBottom()
        {
            Vector2[] positions = BoardLayout.Arrange(9, 5, Spacing);

            for (int i = 1; i < 5; i++)
                Assert.Greater(positions[i].x, positions[i - 1].x);
            Assert.Less(positions[5].y, positions[4].y);
        }

        [Test]
        public void FitScale_ShrinksOnlyWhenTooBig()
        {
            var bottle = new Vector2(120f, 381f);

            Assert.AreEqual(1f, BoardLayout.FitScale(9, 5, Spacing, bottle, new Vector2(1000f, 1300f)));
            Assert.AreEqual(0.5f, BoardLayout.FitScale(5, 5, Spacing, bottle, new Vector2(320f, 1300f)), 0.001f, "too wide");
            Assert.AreEqual(1300f / 1447f, BoardLayout.FitScale(12, 5, Spacing, bottle, new Vector2(1000f, 1300f)), 0.001f, "three rows too tall");
        }
    
        private static readonly Vector2 Bottle = new Vector2(120f, 381f);

        [Test]
        public void BestPerRow_TwelveBottlesOnShortScreen_UsesTwoRows()
        {
            var available = new Vector2(760f, 1050f);

            int perRow = BoardLayout.BestPerRow(12, 7, Spacing, Bottle, available);

            Assert.AreEqual(6, perRow);
            Assert.Greater(BoardLayout.FitScale(12, perRow, Spacing, Bottle, available),
                BoardLayout.FitScale(12, 5, Spacing, Bottle, available));
        }

        [Test]
        public void BestPerRow_WhenEverythingFits_UsesFewestBalancedRows()
        {
            int perRow = BoardLayout.BestPerRow(9, 7, Spacing, Bottle, new Vector2(760f, 1400f));

            Assert.AreEqual(5, perRow);
            Assert.AreEqual(2, BoardLayout.Arrange(9, perRow, Spacing).Select(p => p.y).Distinct().Count());
        }

        [Test]
        public void BestPerRow_NeverExceedsBottleCount()
        {
            Assert.AreEqual(3, BoardLayout.BestPerRow(3, 7, Spacing, Bottle, new Vector2(760f, 1400f)));
        }
    }
}
