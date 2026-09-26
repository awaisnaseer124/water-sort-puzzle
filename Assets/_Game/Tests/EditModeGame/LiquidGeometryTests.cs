using ColorSort.Game.Board;
using NUnit.Framework;
using UnityEngine;

namespace ColorSort.Game.Tests
{
    public class LiquidGeometryTests
    {
        private const float Bottom = -194f;
        private const float Top = 150f;
        private const float Width = 94f;
        private const float Layer = 76f;

        [Test]
        public void Upright_MatchesTheUntiltedLayout()
        {
            LiquidGeometry g = LiquidGeometry.At(0f, Bottom, Top, Width, Layer);

            Assert.AreEqual(Bottom, g.Lowest, 0.01f);
            Assert.AreEqual(Layer, g.LayerThickness, 0.01f);
            Assert.AreEqual(0f, g.Centre.x, 0.01f);
        }

        [TestCase(90f)]
        [TestCase(-90f)]
        public void LyingFlat_LayersSpreadAlongTheLength(float degrees)
        {
            LiquidGeometry g = LiquidGeometry.At(degrees, Bottom, Top, Width, Layer);

            Assert.AreEqual(Layer * Width / (Top - Bottom), g.LayerThickness, 0.01f);
            Assert.AreEqual(-Width / 2f, g.Lowest, 0.01f, "Lowest point is the side wall");
        }

        [TestCase(10f)]
        [TestCase(-35f)]
        [TestCase(60f)]
        [TestCase(-110f)]
        public void AnyTilt_KeepsLayerAreaConstant(float degrees)
        {
            LiquidGeometry g = LiquidGeometry.At(degrees, Bottom, Top, Width, Layer);
            float radians = degrees * Mathf.Deg2Rad;
            float crossWidth = Mathf.Min(Width / Mathf.Abs(Mathf.Cos(radians)), (Top - Bottom) / Mathf.Abs(Mathf.Sin(radians)));

            Assert.AreEqual(Layer * Width, g.LayerThickness * crossWidth, 0.5f);
        }

        [Test]
        public void TiltingRaisesTheLowestPoint_UntilLyingFlat()
        {
            float upright = LiquidGeometry.At(0f, Bottom, Top, Width, Layer).Lowest;
            float tilted = LiquidGeometry.At(45f, Bottom, Top, Width, Layer).Lowest;
            float flat = LiquidGeometry.At(90f, Bottom, Top, Width, Layer).Lowest;

            Assert.Less(upright, tilted);
            Assert.Less(tilted, flat);
        }
    }
}
