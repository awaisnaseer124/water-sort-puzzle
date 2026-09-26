using System;
using System.Linq;
using ColorSort.Core.Board;
using ColorSort.Core.Generation;
using ColorSort.Core.Solving;
using NUnit.Framework;

namespace ColorSort.Core.Tests
{
    public class LevelGeneratorTests
    {
        private static GeneratorSettings Settings(int colours = 5) => new GeneratorSettings
        {
            Colours = colours,
            Capacity = 4,
            EmptyBottles = 2,
        };

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void SameSeed_SameLevel(int seed)
        {
            LevelGenerator.TryGenerate(Settings(), new Random(seed), out GeneratedLevel a);
            LevelGenerator.TryGenerate(Settings(), new Random(seed), out GeneratedLevel b);

            Assert.AreEqual(a.Board.Fingerprint(), b.Board.Fingerprint());
            Assert.AreEqual(a.OptimalMoves, b.OptimalMoves);
        }

        [Test]
        public void DifferentSeeds_Differ()
        {
            LevelGenerator.TryGenerate(Settings(), new Random(1), out GeneratedLevel a);
            LevelGenerator.TryGenerate(Settings(), new Random(2), out GeneratedLevel b);

            Assert.AreNotEqual(a.Board.Fingerprint(), b.Board.Fingerprint());
        }

        [TestCase(3)]
        [TestCase(5)]
        [TestCase(7)]
        public void Layout_HasFullColourBottlesPlusEmpties_AndNoCompleteBottle(int colours)
        {
            Assert.IsTrue(LevelGenerator.TryGenerate(Settings(colours), new Random(colours), out GeneratedLevel level));
            BoardState board = level.Board;

            Assert.AreEqual(colours + 2, board.BottleCount);
            Assert.AreEqual(2, board.Bottles.Count(b => b.IsEmpty));
            Assert.IsTrue(board.Bottles.Where(b => !b.IsEmpty).All(b => b.IsFull && !b.IsComplete));
            Assert.AreEqual(colours, board.CountColors().Count);
            Assert.IsTrue(board.CountColors().Values.All(n => n == 4));
        }

        [Test]
        public void Layout_RespectsMaxInitialRun()
        {
            var settings = Settings(6);
            settings.MaxInitialRun = 1;

            for (int seed = 0; seed < 10; seed++)
            {
                Assert.IsTrue(LevelGenerator.TryGenerate(settings, new Random(seed), out GeneratedLevel level));
                foreach (Bottle bottle in level.Board.Bottles.Where(b => !b.IsEmpty))
                {
                    for (int i = 1; i < bottle.Count; i++)
                        Assert.AreNotEqual(bottle[i - 1], bottle[i], bottle.ToString());
                }
            }
        }

        [Test]
        public void OptimalMoves_IsWithinRange_AndMatchesSolver()
        {
            var settings = Settings(5);
            settings.MinOptimalMoves = 15;
            settings.MaxOptimalMoves = 17;

            Assert.IsTrue(LevelGenerator.TryGenerate(settings, new Random(7), out GeneratedLevel level));

            Assert.That(level.OptimalMoves, Is.InRange(15, 17));
            SolveResult check = Solver.Solve(level.Board);
            Assert.AreEqual(SolveStatus.Solved, check.Status);
            Assert.AreEqual(level.OptimalMoves, check.Moves.Count);
        }

        [Test]
        public void UsesGivenColourIds()
        {
            byte[] ids = { 10, 20, 30, 40, 50, 60 };

            LevelGenerator.TryGenerate(Settings(4), new Random(3), out GeneratedLevel level, ids);

            CollectionAssert.AreEquivalent(new byte[] { 10, 20, 30, 40 }, level.Board.CountColors().Keys);
        }

        [Test]
        public void UnreachableDifficulty_ReturnsFalse()
        {
            var settings = Settings(3);
            settings.MinOptimalMoves = 500;
            settings.MaxAttempts = 3;

            Assert.IsFalse(LevelGenerator.TryGenerate(settings, new Random(1), out GeneratedLevel level));
            Assert.IsNull(level);
        }

        [Test]
        public void InvalidSettings_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => LevelGenerator.TryGenerate(Settings(1), new Random(1), out _));
            Assert.Throws<ArgumentException>(() =>
                LevelGenerator.TryGenerate(new GeneratorSettings { MinOptimalMoves = 5, MaxOptimalMoves = 4 }, new Random(1), out _));
            Assert.Throws<ArgumentException>(() => LevelGenerator.TryGenerate(Settings(4), new Random(1), out _, new byte[] { 1, 2 }));
        }
    }
}
