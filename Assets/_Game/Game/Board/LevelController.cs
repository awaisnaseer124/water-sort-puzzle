using System;
using ColorSort.Core.Board;
using ColorSort.Core.Session;
using ColorSort.Game.Data;
using DG.Tweening;
using UnityEngine;

namespace ColorSort.Game.Board
{
    public sealed class LevelController : MonoBehaviour
    {
        private enum InputState
        {
            Idle,
            Selected,
            Animating,
        }

        [SerializeField] private BoardView _board;
        [SerializeField] private LevelCatalog _catalog;

        private InputState _state = InputState.Idle;
        private int _selected = -1;
        private bool _clockRunning;
        private TimeLimitRule _timeRule;
        private Pour _lastPour;
        private int _boardVersion;

        public event Action<LevelDefinition> LevelStarted;
        public event Action BottleTapped;
        public event Action<int> MovesChanged;
        public event Action<PourCheck> PourRejected;
        public event Action<int> BottleCompleted;
        public event Action<int> LevelWon;
        public event Action LevelLost;

        public event Action InputReady;

        public event Action PourStarted
        {
            add => _board.PourStarted += value;
            remove => _board.PourStarted -= value;
        }

        public event Action PourFinished
        {
            add => _board.PourFinished += value;
            remove => _board.PourFinished -= value;
        }

        public LevelCatalog Catalog => _catalog;
        public LevelDefinition Level { get; private set; }
        public GameSession Session { get; private set; }
        public bool IsTimed => _timeRule != null;
        public float RemainingSeconds => _timeRule != null && Session != null ? _timeRule.RemainingSeconds(Session) : 0f;

        public bool CanUndo => Session != null && Session.CanUndo && _state != InputState.Animating;

        public int BoardVersion => _boardVersion;

        public BoardState SnapshotBoard() => Session.SnapshotBoard();

        public int CurrentStars => Session == null ? 0 : Level.Stars.Rate(Math.Max(1, Session.Moves));

        private void Awake()
        {
            _board.BottleClicked += OnBottleClicked;
        }

        private void OnDestroy()
        {
            _board.BottleClicked -= OnBottleClicked;
            DetachSession();
        }

        private void Update()
        {
            if (_clockRunning && Session != null && Session.Status == SessionStatus.Playing)
                Session.Tick(Time.deltaTime);
        }

        public void StartLevel(int levelIndex, bool timed)
        {
            if ((uint)levelIndex >= (uint)_catalog.Count)
                throw new ArgumentOutOfRangeException(nameof(levelIndex), levelIndex, $"Catalog has {_catalog.Count} levels.");

            DetachSession();

            Level = _catalog.Levels[levelIndex];
            _timeRule = timed ? new TimeLimitRule(Level.TimeLimitSeconds) : null;
            Session = _timeRule != null
                ? new GameSession(Level.CreateBoard(), new ILoseRule[] { _timeRule })
                : new GameSession(Level.CreateBoard());
            Session.Poured += OnPoured;
            Session.StatusChanged += OnStatusChanged;

            _state = InputState.Idle;
            _selected = -1;
            _clockRunning = false;
            _boardVersion++;

            _board.Build(Session.Bottles);
            LevelStarted?.Invoke(Level);
            MovesChanged?.Invoke(0);
        }

        public void StartClock() => _clockRunning = true;

        public void PauseClock() => _clockRunning = false;

        public int AddExtraBottle()
        {
            int index = Session.AddEmptyBottle(Level.ExtraBottleCapacity);
            _boardVersion++;
            _board.ClearHint();
            _board.AddBottle(Session.Bottles[index]);
            return index;
        }

        public bool Undo()
        {
            if (_state == InputState.Animating || !Session.Undo())
                return false;

            _boardVersion++;
            ClearSelection();
            _board.ClearHint();
            _board.Refresh(Session.Bottles);
            MovesChanged?.Invoke(Session.Moves);
            InputReady?.Invoke();
            return true;
        }

        private void OnBottleClicked(int index)
        {
            if (Session == null || Session.Status != SessionStatus.Playing || _state == InputState.Animating)
                return;

            BottleTapped?.Invoke();
            _board.ClearHint();

            if (_state == InputState.Idle)
            {
                Bottle bottle = Session.Bottles[index];
                if (bottle.IsEmpty || bottle.IsComplete)
                {
                    Reject(index, bottle.IsEmpty ? PourCheck.SourceEmpty : PourCheck.SourceComplete);
                    return;
                }

                _selected = index;
                _state = InputState.Selected;
                _board.SetSelected(index, true);
                return;
            }

            if (index == _selected)
            {
                ClearSelection();
                return;
            }

            int from = _selected;
            PourCheck check = Session.TryPour(from, index);
            if (check != PourCheck.Allowed)
            {
                ClearSelection();
                Reject(from, check);
                return;
            }

            // Session state is already final; the view catches up.
            _selected = -1;
            _state = InputState.Animating;
            Pour pour = _lastPour;
            MovesChanged?.Invoke(Session.Moves);
            _board.PlayPour(pour, Session.Bottles).OnComplete(() => OnPourAnimated(pour));
        }

        private void OnPourAnimated(Pour pour)
        {
            _state = InputState.Idle;
            InputReady?.Invoke();

            if (Session.Bottles[pour.To].IsComplete)
                BottleCompleted?.Invoke(pour.To);

            if (Session.Status == SessionStatus.Won)
                LevelWon?.Invoke(Level.Stars.Rate(Session.Moves));
        }

        public void ShowHint(int from, int to)
        {
            ClearSelection();
            _board.ShowHint(from, to);
        }

        private void OnPoured(Pour pour)
        {
            _lastPour = pour;
            _boardVersion++;
        }

        private void OnStatusChanged(SessionStatus status)
        {
            if (status != SessionStatus.Lost)
                return;

            _clockRunning = false;
            ClearSelection();
            LevelLost?.Invoke();
        }

        private void Reject(int index, PourCheck reason)
        {
            _board.ShowRejected(index);
            PourRejected?.Invoke(reason);
        }

        private void ClearSelection()
        {
            if (_selected >= 0)
                _board.SetSelected(_selected, false);
            _selected = -1;
            if (_state == InputState.Selected)
                _state = InputState.Idle;
        }

        private void DetachSession()
        {
            if (Session == null)
                return;
            Session.Poured -= OnPoured;
            Session.StatusChanged -= OnStatusChanged;
            Session = null;
        }
    }
}
