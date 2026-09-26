using System;
using ColorSort.Core.Board;
using NUnit.Framework;

namespace ColorSort.Core.Tests
{
    public class BottleTests
    {
        [Test]
        public void Constructor_RejectsNonPositiveCapacity()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Bottle(0));
        }

        [Test]
        public void Constructor_RejectsMoreLayersThanCapacity()
        {
            Assert.Throws<ArgumentException>(() => TestBoards.Bottle(3, "RRRR"));
        }

        [Test]
        public void Constructor_CopiesLayers()
        {
            var layers = new byte[] { 1, 2 };
            var bottle = new Bottle(4, layers);
            layers[0] = 9;

            Assert.AreEqual(1, bottle[0]);
        }

        [TestCase("", 0)]
        [TestCase("R", 1)]
        [TestCase("BR", 1)]
        [TestCase("BRR", 2)]
        [TestCase("RRRR", 4)]
        [TestCase("RBRR", 2)]
        public void TopRunLength_CountsTopSameColourLayers(string layers, int expected)
        {
            Assert.AreEqual(expected, TestBoards.Bottle(4, layers).TopRunLength);
        }

        [TestCase("", false, true)]
        [TestCase("RRR", false, false)]
        [TestCase("RRRB", false, false)]
        [TestCase("RRRR", true, true)]
        public void CompleteAndSettled(string layers, bool complete, bool settled)
        {
            Bottle bottle = TestBoards.Bottle(4, layers);

            Assert.AreEqual(complete, bottle.IsComplete, nameof(Bottle.IsComplete));
            Assert.AreEqual(settled, bottle.IsSettled, nameof(Bottle.IsSettled));
        }

        [Test]
        public void CapacityThree_CompletesAtThree()
        {
            Assert.IsTrue(TestBoards.Bottle(3, "GGG").IsComplete);
        }

        [Test]
        public void SpaceProperties()
        {
            Bottle bottle = TestBoards.Bottle(4, "RB");

            Assert.AreEqual(4, bottle.Capacity);
            Assert.AreEqual(2, bottle.Count);
            Assert.AreEqual(2, bottle.FreeSpace);
            Assert.IsFalse(bottle.IsEmpty);
            Assert.IsFalse(bottle.IsFull);
            Assert.AreEqual((byte)'B', bottle.TopColor);
        }

        [Test]
        public void TopColor_ThrowsWhenEmpty()
        {
            Assert.Throws<InvalidOperationException>(() => { byte _ = new Bottle(4).TopColor; });
        }

        [Test]
        public void Indexer_ThrowsOutsideFilledRange()
        {
            Bottle bottle = TestBoards.Bottle(4, "RB");

            Assert.Throws<ArgumentOutOfRangeException>(() => { byte _ = bottle[2]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { byte _ = bottle[-1]; });
        }

        [Test]
        public void Clone_CopiesContentAndCapacity()
        {
            Bottle original = TestBoards.Bottle(4, "RB");
            Bottle clone = original.Clone();

            Assert.AreNotSame(original, clone);
            Assert.AreEqual("RB", TestBoards.Describe(clone));
            Assert.AreEqual(4, clone.Capacity);
        }
    }
}
