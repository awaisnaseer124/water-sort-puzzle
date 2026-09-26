using System.Collections.Generic;
using ColorSort.Game.Data;
using UnityEngine;

namespace ColorSort.EditorTools
{
    public static class PaletteChecks
    {
        public const float MinColourDistance = 0.15f;

        public static bool AreConfusable(Color a, Color b) =>
            Vector3.Distance(new Vector3(a.r, a.g, a.b), new Vector3(b.r, b.g, b.b)) < MinColourDistance;

        public static List<byte> DistinctIds(ColorPalette palette)
        {
            var kept = new List<byte>();
            for (int id = 0; id < palette.Count && id <= byte.MaxValue; id++)
            {
                Color colour = palette.GetColor((byte)id);
                bool clash = kept.Exists(k => AreConfusable(colour, palette.GetColor(k)));
                if (!clash)
                    kept.Add((byte)id);
            }
            return kept;
        }
    }
}
