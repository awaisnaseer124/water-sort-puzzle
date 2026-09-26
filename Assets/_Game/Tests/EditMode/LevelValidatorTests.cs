using System;
using System.Linq;
using ColorSort.Core.Progression;
using ColorSort.Core.Solving;
using NUnit.Framework;

namespace ColorSort.Core.Tests
{
    public class LevelValidatorTests
    {
        [Test]
        public void PlayableLevel_ReportsOptimalMoves()
        {
            LevelReport report = LevelValidator.Validate(TestBoards.Make(4, "RRBB", "BBRR", ""));

            Assert.IsTrue(report.IsPlayable);
            Assert.AreEqual(3, report.OptimalMoves);
            Assert.IsEmpty(report.Errors);
            Assert.IsEmpty(report.Warnings);
        }

        [Test]
        public void Unsolvable_IsAnError()
        {
            LevelReport report = LevelValidator.Validate(TestBoards.Make(2, "RB", "BR"));

            Assert.IsFalse(report.IsPlayable);
            Assert.That(report.Errors, Has.Some.Contains("Unsolvable"));
            Assert.That(report.Warnings, Has.Some.Contains("No empty bottle"));
        }

        [Test]
        public void AlreadySolved_IsAnError_AndSkipsSolver()
        {
            LevelReport report = LevelValidator.Validate(TestBoards.Make(4, "RRRR", ""));

            Assert.IsFalse(report.IsPlayable);
            Assert.IsNull(report.Solve);
            Assert.That(report.Errors, Has.Some.Contains("already solved"));
        }

        [Test]
        public void TooFewBottles_IsAnError()
        {
            LevelReport report = LevelValidator.Validate(TestBoards.Make(4, "RB"));

            Assert.That(report.Errors, Has.Some.Contains("at least 2 bottles"));
        }

        [Test]
        public void ColourCountNotMatchingCapacity_IsAWarning()
        {
            LevelReport report = LevelValidator.Validate(TestBoards.Make(4, "RRRB", "BBB", ""));

            Assert.That(report.Warnings.Single(), Does.Contain("Colour"));
        }

        [TestCase(1, 30f)]
        [TestCase(7, 45f)]
        [TestCase(20, 120f)]
        [TestCase(100, 300f)]
        public void DefaultTimeLimit_RoundsAndClamps(int optimal, float expected)
        {
            Assert.AreEqual(expected, LevelBalancing.DefaultTimeLimitSeconds(optimal));
        }

        [Test]
        public void DefaultTimeLimit_RejectsNonPositive()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => LevelBalancing.DefaultTimeLimitSeconds(0));
        }
    }
}
