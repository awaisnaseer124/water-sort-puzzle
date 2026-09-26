using System;
using System.Collections.Generic;
using System.Linq;
using ColorSort.Core.Board;
using ColorSort.Core.Progression;
using UnityEngine;

namespace ColorSort.Game.Data
{
    [CreateAssetMenu(menuName = "ColorSort/Level", fileName = "Level")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [SerializeField] private List<BottleLayout> _bottles = new List<BottleLayout>();

        [Tooltip("Where this level came from, e.g. the generator seed and settings.")]
        [SerializeField] private string _source;

        [Tooltip("Written by ColorSort > Validate Levels. 0 means not validated.")]
        [SerializeField, Min(0)] private int _optimalMoves;

        [Header("Overrides")]
        [SerializeField] private bool _overrideStars;
        [SerializeField, Min(1)] private int _threeStarMaxMoves = 1;
        [SerializeField, Min(1)] private int _twoStarMaxMoves = 1;

        [Tooltip("Time mode limit. 0 derives it from the optimal move count.")]
        [SerializeField, Min(0f)] private float _timeLimitSeconds;

        public IReadOnlyList<BottleLayout> Bottles => _bottles;
        public int OptimalMoves => _optimalMoves;
        public string Source => _source;
        public bool IsValidated => _optimalMoves > 0;

        public int ExtraBottleCapacity => _bottles.Count == 0 ? 4 : _bottles.Max(b => b.Capacity);

        public StarThresholds Stars
        {
            get
            {
                if (_overrideStars)
                    return new StarThresholds(_threeStarMaxMoves, _twoStarMaxMoves);
                RequireValidated();
                return StarThresholds.FromOptimal(_optimalMoves);
            }
        }

        public float TimeLimitSeconds
        {
            get
            {
                if (_timeLimitSeconds > 0f)
                    return _timeLimitSeconds;
                RequireValidated();
                return LevelBalancing.DefaultTimeLimitSeconds(_optimalMoves);
            }
        }

        public BoardState CreateBoard() => new BoardState(_bottles.Select(b => b.ToBottle()));

        private void RequireValidated()
        {
            if (!IsValidated)
                throw new InvalidOperationException($"Level '{name}' is not validated. Run ColorSort > Validate Levels.");
        }

#if UNITY_EDITOR
        public void EditorSetBottles(IEnumerable<BottleLayout> bottles)
        {
            _bottles = bottles.ToList();
            _optimalMoves = 0;
        }

        public void EditorSetOptimalMoves(int optimalMoves) => _optimalMoves = Math.Max(0, optimalMoves);

        public void EditorSetSource(string source) => _source = source;
#endif
    }
}
