using System;
using System.Collections.Generic;
using ColorSort.Core.Board;

namespace ColorSort.Core.Session
{
    public enum SessionStatus
    {
        Playing,
        Won,
        Lost,
    }

    public sealed class GameSession
    {
        private readonly BoardState _board;
        private readonly List<ILoseRule> _loseRules;
        private readonly Stack<Pour> _history = new Stack<Pour>();

        public GameSession(BoardState board, IEnumerable<ILoseRule> loseRules = null)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));

            _board = board.Clone();
            _loseRules = loseRules != null ? new List<ILoseRule>(loseRules) : new List<ILoseRule>();

            // A level that starts solved is a data error, not a win.
            if (_board.IsSolved)
                throw new ArgumentException("Board is already solved.", nameof(board));
        }

        public event Action<Pour> Poured;
        public event Action<Pour> Undone;
        public event Action<int> BottleAdded;
        public event Action<SessionStatus> StatusChanged;

        public IReadOnlyList<Bottle> Bottles => _board.Bottles;
        public SessionStatus Status { get; private set; } = SessionStatus.Playing;
        public int Moves { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public bool CanUndo => Status == SessionStatus.Playing && _history.Count > 0;

        public PourCheck CanPour(int from, int to) =>
            Status == SessionStatus.Playing ? _board.CanPour(from, to) : PourCheck.SessionOver;

        public PourCheck TryPour(int from, int to)
        {
            if (Status != SessionStatus.Playing)
                return PourCheck.SessionOver;

            PourCheck check = _board.TryPour(from, to, out Pour pour);
            if (check != PourCheck.Allowed)
                return check;

            _history.Push(pour);
            Moves++;
            Poured?.Invoke(pour);
            Evaluate();
            return check;
        }

        public bool Undo()
        {
            if (!CanUndo)
                return false;

            Pour pour = _history.Pop();
            _board.Revert(pour);
            Moves--;
            Undone?.Invoke(pour);
            return true;
        }

        public int AddEmptyBottle(int capacity)
        {
            if (Status != SessionStatus.Playing)
                throw new InvalidOperationException("Cannot add a bottle after the session has ended.");

            int index = _board.AddEmptyBottle(capacity);
            BottleAdded?.Invoke(index);
            return index;
        }

        public void Tick(float deltaSeconds)
        {
            if (Status != SessionStatus.Playing || deltaSeconds <= 0f)
                return;

            ElapsedSeconds += deltaSeconds;
            Evaluate();
        }

        public bool TryResume()
        {
            if (Status != SessionStatus.Lost || IsAnyLoseRuleMet())
                return false;

            SetStatus(SessionStatus.Playing);
            return true;
        }

        public BoardState SnapshotBoard() => _board.Clone();

        private void Evaluate()
        {
            if (_board.IsSolved)
                SetStatus(SessionStatus.Won);
            else if (IsAnyLoseRuleMet())
                SetStatus(SessionStatus.Lost);
        }

        private bool IsAnyLoseRuleMet()
        {
            foreach (ILoseRule rule in _loseRules)
            {
                if (rule.IsLost(this))
                    return true;
            }
            return false;
        }

        private void SetStatus(SessionStatus status)
        {
            if (Status == status)
                return;
            Status = status;
            StatusChanged?.Invoke(status);
        }
    }
}
