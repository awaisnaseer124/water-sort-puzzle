using System;
using System.Collections.Generic;
using System.Linq;

namespace ColorSort.Core.Progression
{
    public static class PlayerDataMigrator
    {
        public static PlayerData Upgrade(PlayerData data)
        {
            data ??= new PlayerData();

            // Version steps go here, e.g. if (data.version < 2) { ...; data.version = 2; }
            // A file from a newer build is loaded best-effort and keeps its version number.
            if (data.version < PlayerData.CurrentVersion)
                data.version = PlayerData.CurrentVersion;

            Normalize(data);
            return data;
        }

        private static void Normalize(PlayerData data)
        {
            data.coins = Math.Max(0, data.coins);

            if (!GameModes.TryParse(data.selectedMode, out GameMode _))
                data.selectedMode = nameof(GameMode.Moves);

            data.settings ??= new SettingsData();
            data.backgrounds = NormalizeSlot(data.backgrounds);
            data.bottleSkins = NormalizeSlot(data.bottleSkins);
            data.processedTransactions = (data.processedTransactions ?? new List<string>())
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            var seen = new HashSet<string>();
            data.modes = (data.modes ?? new List<ModeProgressData>())
                .Where(m => m != null && GameModes.TryParse(m.mode, out GameMode _) && seen.Add(m.mode))
                .ToList();

            foreach (ModeProgressData mode in data.modes)
            {
                mode.highestUnlocked = Math.Max(0, mode.highestUnlocked);
                mode.currentLevel = Math.Min(Math.Max(0, mode.currentLevel), mode.highestUnlocked);
                mode.bestStars = (mode.bestStars ?? new List<int>()).Select(s => Math.Min(3, Math.Max(0, s))).ToList();
            }
        }

        private static CosmeticSlotData NormalizeSlot(CosmeticSlotData slot)
        {
            slot ??= new CosmeticSlotData();
            slot.unlocked = (slot.unlocked ?? new List<int>()).Where(id => id >= 0).Distinct().OrderBy(id => id).ToList();
            if (!slot.unlocked.Contains(0))
                slot.unlocked.Insert(0, 0);
            if (!slot.unlocked.Contains(slot.selected))
                slot.selected = 0;
            return slot;
        }
    }
}
