using System;
using System.Collections.Generic;
using System.Linq;

namespace ColorSort.Core.Progression
{
    public sealed class PlayerProfile
    {
        public PlayerProfile(PlayerData data)
        {
            Data = PlayerDataMigrator.Upgrade(data);
            Wallet = new Wallet(Data, NotifyChanged);
            Progress = new LevelProgress(Data, NotifyChanged);
            Settings = new UserSettings(Data.settings, NotifyChanged);
            Backgrounds = new CosmeticSlot(Data.backgrounds, NotifyChanged);
            BottleSkins = new CosmeticSlot(Data.bottleSkins, NotifyChanged);
            Purchases = new PurchaseLedger(Data.processedTransactions, NotifyChanged);
        }

        public event Action Changed;

        public PlayerData Data { get; }

        public Wallet Wallet { get; }
        public LevelProgress Progress { get; }
        public UserSettings Settings { get; }
        public CosmeticSlot Backgrounds { get; }
        public CosmeticSlot BottleSkins { get; }
        public PurchaseLedger Purchases { get; }

        public bool AdsRemoved => Data.adsRemoved;

        public void RemoveAds()
        {
            if (Data.adsRemoved)
                return;
            Data.adsRemoved = true;
            NotifyChanged();
        }

        public IDisposable BatchChanges()
        {
            _batchDepth++;
            return new Batch(this);
        }

        private int _batchDepth;
        private bool _changedDuringBatch;

        private void NotifyChanged()
        {
            if (_batchDepth > 0)
            {
                _changedDuringBatch = true;
                return;
            }
            Changed?.Invoke();
        }

        private void EndBatch()
        {
            if (--_batchDepth > 0 || !_changedDuringBatch)
                return;
            _changedDuringBatch = false;
            Changed?.Invoke();
        }

        private sealed class Batch : IDisposable
        {
            private PlayerProfile _profile;

            public Batch(PlayerProfile profile) => _profile = profile;

            public void Dispose()
            {
                _profile?.EndBatch();
                _profile = null; // double Dispose is harmless
            }
        }
    }

    public sealed class Wallet
    {
        private readonly PlayerData _data;
        private readonly Action _changed;

        internal Wallet(PlayerData data, Action changed)
        {
            _data = data;
            _changed = changed;
        }

        public event Action<int> BalanceChanged;

        public int Balance => _data.coins;

        public void Add(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Must be positive.");
            _data.coins = checked(_data.coins + amount);
            Notify();
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Must be positive.");
            if (_data.coins < amount)
                return false;
            _data.coins -= amount;
            Notify();
            return true;
        }

        private void Notify()
        {
            BalanceChanged?.Invoke(_data.coins);
            _changed();
        }
    }

    public readonly struct WinResult
    {
        public WinResult(bool newBest, bool unlockedNext)
        {
            NewBest = newBest;
            UnlockedNext = unlockedNext;
        }

        public bool NewBest { get; }
        public bool UnlockedNext { get; }
    }

    public sealed class LevelProgress
    {
        private readonly PlayerData _data;
        private readonly Action _changed;

        internal LevelProgress(PlayerData data, Action changed)
        {
            _data = data;
            _changed = changed;
        }

        public GameMode SelectedMode
        {
            get => GameModes.TryParse(_data.selectedMode, out GameMode mode) ? mode : GameMode.Moves;
            set
            {
                if (SelectedMode == value)
                    return;
                _data.selectedMode = value.ToString();
                _changed();
            }
        }

        public int CurrentLevel(GameMode mode) => Find(mode)?.currentLevel ?? 0;
        public int HighestUnlocked(GameMode mode) => Find(mode)?.highestUnlocked ?? 0;
        public bool IsUnlocked(GameMode mode, int levelIndex) => levelIndex >= 0 && levelIndex <= HighestUnlocked(mode);

        public int BestStars(GameMode mode, int levelIndex)
        {
            List<int> stars = Find(mode)?.bestStars;
            return stars != null && levelIndex >= 0 && levelIndex < stars.Count ? stars[levelIndex] : 0;
        }

        public int TotalStars(GameMode mode) => Find(mode)?.bestStars.Sum() ?? 0;

        public void SetCurrentLevel(GameMode mode, int levelIndex)
        {
            if (!IsUnlocked(mode, levelIndex))
                throw new ArgumentOutOfRangeException(nameof(levelIndex), levelIndex, $"Level is locked in {mode}.");

            ModeProgressData progress = GetOrCreate(mode);
            if (progress.currentLevel == levelIndex)
                return;
            progress.currentLevel = levelIndex;
            _changed();
        }

        public void UnlockUpTo(GameMode mode, int levelIndex)
        {
            if (levelIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(levelIndex), levelIndex, "Must not be negative.");
            ModeProgressData progress = GetOrCreate(mode);
            if (progress.highestUnlocked >= levelIndex)
                return;
            progress.highestUnlocked = levelIndex;
            _changed();
        }

        public WinResult RecordWin(GameMode mode, int levelIndex, int stars)
        {
            if (levelIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(levelIndex), levelIndex, "Must not be negative.");
            if (stars < 1 || stars > 3)
                throw new ArgumentOutOfRangeException(nameof(stars), stars, "Must be 1-3.");
            if (!IsUnlocked(mode, levelIndex))
                throw new InvalidOperationException($"Level {levelIndex} is locked in {mode}.");

            ModeProgressData progress = GetOrCreate(mode);
            while (progress.bestStars.Count <= levelIndex)
                progress.bestStars.Add(0);

            bool newBest = stars > progress.bestStars[levelIndex];
            if (newBest)
                progress.bestStars[levelIndex] = stars;

            bool unlockedNext = levelIndex == progress.highestUnlocked;
            if (unlockedNext)
                progress.highestUnlocked++;

            if (newBest || unlockedNext)
                _changed();
            return new WinResult(newBest, unlockedNext);
        }

        private ModeProgressData Find(GameMode mode)
        {
            string key = mode.ToString();
            return _data.modes.FirstOrDefault(m => m.mode == key);
        }

        private ModeProgressData GetOrCreate(GameMode mode)
        {
            ModeProgressData progress = Find(mode);
            if (progress == null)
            {
                progress = new ModeProgressData { mode = mode.ToString() };
                _data.modes.Add(progress);
            }
            return progress;
        }
    }

    public sealed class UserSettings
    {
        private readonly SettingsData _data;
        private readonly Action _changed;

        internal UserSettings(SettingsData data, Action changed)
        {
            _data = data;
            _changed = changed;
        }

        public bool MusicEnabled
        {
            get => _data.music;
            set
            {
                if (_data.music == value)
                    return;
                _data.music = value;
                _changed();
            }
        }

        public bool VibrationEnabled
        {
            get => _data.vibration;
            set
            {
                if (_data.vibration == value)
                    return;
                _data.vibration = value;
                _changed();
            }
        }
    }

    public sealed class CosmeticSlot
    {
        private readonly CosmeticSlotData _data;
        private readonly Action _changed;

        internal CosmeticSlot(CosmeticSlotData data, Action changed)
        {
            _data = data;
            _changed = changed;
        }

        public int Selected => _data.selected;
        public IReadOnlyList<int> Unlocked => _data.unlocked;

        public bool IsUnlocked(int id) => _data.unlocked.Contains(id);

        public void Select(int id)
        {
            if (!IsUnlocked(id))
                throw new InvalidOperationException($"Cosmetic {id} is locked.");
            if (_data.selected == id)
                return;
            _data.selected = id;
            _changed();
        }

        public bool Unlock(int id)
        {
            if (id < 0)
                throw new ArgumentOutOfRangeException(nameof(id), id, "Must not be negative.");
            if (IsUnlocked(id))
                return false;
            _data.unlocked.Add(id);
            _data.unlocked.Sort();
            _changed();
            return true;
        }

        public int NextLocked(int count)
        {
            for (int id = 0; id < count; id++)
            {
                if (!IsUnlocked(id))
                    return id;
            }
            return -1;
        }
    }

    public sealed class PurchaseLedger
    {
        public const int Capacity = 100;

        private readonly List<string> _ids;
        private readonly Action _changed;

        internal PurchaseLedger(List<string> ids, Action changed)
        {
            _ids = ids;
            _changed = changed;
        }

        public bool Contains(string transactionId) => _ids.Contains(transactionId);

        public bool TryRecord(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
                throw new ArgumentException("Transaction id is required.", nameof(transactionId));
            if (_ids.Contains(transactionId))
                return false;

            _ids.Add(transactionId);
            if (_ids.Count > Capacity)
                _ids.RemoveRange(0, _ids.Count - Capacity);
            _changed();
            return true;
        }
    }

    public enum ShopResult
    {
        Bought,
        NotEnoughCoins,
        AllOwned,
    }

    public static class CosmeticShop
    {
        public static ShopResult TryBuyNext(CosmeticSlot slot, Wallet wallet, int itemCount, int price, out int boughtId)
        {
            if (slot == null)
                throw new ArgumentNullException(nameof(slot));
            if (wallet == null)
                throw new ArgumentNullException(nameof(wallet));

            boughtId = slot.NextLocked(itemCount);
            if (boughtId < 0)
                return ShopResult.AllOwned;
            if (!wallet.TrySpend(price))
            {
                boughtId = -1;
                return ShopResult.NotEnoughCoins;
            }

            slot.Unlock(boughtId);
            return ShopResult.Bought;
        }
    }
}
