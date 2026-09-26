using System;
using System.Collections.Generic;
using ColorSort.Core.Progression;
using NUnit.Framework;

namespace ColorSort.Core.Tests
{
    public class PlayerProfileTests
    {
        private PlayerProfile _profile;
        private int _changes;

        [SetUp]
        public void SetUp()
        {
            _profile = new PlayerProfile(new PlayerData());
            _changes = 0;
            _profile.Changed += () => _changes++;
        }

        [Test]
        public void FreshProfile_Defaults()
        {
            Assert.AreEqual(0, _profile.Wallet.Balance);
            Assert.IsFalse(_profile.AdsRemoved);
            Assert.AreEqual(GameMode.Moves, _profile.Progress.SelectedMode);
            Assert.AreEqual(0, _profile.Progress.CurrentLevel(GameMode.Timed));
            Assert.IsTrue(_profile.Progress.IsUnlocked(GameMode.Timed, 0));
            Assert.IsFalse(_profile.Progress.IsUnlocked(GameMode.Timed, 1));
            Assert.IsTrue(_profile.Settings.MusicEnabled);
            Assert.IsTrue(_profile.Settings.VibrationEnabled);
            Assert.IsTrue(_profile.Backgrounds.IsUnlocked(0));
            Assert.AreEqual(0, _profile.Backgrounds.Selected);
        }

        // Wallet

        [Test]
        public void Wallet_AddAndSpend_RaiseEvents()
        {
            var balances = new List<int>();
            _profile.Wallet.BalanceChanged += balances.Add;

            _profile.Wallet.Add(300);
            Assert.IsTrue(_profile.Wallet.TrySpend(200));

            CollectionAssert.AreEqual(new[] { 300, 100 }, balances);
            Assert.AreEqual(2, _changes);
        }

        [Test]
        public void Wallet_ExactBalanceIsEnough()
        {
            _profile.Wallet.Add(200);

            Assert.IsTrue(_profile.Wallet.TrySpend(200));
            Assert.AreEqual(0, _profile.Wallet.Balance);
        }

        [Test]
        public void Wallet_InsufficientFunds_ChangesNothing()
        {
            _profile.Wallet.Add(50);
            _changes = 0;

            Assert.IsFalse(_profile.Wallet.TrySpend(51));
            Assert.AreEqual(50, _profile.Wallet.Balance);
            Assert.AreEqual(0, _changes);
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void Wallet_RejectsNonPositiveAmounts(int amount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _profile.Wallet.Add(amount));
            Assert.Throws<ArgumentOutOfRangeException>(() => _profile.Wallet.TrySpend(amount));
        }

        // Progress

        [Test]
        public void RecordWin_UnlocksNext_AndKeepsBestStars()
        {
            WinResult first = _profile.Progress.RecordWin(GameMode.Moves, 0, 2);

            Assert.IsTrue(first.NewBest);
            Assert.IsTrue(first.UnlockedNext);
            Assert.IsTrue(_profile.Progress.IsUnlocked(GameMode.Moves, 1));
            Assert.AreEqual(2, _profile.Progress.BestStars(GameMode.Moves, 0));

            WinResult worse = _profile.Progress.RecordWin(GameMode.Moves, 0, 1);
            Assert.IsFalse(worse.NewBest);
            Assert.IsFalse(worse.UnlockedNext, "Replaying does not unlock again");
            Assert.AreEqual(2, _profile.Progress.BestStars(GameMode.Moves, 0));
            Assert.AreEqual(1, _profile.Progress.HighestUnlocked(GameMode.Moves));

            Assert.IsTrue(_profile.Progress.RecordWin(GameMode.Moves, 0, 3).NewBest);
            Assert.AreEqual(3, _profile.Progress.TotalStars(GameMode.Moves));
        }

        [Test]
        public void RecordWin_NoImprovement_DoesNotSave()
        {
            _profile.Progress.RecordWin(GameMode.Moves, 0, 3);
            _changes = 0;

            _profile.Progress.RecordWin(GameMode.Moves, 0, 3);

            Assert.AreEqual(0, _changes);
        }

        [Test]
        public void Progress_IsPerMode()
        {
            _profile.Progress.RecordWin(GameMode.Moves, 0, 3);

            Assert.IsFalse(_profile.Progress.IsUnlocked(GameMode.Timed, 1));
            Assert.AreEqual(0, _profile.Progress.BestStars(GameMode.Timed, 0));
        }

        [Test]
        public void RecordWin_LockedLevel_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _profile.Progress.RecordWin(GameMode.Moves, 3, 3));
        }

        [TestCase(0)]
        [TestCase(4)]
        public void RecordWin_RejectsInvalidStars(int stars)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _profile.Progress.RecordWin(GameMode.Moves, 0, stars));
        }

        [Test]
        public void SetCurrentLevel_OnlyUnlocked()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _profile.Progress.SetCurrentLevel(GameMode.Moves, 1));

            _profile.Progress.RecordWin(GameMode.Moves, 0, 1);
            _profile.Progress.SetCurrentLevel(GameMode.Moves, 1);

            Assert.AreEqual(1, _profile.Progress.CurrentLevel(GameMode.Moves));
        }

        [Test]
        public void UnlockUpTo_OnlyRaises()
        {
            _profile.Progress.UnlockUpTo(GameMode.Timed, 9);
            _profile.Progress.UnlockUpTo(GameMode.Timed, 4);

            Assert.AreEqual(9, _profile.Progress.HighestUnlocked(GameMode.Timed));
        }

        [Test]
        public void SelectedMode_Persists_AndRaisesOnlyOnChange()
        {
            _profile.Progress.SelectedMode = GameMode.Timed;
            _profile.Progress.SelectedMode = GameMode.Timed;

            Assert.AreEqual("Timed", _profile.Data.selectedMode);
            Assert.AreEqual(1, _changes);
        }

        // Settings, ads

        [Test]
        public void Settings_RaiseOnlyOnChange()
        {
            _profile.Settings.MusicEnabled = true;
            _profile.Settings.MusicEnabled = false;
            _profile.Settings.VibrationEnabled = false;

            Assert.AreEqual(2, _changes);
            Assert.IsFalse(_profile.Data.settings.music);
        }

        [Test]
        public void RemoveAds_IsIdempotent()
        {
            _profile.RemoveAds();
            _profile.RemoveAds();

            Assert.IsTrue(_profile.AdsRemoved);
            Assert.AreEqual(1, _changes);
        }

        // Cosmetics

        [Test]
        public void Cosmetics_UnlockSelectAndNextLocked()
        {
            Assert.AreEqual(1, _profile.Backgrounds.NextLocked(5));
            Assert.Throws<InvalidOperationException>(() => _profile.Backgrounds.Select(1));

            Assert.IsTrue(_profile.Backgrounds.Unlock(1));
            Assert.IsFalse(_profile.Backgrounds.Unlock(1));
            _profile.Backgrounds.Select(1);

            Assert.AreEqual(1, _profile.Backgrounds.Selected);
            Assert.AreEqual(2, _profile.Backgrounds.NextLocked(5));
            Assert.AreEqual(-1, _profile.Backgrounds.NextLocked(2));
            Assert.IsFalse(_profile.BottleSkins.IsUnlocked(1), "Families are independent");
        }
    }

    public class PlayerDataMigratorTests
    {
        [Test]
        public void Null_BecomesFreshData()
        {
            PlayerData data = PlayerDataMigrator.Upgrade(null);

            Assert.AreEqual(PlayerData.CurrentVersion, data.version);
            Assert.IsNotNull(data.modes);
        }

        [Test]
        public void RepairsDamagedData()
        {
            var data = new PlayerData
            {
                version = 0,
                coins = -10,
                selectedMode = "7",
                settings = null,
                backgrounds = new CosmeticSlotData { selected = 4, unlocked = new List<int> { 3, 3, -1 } },
                bottleSkins = null,
                modes = new List<ModeProgressData>
                {
                    new ModeProgressData { mode = "Moves", currentLevel = 9, highestUnlocked = 2, bestStars = new List<int> { 5, -1, 2 } },
                    new ModeProgressData { mode = "Moves", currentLevel = 0, highestUnlocked = 40 },
                    new ModeProgressData { mode = "Bogus" },
                    null,
                },
            };

            PlayerDataMigrator.Upgrade(data);

            Assert.AreEqual(PlayerData.CurrentVersion, data.version);
            Assert.AreEqual(0, data.coins);
            Assert.AreEqual("Moves", data.selectedMode);
            Assert.IsNotNull(data.settings);
            Assert.IsNotNull(data.bottleSkins);
            CollectionAssert.AreEqual(new[] { 0, 3 }, data.backgrounds.unlocked);
            Assert.AreEqual(0, data.backgrounds.selected, "Selected id was not owned");

            Assert.AreEqual(1, data.modes.Count, "Duplicate, unknown and null modes dropped");
            ModeProgressData moves = data.modes[0];
            Assert.AreEqual(2, moves.currentLevel, "Current level clamped to highest unlocked");
            CollectionAssert.AreEqual(new[] { 3, 0, 2 }, moves.bestStars);
        }

        [Test]
        public void NewerVersion_KeepsItsVersionNumber()
        {
            PlayerData data = PlayerDataMigrator.Upgrade(new PlayerData { version = PlayerData.CurrentVersion + 1 });

            Assert.AreEqual(PlayerData.CurrentVersion + 1, data.version);
        }

        [TestCase("Moves", true)]
        [TestCase("Timed", true)]
        [TestCase("MovesAndTimed", true)]
        [TestCase("moves", false)]
        [TestCase("1", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void GameModes_TryParse_IsStrict(string text, bool expected)
        {
            Assert.AreEqual(expected, GameModes.TryParse(text, out _));
        }
    }
}
