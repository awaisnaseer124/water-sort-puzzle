using System;
using System.Collections.Generic;
using System.Linq;
using ColorSort.Core.Progression;

namespace ColorSort.Core.Store
{
    public enum ProductKind
    {
        Consumable,
        NonConsumable,
    }

    public sealed class StoreProduct
    {
        public StoreProduct(string id, ProductKind kind, int coins = 0, bool removesAds = false, bool unlocksAllLevels = false)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Product id is required.", nameof(id));
            if (coins < 0)
                throw new ArgumentOutOfRangeException(nameof(coins), coins, "Must not be negative.");

            Id = id;
            Kind = kind;
            Coins = coins;
            RemovesAds = removesAds;
            UnlocksAllLevels = unlocksAllLevels;
        }

        public string Id { get; }
        public ProductKind Kind { get; }
        public int Coins { get; }
        public bool RemovesAds { get; }
        public bool UnlocksAllLevels { get; }
    }

    public static class StoreCatalog
    {
        public const string RemoveAds = "removead";
        public const string UnlockLevels = "unlocklevels";
        public const string UnlockAllGame = "unlockallgame";
        public const string Coins1K = "coins1k";
        public const string Coins10K = "coins10k";
        public const string Coins20K = "coins20k";

        public static IReadOnlyList<StoreProduct> Products { get; } = new[]
        {
            new StoreProduct(RemoveAds, ProductKind.NonConsumable, removesAds: true),
            new StoreProduct(UnlockLevels, ProductKind.NonConsumable, unlocksAllLevels: true),
            new StoreProduct(UnlockAllGame, ProductKind.NonConsumable, removesAds: true, unlocksAllLevels: true),
            new StoreProduct(Coins1K, ProductKind.Consumable, coins: 1000),
            new StoreProduct(Coins10K, ProductKind.Consumable, coins: 10000),
            new StoreProduct(Coins20K, ProductKind.Consumable, coins: 20000),
        };

        public static StoreProduct Find(string id) => Products.FirstOrDefault(p => p.Id == id);
    }

    public enum FulfillmentResult
    {
        Granted,
        AlreadyGranted,
        UnknownProduct,
    }

    public static class PurchaseFulfillment
    {
        public static FulfillmentResult Grant(PlayerProfile profile, string productId, string transactionId, int levelCount)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            StoreProduct product = StoreCatalog.Find(productId);
            if (product == null)
                return FulfillmentResult.UnknownProduct;

            if (!string.IsNullOrEmpty(transactionId) && profile.Purchases.Contains(transactionId))
                return FulfillmentResult.AlreadyGranted;

            // One save for the grant and its ledger entry: a crash cannot leave one without the other.
            using (profile.BatchChanges())
            {
                if (!string.IsNullOrEmpty(transactionId))
                    profile.Purchases.TryRecord(transactionId);
                if (product.Coins > 0)
                    profile.Wallet.Add(product.Coins);
                if (product.RemovesAds)
                    profile.RemoveAds();
                if (product.UnlocksAllLevels && levelCount > 0)
                {
                    foreach (GameMode mode in (GameMode[])Enum.GetValues(typeof(GameMode)))
                        profile.Progress.UnlockUpTo(mode, levelCount - 1);
                }
            }
            return FulfillmentResult.Granted;
        }
    }
}
