using System;
using System.Collections.Generic;

namespace ColorSort.Core.Board
{
    public sealed class BoardState
    {
        private readonly List<Bottle> _bottles;

        public BoardState(IEnumerable<Bottle> bottles)
        {
            if (bottles == null)
                throw new ArgumentNullException(nameof(bottles));

            _bottles = new List<Bottle>();
            foreach (Bottle bottle in bottles)
                _bottles.Add((bottle ?? throw new ArgumentException("Bottle list contains null.", nameof(bottles))).Clone());
        }

        public IReadOnlyList<Bottle> Bottles => _bottles;
        public int BottleCount => _bottles.Count;

        public bool IsSolved
        {
            get
            {
                foreach (Bottle bottle in _bottles)
                {
                    if (!bottle.IsSettled)
                        return false;
                }
                return true;
            }
        }

        public PourCheck CanPour(int from, int to)
        {
            if (!IsValidIndex(from) || !IsValidIndex(to))
                return PourCheck.InvalidIndex;
            if (from == to)
                return PourCheck.SameBottle;

            Bottle source = _bottles[from];
            Bottle target = _bottles[to];

            if (source.IsEmpty)
                return PourCheck.SourceEmpty;
            // A finished bottle is locked; pouring out of it can only undo progress.
            if (source.IsComplete)
                return PourCheck.SourceComplete;
            if (target.IsFull)
                return PourCheck.TargetFull;
            if (!target.IsEmpty && target.TopColor != source.TopColor)
                return PourCheck.ColorMismatch;

            return PourCheck.Allowed;
        }

        public PourCheck TryPour(int from, int to, out Pour pour)
        {
            PourCheck check = CanPour(from, to);
            if (check != PourCheck.Allowed)
            {
                pour = default;
                return check;
            }

            Bottle source = _bottles[from];
            Bottle target = _bottles[to];
            byte color = source.TopColor;
            int amount = Math.Min(source.TopRunLength, target.FreeSpace);

            source.Pop(amount);
            target.Push(color, amount);

            pour = new Pour(from, to, color, amount);
            return PourCheck.Allowed;
        }

        public void Revert(Pour pour)
        {
            if (!IsValidIndex(pour.From) || !IsValidIndex(pour.To) || pour.Amount <= 0)
                throw new ArgumentException($"Pour {pour} does not belong to this board.", nameof(pour));

            Bottle target = _bottles[pour.To];
            Bottle source = _bottles[pour.From];

            if (target.Count < pour.Amount || target.TopColor != pour.Color || target.TopRunLength < pour.Amount)
                throw new InvalidOperationException($"Pour {pour} is not the latest change to bottle {pour.To}.");
            if (source.FreeSpace < pour.Amount)
                throw new InvalidOperationException($"Bottle {pour.From} has no room to take back {pour}.");

            target.Pop(pour.Amount);
            source.Push(pour.Color, pour.Amount);
        }

        public int AddEmptyBottle(int capacity)
        {
            _bottles.Add(new Bottle(capacity));
            return _bottles.Count - 1;
        }

        public BoardState Clone() => new BoardState(_bottles);

        public Dictionary<byte, int> CountColors()
        {
            var counts = new Dictionary<byte, int>();
            foreach (Bottle bottle in _bottles)
            {
                for (int i = 0; i < bottle.Count; i++)
                {
                    counts.TryGetValue(bottle[i], out int n);
                    counts[bottle[i]] = n + 1;
                }
            }
            return counts;
        }

        public string Fingerprint()
        {
            var parts = new List<string>(_bottles.Count);
            foreach (Bottle bottle in _bottles)
                parts.Add(bottle.Capacity + ":" + string.Join(",", bottle.ToArray()));
            parts.Sort(StringComparer.Ordinal);
            return string.Join("|", parts);
        }

        private bool IsValidIndex(int index) => (uint)index < (uint)_bottles.Count;

        public override string ToString() => string.Join(" ", _bottles);
    }
}
