using System;
using System.Collections.Generic;
using ColorSort.Core.Board;

namespace ColorSort.Core.Solving
{
    public enum SolveStatus
    {
        Solved,
        Unsolvable,
        LimitReached,
    }

    public readonly struct SolverMove
    {
        public SolverMove(int from, int to)
        {
            From = from;
            To = to;
        }

        public int From { get; }
        public int To { get; }

        public override string ToString() => $"{From}->{To}";
    }

    public sealed class SolveResult
    {
        internal SolveResult(SolveStatus status, IReadOnlyList<SolverMove> moves, int exploredStates)
        {
            Status = status;
            Moves = moves;
            ExploredStates = exploredStates;
        }

        public SolveStatus Status { get; }

        public IReadOnlyList<SolverMove> Moves { get; }

        public int ExploredStates { get; }
    }

    public static class Solver
    {
        public const int DefaultStateLimit = 1_000_000;

        public static SolveResult Solve(BoardState board, int stateLimit = DefaultStateLimit)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));
            if (stateLimit <= 0)
                throw new ArgumentOutOfRangeException(nameof(stateLimit), stateLimit, "Must be positive.");

            return new Search(board, stateLimit).Run();
        }

        private sealed class Search
        {
            private readonly int _bottleCount;
            private readonly int[] _capacity;
            private readonly int[] _offset;
            private readonly int _stateLength;
            private readonly int _stateLimit;

            private readonly List<Node> _nodes = new List<Node>();
            private readonly Dictionary<string, int> _bestCost = new Dictionary<string, int>();
            private readonly List<Stack<int>> _open = new List<Stack<int>>();
            private readonly int[] _order;
            private readonly char[] _keyBuffer;
            private readonly Comparison<int> _compareBottles;
            private byte[] _sortState;

            public Search(BoardState board, int stateLimit)
            {
                _stateLimit = stateLimit;
                _bottleCount = board.BottleCount;
                _capacity = new int[_bottleCount];
                _offset = new int[_bottleCount];

                int cursor = _bottleCount;
                for (int b = 0; b < _bottleCount; b++)
                {
                    _capacity[b] = board.Bottles[b].Capacity;
                    _offset[b] = cursor;
                    cursor += _capacity[b];
                }
                _stateLength = cursor;
                _order = new int[_bottleCount];
                _keyBuffer = new char[_stateLength];
                _compareBottles = (a, b) => CompareBottles(_sortState, a, b);

                var start = new byte[_stateLength];
                for (int b = 0; b < _bottleCount; b++)
                {
                    Bottle bottle = board.Bottles[b];
                    start[b] = (byte)bottle.Count;
                    for (int i = 0; i < bottle.Count; i++)
                        start[_offset[b] + i] = bottle[i];
                }

                AddNode(start, parent: -1, from: 0, to: 0, cost: 0);
            }

            public SolveResult Run()
            {
                int explored = 0;
                int bucket = 0;

                while (TryPop(ref bucket, out int nodeIndex))
                {
                    Node node = _nodes[nodeIndex];
                    if (_bestCost[node.Key] < node.Cost)
                        continue; // superseded by a cheaper path

                    if (IsSolved(node.State))
                        return new SolveResult(SolveStatus.Solved, BuildPath(nodeIndex), explored);

                    if (++explored > _stateLimit)
                        return new SolveResult(SolveStatus.LimitReached, Array.Empty<SolverMove>(), explored - 1);

                    Expand(nodeIndex);
                }

                return new SolveResult(SolveStatus.Unsolvable, Array.Empty<SolverMove>(), explored);
            }

            private void Expand(int nodeIndex)
            {
                byte[] state = _nodes[nodeIndex].State;
                int childCost = _nodes[nodeIndex].Cost + 1;

                for (int from = 0; from < _bottleCount; from++)
                {
                    int fromCount = state[from];
                    if (fromCount == 0)
                        continue;

                    int fromTop = _offset[from] + fromCount - 1;
                    byte color = state[fromTop];
                    int run = 1;
                    while (run < fromCount && state[fromTop - run] == color)
                        run++;

                    if (run == _capacity[from])
                        continue; // complete bottle is locked

                    // Pouring into any one empty bottle of a given capacity is equivalent to any other.
                    ulong triedEmptyCapacities = 0;

                    for (int to = 0; to < _bottleCount; to++)
                    {
                        if (to == from)
                            continue;

                        int toCount = state[to];
                        int free = _capacity[to] - toCount;
                        if (free == 0)
                            continue;

                        if (toCount == 0)
                        {
                            ulong bit = 1UL << Math.Min(_capacity[to], 63);
                            if ((triedEmptyCapacities & bit) != 0)
                                continue;
                            triedEmptyCapacities |= bit;

                            // Moving a whole single-colour bottle into an empty one of the same size changes nothing.
                            if (run == fromCount && _capacity[to] == _capacity[from])
                                continue;
                        }
                        else if (state[_offset[to] + toCount - 1] != color)
                        {
                            continue;
                        }

                        int amount = Math.Min(run, free);
                        var child = (byte[])state.Clone();
                        for (int i = 0; i < amount; i++)
                        {
                            child[_offset[from] + fromCount - 1 - i] = 0;
                            child[_offset[to] + toCount + i] = color;
                        }
                        child[from] = (byte)(fromCount - amount);
                        child[to] = (byte)(toCount + amount);

                        AddNode(child, nodeIndex, from, to, childCost);
                    }
                }
            }

            private void AddNode(byte[] state, int parent, int from, int to, int cost)
            {
                string key = CanonicalKey(state);
                if (_bestCost.TryGetValue(key, out int known) && known <= cost)
                    return;

                _bestCost[key] = cost;
                _nodes.Add(new Node(state, key, parent, from, to, cost));
                Push(cost + Heuristic(state), _nodes.Count - 1);
            }

            private int Heuristic(byte[] state)
            {
                int boundaries = 0;
                for (int b = 0; b < _bottleCount; b++)
                {
                    int start = _offset[b];
                    for (int i = 1; i < state[b]; i++)
                    {
                        if (state[start + i] != state[start + i - 1])
                            boundaries++;
                    }
                }
                return boundaries;
            }

            private bool IsSolved(byte[] state)
            {
                for (int b = 0; b < _bottleCount; b++)
                {
                    int count = state[b];
                    if (count == 0)
                        continue;
                    if (count != _capacity[b])
                        return false;
                    int start = _offset[b];
                    for (int i = 1; i < count; i++)
                    {
                        if (state[start + i] != state[start])
                            return false;
                    }
                }
                return true;
            }

            private string CanonicalKey(byte[] state)
            {
                for (int i = 0; i < _bottleCount; i++)
                    _order[i] = i;
                _sortState = state;
                Array.Sort(_order, _compareBottles);

                int cursor = 0;
                foreach (int b in _order)
                {
                    _keyBuffer[cursor++] = (char)(_capacity[b] << 8 | state[b]);
                    for (int i = 0; i < _capacity[b]; i++)
                        _keyBuffer[cursor++] = (char)state[_offset[b] + i];
                }
                return new string(_keyBuffer, 0, cursor);
            }

            private int CompareBottles(byte[] state, int a, int b)
            {
                int c = _capacity[a].CompareTo(_capacity[b]);
                if (c != 0)
                    return c;
                c = state[a].CompareTo(state[b]);
                if (c != 0)
                    return c;
                for (int i = 0; i < _capacity[a]; i++)
                {
                    c = state[_offset[a] + i].CompareTo(state[_offset[b] + i]);
                    if (c != 0)
                        return c;
                }
                return a.CompareTo(b);
            }

            private List<SolverMove> BuildPath(int nodeIndex)
            {
                var moves = new List<SolverMove>();
                for (int i = nodeIndex; _nodes[i].Parent >= 0; i = _nodes[i].Parent)
                    moves.Add(new SolverMove(_nodes[i].From, _nodes[i].To));
                moves.Reverse();
                return moves;
            }

            // Bucket queue keyed by f = cost + heuristic; LIFO within a bucket dives toward solutions.
            private void Push(int priority, int nodeIndex)
            {
                while (_open.Count <= priority)
                    _open.Add(new Stack<int>());
                _open[priority].Push(nodeIndex);
            }

            private bool TryPop(ref int bucket, out int nodeIndex)
            {
                for (; bucket < _open.Count; bucket++)
                {
                    if (_open[bucket].Count > 0)
                    {
                        nodeIndex = _open[bucket].Pop();
                        return true;
                    }
                }
                nodeIndex = -1;
                return false;
            }

            private readonly struct Node
            {
                public Node(byte[] state, string key, int parent, int from, int to, int cost)
                {
                    State = state;
                    Key = key;
                    Parent = parent;
                    From = (byte)from;
                    To = (byte)to;
                    Cost = cost;
                }

                public byte[] State { get; }
                public string Key { get; }
                public int Parent { get; }
                public byte From { get; }
                public byte To { get; }
                public int Cost { get; }
            }
        }
    }
}
