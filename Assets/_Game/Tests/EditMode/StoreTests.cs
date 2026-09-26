using System;
using System.Linq;
using ColorSort.Core.Progression;
using ColorSort.Core.Store;
using NUnit.Framework;

namespace ColorSort.Core.Tests
{
    public class StoreTests
    {
        private PlayerProfile _profile;
        private int _saves;

        [SetUp]
        public void SetUp()
        {
            _profile = new PlayerProfile(new PlayerData());
            _saves = 0;
            _profile.Changed += () => _saves++;
        }

        [TestCase(StoreCatalog.Coins1K, 1000)]
        [TestCase(StoreCatalog.Coins10K, 10000)]
        [TestCase(StoreCatalog.Coins20K, 20000)]
        public void CoinPacks_GrantCoins(string productId, int coins)
        {
            Assert.AreEqual(FulfillmentResult.Granted, PurchaseFulfillment.Grant(_profile, productId, "t1", 50));

            Assert.AreEqual(coins, _profile.Wallet.Balance);
        }

        [Test]
        public void RemoveAds_RemovesAds()
        {
            PurchaseFulfillment.Grant(_profile, StoreCatalog.RemoveAds, "t1", 50);

            Assert.IsTrue(_profile.AdsRemoved);
        }

        [Test]
        public void UnlockLevels_UnlocksEveryLevelInEveryMode_ButNotAds()
        {
            PurchaseFulfillment.Grant(_profile, StoreCatalog.UnlockLevels, "t1", 50);

            foreach (GameMode mode in (GameMode[])Enum.GetValues(typeof(GameMode)))
                Assert.AreEqual(49, _profile.Progress.HighestUnlocked(mode), mode.ToString());
            Assert.IsFalse(_profile.AdsRemoved);
        }

        [Test]
        public void UnlockAllGame_RemovesAdsAndUnlocksLevels()
        {
            PurchaseFulfillment.Grant(_profile, StoreCatalog.UnlockAllGame, "t1", 50);

            Assert.IsTrue(_profile.AdsRemoved);
            Assert.AreEqual(49, _profile.Progress.HighestUnlocked(GameMode.Timed));
        }

        [Test]
        public void SameTransactionTwice_GrantsOnce()
        {
            PurchaseFulfillment.Grant(_profile, StoreCatalog.Coins1K, "t1", 50);
            FulfillmentResult second = PurchaseFulfillment.Grant(_profile, StoreCatalog.Coins1K, "t1", 50);

            Assert.AreEqual(FulfillmentResult.AlreadyGranted, second);
            Assert.AreEqual(1000, _profile.Wallet.Balance);
        }

        [Test]
        public void DifferentTransactions_EachGrant()
        {
            PurchaseFulfillment.Grant(_profile, StoreCatalog.Coins1K, "t1", 50);
            PurchaseFulfillment.Grant(_profile, StoreCatalog.Coins1K, "t2", 50);

            Assert.AreEqual(2000, _profile.Wallet.Balance);
        }

        [Test]
        public void Grant_SavesOnce_WithLedgerEntry()
        {
            PurchaseFulfillment.Grant(_profile, StoreCatalog.UnlockAllGame, "t1", 50);

            Assert.AreEqual(1, _saves);
            Assert.IsTrue(_profile.Purchases.Contains("t1"));
        }

        [Test]
        public void UnknownProduct_GrantsNothing()
        {
            Assert.AreEqual(FulfillmentResult.UnknownProduct, PurchaseFulfillment.Grant(_profile, "unlockweapons", "t1", 50));
            Assert.AreEqual(0, _saves);
            Assert.IsFalse(_profile.Purchases.Contains("t1"));
        }

        [Test]
        public void Catalog_HasUniqueIds_AndNoWeapons()
        {
            Assert.AreEqual(StoreCatalog.Products.Count, StoreCatalog.Products.Select(p => p.Id).Distinct().Count());
            Assert.IsNull(StoreCatalog.Find("unlockweapons"));
            Assert.AreEqual(ProductKind.NonConsumable, StoreCatalog.Find(StoreCatalog.UnlockLevels).Kind, "Must be restorable");
        }

        [Test]
        public void Ledger_IsBounded_OldestDropped()
        {
            for (int i = 0; i < PurchaseLedger.Capacity + 5; i++)
                _profile.Purchases.TryRecord("t" + i);

            Assert.AreEqual(PurchaseLedger.Capacity, _profile.Data.processedTransactions.Count);
            Assert.IsFalse(_profile.Purchases.Contains("t0"));
            Assert.IsTrue(_profile.Purchases.Contains("t" + (PurchaseLedger.Capacity + 4)));
        }

        [Test]
        public void BatchChanges_CoalescesAndNests()
        {
            using (_profile.BatchChanges())
            {
                _profile.Wallet.Add(1);
                using (_profile.BatchChanges())
                    _profile.Wallet.Add(1);
                Assert.AreEqual(0, _saves, "Nothing saved while a batch is open");
                _profile.RemoveAds();
            }

            Assert.AreEqual(1, _saves);
        }

        [Test]
        public void BatchChanges_WithoutChanges_DoesNotSave()
        {
            using (_profile.BatchChanges())
            {
            }

            Assert.AreEqual(0, _saves);
        }
    }
}

namespace ColorSort.Core.Tests
{
    public class CosmeticShopTests
    {
        [Test]
        public void BuysLowestLockedItem_WhenAffordable()
        {
            var profile = new PlayerProfile(new PlayerData());
            profile.Wallet.Add(200);

            ShopResult result = CosmeticShop.TryBuyNext(profile.Backgrounds, profile.Wallet, 7, 200, out int id);

            Assert.AreEqual(ShopResult.Bought, result);
            Assert.AreEqual(1, id);
            Assert.IsTrue(profile.Backgrounds.IsUnlocked(1));
            Assert.AreEqual(0, profile.Wallet.Balance);
        }

        [Test]
        public void NotEnoughCoins_ChangesNothing()
        {
            var profile = new PlayerProfile(new PlayerData());
            profile.Wallet.Add(199);

            Assert.AreEqual(ShopResult.NotEnoughCoins, CosmeticShop.TryBuyNext(profile.Backgrounds, profile.Wallet, 7, 200, out int id));
            Assert.AreEqual(-1, id);
            Assert.AreEqual(199, profile.Wallet.Balance);
            Assert.IsFalse(profile.Backgrounds.IsUnlocked(1));
        }

        [Test]
        public void AllOwned_DoesNotCharge()
        {
            var profile = new PlayerProfile(new PlayerData());
            profile.Wallet.Add(500);
            profile.Backgrounds.Unlock(1);

            Assert.AreEqual(ShopResult.AllOwned, CosmeticShop.TryBuyNext(profile.Backgrounds, profile.Wallet, 2, 200, out _));
            Assert.AreEqual(500, profile.Wallet.Balance);
        }
    }
}
