using System;
using System.Collections.Generic;
using ColorSort.Core.Board;
using UnityEngine;

namespace ColorSort.Game.Data
{
    [Serializable]
    public sealed class BottleLayout
    {
        [SerializeField, Min(1)] private int _capacity = 4;

        [Tooltip("Colour ids from the ColorPalette, bottom to top.")]
        [SerializeField] private byte[] _layers = Array.Empty<byte>();

        public BottleLayout()
        {
        }

        public BottleLayout(int capacity, IReadOnlyList<byte> layers)
        {
            _capacity = capacity;
            _layers = new byte[layers.Count];
            for (int i = 0; i < layers.Count; i++)
                _layers[i] = layers[i];
        }

        public int Capacity => _capacity;
        public IReadOnlyList<byte> Layers => _layers;

        public Bottle ToBottle() => new Bottle(_capacity, _layers);
    }
}
