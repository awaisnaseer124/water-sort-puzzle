using ColorSort.Game.UI;
using NUnit.Framework;

namespace ColorSort.Game.Tests
{
    public class RewardWheelTests
    {
        [TestCase(0.10f, 2)]
        [TestCase(0.18f, 2)]
        [TestCase(0.19f, 3)]
        [TestCase(0.44f, 3)]
        [TestCase(0.50f, 5)]
        [TestCase(0.60f, 5)]
        [TestCase(0.70f, 3)]
        [TestCase(0.82f, 3)]
        [TestCase(0.85f, 2)]
        [TestCase(0.90f, 2)]
        public void MultiplierAt_MapsSegments(float value, int expected)
        {
            Assert.AreEqual(expected, RewardWheel.MultiplierAt(value));
        }

        [Test]
        public void EveryReachableValue_PaysAtLeastDouble()
        {
            for (float v = RewardWheel.MinValue; v <= RewardWheel.MaxValue; v += 0.005f)
                Assert.GreaterOrEqual(RewardWheel.MultiplierAt(v), 2, $"value {v}");
        }
    }
}
