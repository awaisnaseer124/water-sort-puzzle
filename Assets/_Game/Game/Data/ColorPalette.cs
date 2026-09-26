using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ColorSort.Game.Data
{
    [CreateAssetMenu(menuName = "ColorSort/Color Palette", fileName = "ColorPalette")]
    public sealed class ColorPalette : ScriptableObject
    {
        public static readonly Color MissingColor = Color.magenta;

        [Tooltip("Index = colour id. Do not reorder: levels store these indices.")]
        [SerializeField] private List<Entry> _entries = new List<Entry>();

        public int Count => _entries.Count;

        public Color GetColor(byte id) => id < _entries.Count ? _entries[id].Color : MissingColor;

        public string GetName(byte id) => id < _entries.Count ? _entries[id].Name : $"#{id}";

#if UNITY_EDITOR
        public void EditorSetEntries(IEnumerable<Entry> entries) => _entries = entries.ToList();
#endif

        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private string _name;
            [SerializeField] private Color _color = Color.white;

            public Entry()
            {
            }

            public Entry(string name, Color color)
            {
                _name = name;
                _color = color;
            }

            public string Name => _name;
            public Color Color => _color;
        }
    }
}
