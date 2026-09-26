using UnityEngine;

namespace ColorSort.Game.Board
{
    // Where the liquid sits inside a tilted bottle, in a frame that stays upright (the liquid surface is level).
    // The inside of the glass is treated as a rectangle; each layer thins as the glass lies down so its area,
    // and therefore the apparent volume, stays the same.
    public readonly struct LiquidGeometry
    {
        public LiquidGeometry(Vector2 centre, float lowest, float layerThickness)
        {
            Centre = centre;
            Lowest = lowest;
            LayerThickness = layerThickness;
        }

        public Vector2 Centre { get; }
        public float Lowest { get; }
        public float LayerThickness { get; }

        public static LiquidGeometry At(float degrees, float bottom, float top, float width, float layerHeight)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Abs(Mathf.Cos(radians));
            float sin = Mathf.Abs(Mathf.Sin(radians));
            float height = top - bottom;

            Vector2 centre = Rotate(new Vector2(0f, (top + bottom) * 0.5f), degrees);
            float lowest = centre.y - (width * 0.5f * sin + height * 0.5f * cos);
            float crossWidth = Mathf.Min(width / Mathf.Max(cos, 0.001f), height / Mathf.Max(sin, 0.001f));
            return new LiquidGeometry(centre, lowest, layerHeight * width / crossWidth);
        }

        public static Vector2 Rotate(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(r), sin = Mathf.Sin(r);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}
