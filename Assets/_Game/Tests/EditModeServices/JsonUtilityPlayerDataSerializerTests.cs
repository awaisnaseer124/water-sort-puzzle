using System;
using System.Collections.Generic;
using ColorSort.Core.Progression;
using ColorSort.Services.Save;
using NUnit.Framework;

namespace ColorSort.Services.Tests
{
    public class JsonUtilityPlayerDataSerializerTests
    {
        private readonly JsonUtilityPlayerDataSerializer _serializer = new JsonUtilityPlayerDataSerializer();

        [Test]
        public void RoundTripsEveryField()
        {
            var data = new PlayerData
            {
                coins = 1234,
                adsRemoved = true,
                selectedMode = "Timed",
                settings = new SettingsData { music = false, vibration = true },
                backgrounds = new CosmeticSlotData { selected = 2, unlocked = new List<int> { 0, 2 } },
                modes = new List<ModeProgressData>
                {
                    new ModeProgressData { mode = "Timed", currentLevel = 3, highestUnlocked = 4, bestStars = new List<int> { 3, 2, 1 } },
                },
            };

            PlayerData copy = _serializer.Deserialize(_serializer.Serialize(data));

            Assert.AreEqual(1234, copy.coins);
            Assert.IsTrue(copy.adsRemoved);
            Assert.AreEqual("Timed", copy.selectedMode);
            Assert.IsFalse(copy.settings.music);
            CollectionAssert.AreEqual(new[] { 0, 2 }, copy.backgrounds.unlocked);
            Assert.AreEqual(4, copy.modes[0].highestUnlocked);
            CollectionAssert.AreEqual(new[] { 3, 2, 1 }, copy.modes[0].bestStars);
        }

        [Test]
        public void MissingFields_KeepDefaults()
        {
            PlayerData data = _serializer.Deserialize("{\"coins\": 7}");

            Assert.AreEqual(7, data.coins);
            Assert.IsTrue(data.settings.music);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("{not json")]
        public void InvalidText_IsFormatException(string text)
        {
            Assert.Throws<FormatException>(() => _serializer.Deserialize(text));
        }
    }
}
