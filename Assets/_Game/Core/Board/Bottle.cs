using System;
using System.Collections.Generic;

namespace ColorSort.Core.Board
{
    public sealed class Bottle
    {
        private readonly byte[] _layers;
        private int _count;

        public Bottle(int capacity, IReadOnlyList<byte> layers = null)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be positive.");

            int count = layers?.Count ?? 0;
            if (count > capacity)
                throw new ArgumentException($"Bottle has {count} layers but capacity {capacity}.", nameof(layers));

            _layers = new byte[capacity];
            for (int i = 0; i < count; i++)
                _layers[i] = layers[i];
            _count = count;
        }

        public int Capacity => _layers.Length;
        public int Count => _count;
        public int FreeSpace => Capacity - _count;
        public bool IsEmpty => _count == 0;
        public bool IsFull => _count == Capacity;

        public bool IsComplete => IsFull && TopRunLength == Capacity;

        public bool IsSettled => IsEmpty || IsComplete;

        public byte this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_count)
                    throw new ArgumentOutOfRangeException(nameof(index), index, $"Bottle holds {_count} layers.");
                return _layers[index];
            }
        }

        public byte TopColor
        {
            get
            {
                if (_count == 0)
                    throw new InvalidOperationException("An empty bottle has no top colour.");
                return _layers[_count - 1];
            }
        }

        public int TopRunLength
        {
            get
            {
                if (_count == 0)
                    return 0;

                byte top = _layers[_count - 1];
                int run = 1;
                for (int i = _count - 2; i >= 0 && _layers[i] == top; i--)
                    run++;
                return run;
            }
        }

        public Bottle Clone() => new Bottle(Capacity, ToArray());

        public byte[] ToArray()
        {
            var copy = new byte[_count];
            Array.Copy(_layers, copy, _count);
            return copy;
        }

        internal void Push(byte color, int amount)
        {
            if (amount > FreeSpace)
                throw new InvalidOperationException($"Cannot push {amount} into a bottle with {FreeSpace} free.");
            for (int i = 0; i < amount; i++)
                _layers[_count++] = color;
        }

        internal void Pop(int amount)
        {
            if (amount > _count)
                throw new InvalidOperationException($"Cannot pop {amount} from a bottle holding {_count}.");
            _count -= amount;
        }

        public override string ToString() => $"[{string.Join(",", ToArray())}]/{Capacity}";
    }
}
