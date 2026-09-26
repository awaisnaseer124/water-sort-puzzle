using System;
using UnityEngine;

namespace ColorSort.Game.Board
{
    public static class BoardLayout
    {
        public static Vector2[] Arrange(int count, int maxPerRow, Vector2 spacing)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), count, "Must not be negative.");
            if (maxPerRow <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxPerRow), maxPerRow, "Must be positive.");

            var positions = new Vector2[count];
            if (count == 0)
                return positions;

            int rows = (count + maxPerRow - 1) / maxPerRow;
            int perRow = (count + rows - 1) / rows;

            for (int i = 0; i < count; i++)
            {
                int row = i / perRow;
                int column = i % perRow;
                int inThisRow = Math.Min(perRow, count - row * perRow);

                float x = (column - (inThisRow - 1) / 2f) * spacing.x;
                float y = ((rows - 1) / 2f - row) * spacing.y;
                positions[i] = new Vector2(x, y);
            }
            return positions;
        }

        public static int BestPerRow(int count, int maxPerRow, Vector2 spacing, Vector2 bottleSize, Vector2 available)
        {
            if (maxPerRow <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxPerRow), maxPerRow, "Must be positive.");

            int best = Math.Max(1, Math.Min(maxPerRow, count));
            float bestScale = -1f;
            for (int perRow = best; perRow >= 1; perRow--)
            {
                float scale = FitScale(count, perRow, spacing, bottleSize, available);
                if (scale > bestScale + 0.0001f)
                {
                    bestScale = scale;
                    best = perRow;
                }
            }

            if (count <= 0)
                return best;
            int rows = (count + best - 1) / best;
            return (count + rows - 1) / rows;
        }

        public static float FitScale(int count, int maxPerRow, Vector2 spacing, Vector2 bottleSize, Vector2 available)
        {
            if (count == 0)
                return 1f;
            int rows = (count + maxPerRow - 1) / maxPerRow;
            int perRow = (count + rows - 1) / rows;
            float width = (perRow - 1) * spacing.x + bottleSize.x;
            float height = (rows - 1) * spacing.y + bottleSize.y;
            return Math.Min(1f, Math.Min(available.x / width, available.y / height));
        }
    }
}
