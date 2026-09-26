using System;
using ColorSort.Core.Progression;
using NUnit.Framework;

namespace ColorSort.Core.Tests
{
    public class StarThresholdsTests
    {
        [TestCase(1, 3)]
        [TestCase(10, 3)]
        [TestCase(11, 2)]
        [TestCase(15, 2)]
        [TestCase(16, 1)]
        [TestCase(500, 1)]
        public void Rate_UsesInclusiveLimits(int moves, int expectedStars)
        {
            Assert.AreEqual(expectedStars, new StarThresholds(10, 15).Rate(moves));
        }

        [Test]
        public void Constructor_RejectsInvertedLimits()
        {
            Assert.Throws<ArgumentException>(() => new StarThresholds(10, 9));
        }

        [Test]
        public void Constructor_RejectsNonPositiveLimit()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StarThresholds(0, 5));
        }

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(14)]
        [TestCase(40)]
        public void FromOptimal_OptimalPlayEarnsThreeStars_AndLimitsIncrease(int optimal)
        {
            StarThresholds thresholds = StarThresholds.FromOptimal(optimal);

            Assert.AreEqual(3, thresholds.Rate(optimal));
            Assert.Greater(thresholds.ThreeStarMaxMoves, optimal);
            Assert.Greater(thresholds.TwoStarMaxMoves, thresholds.ThreeStarMaxMoves);
        }
    }
}
