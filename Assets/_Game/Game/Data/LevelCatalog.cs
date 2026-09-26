using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ColorSort.Game.Data
{
    [CreateAssetMenu(menuName = "ColorSort/Level Catalog", fileName = "LevelCatalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        [SerializeField] private List<LevelDefinition> _levels = new List<LevelDefinition>();

        public IReadOnlyList<LevelDefinition> Levels => _levels;
        public int Count => _levels.Count;

#if UNITY_EDITOR
        public void EditorSetLevels(IEnumerable<LevelDefinition> levels) => _levels = levels.ToList();
#endif
    }
}
