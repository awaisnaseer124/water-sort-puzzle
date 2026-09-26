using System;
using System.Collections.Generic;
using ColorSort.Core.Board;
using ColorSort.Core.Session;
using NUnit.Framework;

namespace ColorSort.Core.Tests
{
    public class GameSessionTests
    {
        // Solved by the three pours in SolveSimple.
        private static BoardState TwoColourBoard() => TestBoards.Make(4, "RRBB", "BBRR", "");

        private static void SolveSimple(GameSession session)
        {
            session.TryPour(0, 2); // "RR",   "BBRR", "BB"
            session.TryPour(1, 0); // "RRRR", "BB",   "BB"
            session.TryPour(2, 1); // "RRRR", "BBBB", ""
        }

        [Test]
        public void Constructor_RejectsSolvedBoard()
        {
            Assert.Throws<ArgumentException>(() => new GameSession(TestBoards.Make(4, "RRRR", "")));
        }

        [Test]
        public void Constructor_CopiesBoard()
        {
            BoardState board = TwoColourBoard();
            var session = new GameSession(board);

            board.TryPour(0, 2, out _);

            Assert.AreEqual(4, session.Bottles[0].Count);
        }

        [Test]
        public void TryPour_Valid_CountsMoveAndRaisesEvent()
        {
            var session = new GameSession(TwoColourBoard());
            var poured = new List<Pour>();
            session.Poured += poured.Add;

            PourCheck result = session.TryPour(0, 2);

            Assert.AreEqual(PourCheck.Allowed, result);
            Assert.AreEqual(1, session.Moves);
            Assert.AreEqual(1, poured.Count);
            Assert.AreEqual(2, poured[0].Amount);
        }

        [Test]
        public void TryPour_Invalid_DoesNotCountOrRaise()
        {
            var session = new GameSession(TwoColourBoard());
            int events = 0;
            session.Poured += _ => events++;

            PourCheck result = session.TryPour(0, 1);

            Assert.AreEqual(PourCheck.TargetFull, result);
            Assert.AreEqual(0, session.Moves);
            Assert.AreEqual(0, events);
        }

        [Test]
        public void SolvingBoard_Wins_AndLocksSession()
        {
            var session = new GameSession(TwoColourBoard());
            var statuses = new List<SessionStatus>();
            session.StatusChanged += statuses.Add;

            SolveSimple(session);

            Assert.AreEqual(SessionStatus.Won, session.Status);
            CollectionAssert.AreEqual(new[] { SessionStatus.Won }, statuses);
            Assert.AreEqual(3, session.Moves);
            Assert.IsFalse(session.CanUndo);
            Assert.AreEqual(PourCheck.SessionOver, session.TryPour(0, 2));
            Assert.AreEqual(PourCheck.SessionOver, session.CanPour(0, 2));
        }

        [Test]
        public void Undo_RestoresBoardAndMoves()
        {
            var session = new GameSession(TwoColourBoard());
            Pour? undone = null;
            session.Undone += p => undone = p;
            session.TryPour(0, 2);

            Assert.IsTrue(session.Undo());

            Assert.AreEqual(0, session.Moves);
            Assert.AreEqual("RRBB", TestBoards.Describe(session.Bottles[0]));
            Assert.AreEqual("", TestBoards.Describe(session.Bottles[2]));
            Assert.IsTrue(undone.HasValue);
            Assert.IsFalse(session.CanUndo);
        }

        [Test]
        public void Undo_WithNoHistory_ReturnsFalse()
        {
            Assert.IsFalse(new GameSession(TwoColourBoard()).Undo());
        }

        [Test]
        public void Undo_MultipleSteps_UnwindsInOrder()
        {
            var session = new GameSession(TwoColourBoard());
            session.TryPour(0, 2);
            session.TryPour(1, 0);

            session.Undo();
            session.Undo();

            CollectionAssert.AreEqual(
                new[] { "RRBB", "BBRR", "" },
                TestBoards.Describe(session.SnapshotBoard()));
        }

        [Test]
        public void AddEmptyBottle_RaisesEventAndAllowsPouring()
        {
            var session = new GameSession(TestBoards.Make(4, "RB", "BR"));
            int added = -1;
            session.BottleAdded += i => added = i;

            int index = session.AddEmptyBottle(4);

            Assert.AreEqual(2, index);
            Assert.AreEqual(2, added);
            Assert.AreEqual(PourCheck.Allowed, session.TryPour(0, index));
        }

        [Test]
        public void SnapshotBoard_IsIndependent()
        {
            var session = new GameSession(TwoColourBoard());
            BoardState snapshot = session.SnapshotBoard();

            snapshot.TryPour(0, 2, out _);

            Assert.AreEqual(4, session.Bottles[0].Count);
        }

        [Test]
        public void Tick_WithoutRules_OnlyAdvancesTime()
        {
            var session = new GameSession(TwoColourBoard());

            session.Tick(100f);

            Assert.AreEqual(100f, session.ElapsedSeconds);
            Assert.AreEqual(SessionStatus.Playing, session.Status);
        }

        [Test]
        public void Tick_IgnoresNonPositiveDelta()
        {
            var session = new GameSession(TwoColourBoard());

            session.Tick(-1f);
            session.Tick(0f);

            Assert.AreEqual(0f, session.ElapsedSeconds);
        }

        [Test]
        public void TimeLimit_Reached_Loses()
        {
            var session = new GameSession(TwoColourBoard(), new[] { new TimeLimitRule(10f) });

            session.Tick(9.5f);
            Assert.AreEqual(SessionStatus.Playing, session.Status);

            session.Tick(0.5f);
            Assert.AreEqual(SessionStatus.Lost, session.Status);
            Assert.AreEqual(PourCheck.SessionOver, session.TryPour(0, 2));
        }

        [Test]
        public void TimeLimit_StopsClockAfterLoss()
        {
            var session = new GameSession(TwoColourBoard(), new[] { new TimeLimitRule(1f) });

            session.Tick(1f);
            session.Tick(5f);

            Assert.AreEqual(1f, session.ElapsedSeconds);
        }

        [Test]
        public void WinningPour_BeatsLoseRule()
        {
            var rule = new TimeLimitRule(10f);
            var session = new GameSession(TwoColourBoard(), new[] { rule });
            session.Tick(9.99f);

            SolveSimple(session);

            Assert.AreEqual(SessionStatus.Won, session.Status);
        }

        [Test]
        public void ExtendThenResume_ContinuesPlay()
        {
            var rule = new TimeLimitRule(10f);
            var session = new GameSession(TwoColourBoard(), new[] { rule });
            session.Tick(10f);

            Assert.IsFalse(session.TryResume(), "Still out of time before extending.");

            rule.Extend(30f);
            Assert.IsTrue(session.TryResume());
            Assert.AreEqual(SessionStatus.Playing, session.Status);
            Assert.AreEqual(30f, rule.RemainingSeconds(session), 0.0001f);
        }

        [Test]
        public void TryResume_WhenPlaying_ReturnsFalse()
        {
            Assert.IsFalse(new GameSession(TwoColourBoard()).TryResume());
        }

        [Test]
        public void AddEmptyBottle_AfterSessionEnds_Throws()
        {
            var session = new GameSession(TwoColourBoard());
            SolveSimple(session);

            Assert.Throws<InvalidOperationException>(() => session.AddEmptyBottle(4));
        }
    }
}
