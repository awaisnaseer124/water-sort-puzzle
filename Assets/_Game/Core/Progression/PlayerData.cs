using System;
using System.Collections.Generic;

namespace ColorSort.Core.Progression
{
    [Serializable]
    public sealed class PlayerData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int coins;
        public bool adsRemoved;
        public string selectedMode = nameof(GameMode.Moves);
        public List<ModeProgressData> modes = new List<ModeProgressData>();
        public SettingsData settings = new SettingsData();
        public CosmeticSlotData backgrounds = new CosmeticSlotData();
        public CosmeticSlotData bottleSkins = new CosmeticSlotData();

        public List<string> processedTransactions = new List<string>();
    }

    [Serializable]
    public sealed class ModeProgressData
    {
        public string mode;

        public int currentLevel;

        public int highestUnlocked;

        public List<int> bestStars = new List<int>();
    }

    [Serializable]
    public sealed class SettingsData
    {
        public bool music = true;
        public bool vibration = true;
    }

    [Serializable]
    public sealed class CosmeticSlotData
    {
        public int selected;
        public List<int> unlocked = new List<int> { 0 };
    }

    public enum GameMode
    {
        Moves,
        Timed,
        MovesAndTimed,
    }

    public static class GameModes
    {
        public static bool TryParse(string name, out GameMode mode) =>
            Enum.TryParse(name, out mode) && Enum.IsDefined(typeof(GameMode), mode) && name == mode.ToString();
    }
}
