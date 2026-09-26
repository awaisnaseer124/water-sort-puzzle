using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ColorSort.Game.Data
{
    // Cosmetic art. An item's index is its id in the player's save, so append, never reorder.
    [CreateAssetMenu(menuName = "ColorSort/Skin Catalog", fileName = "SkinCatalog")]
    public sealed class SkinCatalog : ScriptableObject
    {
        public const int UnlockPrice = 200;

        [SerializeField] private List<Sprite> _backgrounds = new List<Sprite>();
        [SerializeField] private List<BottleDesign> _bottles = new List<BottleDesign>();

        public IReadOnlyList<Sprite> Backgrounds => _backgrounds;
        public IReadOnlyList<BottleDesign> Bottles => _bottles;
        public IReadOnlyList<Sprite> BottlePreviews => _bottles.Select(b => b.Preview).ToList();

        public Sprite Background(int id) => id >= 0 && id < _backgrounds.Count ? _backgrounds[id] : _backgrounds.FirstOrDefault();

        public BottleDesign Bottle(int id) => id >= 0 && id < _bottles.Count ? _bottles[id] : _bottles.FirstOrDefault();

#if UNITY_EDITOR
        public void EditorSet(IEnumerable<Sprite> backgrounds, IEnumerable<BottleDesign> bottles)
        {
            _backgrounds = backgrounds.ToList();
            _bottles = bottles.ToList();
        }
#endif
    }

    // A bottle is drawn in two layers around the liquid: the body behind it (its shape also clips the liquid)
    // and the glass in front (outline, rim, reflections, transparent inside).
    [Serializable]
    public sealed class BottleDesign
    {
        [SerializeField] private Sprite _body;
        [SerializeField] private Sprite _glass;
        [Tooltip("Shown in the shop; defaults to the body.")]
        [SerializeField] private Sprite _preview;

        public BottleDesign()
        {
        }

        public BottleDesign(Sprite body, Sprite glass, Sprite preview)
        {
            _body = body;
            _glass = glass;
            _preview = preview;
        }

        public Sprite Body => _body;
        public Sprite Glass => _glass;
        public Sprite Preview => _preview != null ? _preview : _body;
    }
}
