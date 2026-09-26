using System.Collections.Generic;
using System.IO;
using ColorSort.Core.Progression;
using ColorSort.Core.Store;
using ColorSort.Platform.Ads;
using ColorSort.Platform.Haptics;
using ColorSort.Platform.Purchasing;
using ColorSort.Services.Ads;
using ColorSort.Services.Analytics;
using ColorSort.Services.Haptics;
using ColorSort.Services.Purchasing;
using ColorSort.Services.Save;
using UnityEngine;

namespace ColorSort.App
{
    public sealed class GameServices
    {
        internal GameServices(GameConfig config, PlayerProfile profile, FilePlayerDataStore store, IAdService ads,
            IPurchaseService purchases, IAnalyticsService analytics, IHapticsService haptics)
        {
            Config = config;
            Profile = profile;
            Store = store;
            Ads = ads;
            Purchases = purchases;
            Analytics = analytics;
            Haptics = haptics;
        }

        public GameConfig Config { get; }
        public PlayerProfile Profile { get; }
        public FilePlayerDataStore Store { get; }
        public IAdService Ads { get; }
        public IPurchaseService Purchases { get; }
        public IAnalyticsService Analytics { get; }
        public IHapticsService Haptics { get; }
    }

    [DefaultExecutionOrder(-10000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        public const string SaveFileName = "player.json";

        internal static GameServices Services { get; private set; }

        public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        // Static state survives play sessions when domain reload is disabled.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Services = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            var host = new GameObject("[GameBootstrap]");
            DontDestroyOnLoad(host);
            host.AddComponent<GameBootstrap>();
        }

        private void Awake()
        {
            GameConfig config = LoadConfig();

            var store = new FilePlayerDataStore(SavePath, new JsonUtilityPlayerDataSerializer(), Debug.LogWarning);
            var profile = new PlayerProfile(store.Load());
            profile.Changed += () => Save(store, profile);

            IAnalyticsService analytics = config.LogAnalytics
                ? new LogAnalyticsService(Debug.Log)
                : (IAnalyticsService)new NullAnalyticsService();

            AdService ads = CreateAds(config, profile, analytics);
            IPurchaseService purchases = CreatePurchases(config, profile, analytics, ads);
            IHapticsService haptics = new SettingsGatedHaptics(
                Application.isEditor ? (IHapticsService)new NullHapticsService() : new NiceVibrationsHaptics(),
                () => profile.Settings.VibrationEnabled);

            Services = new GameServices(config, profile, store, ads, purchases, analytics, haptics);

            ads.Initialize();
            purchases.Initialize();
        }

        private static GameConfig LoadConfig()
        {
            var config = Resources.Load<GameConfig>(GameConfig.ResourceName);
            if (config != null)
                return config;

            Debug.LogError($"Resources/{GameConfig.ResourceName} not found; using defaults. Levels cannot be unlocked by purchase.");
            return ScriptableObject.CreateInstance<GameConfig>();
        }

        private static AdService CreateAds(GameConfig config, PlayerProfile profile, IAnalyticsService analytics)
        {
            var networks = new List<IAdNetwork>();
            if (config.SimulateAds)
            {
                networks.Add(new SimulatedAdNetwork(Debug.Log));
            }
            else
            {
                networks.Add(new UnityAdsNetwork(config.UnityAdsGameId, config.UseTestAds,
                    config.UnityAdsInterstitial, config.UnityAdsRewarded, Debug.LogWarning));
            }

            var policy = new AdPolicy
            {
                StartupGraceSeconds = config.StartupGraceSeconds,
                MinSecondsBetweenInterstitials = config.MinSecondsBetweenInterstitials,
            };
            return new AdService(networks, policy, () => profile.AdsRemoved, () => Time.realtimeSinceStartupAsDouble, analytics);
        }

        private static IPurchaseService CreatePurchases(GameConfig config, PlayerProfile profile, IAnalyticsService analytics, AdService ads)
        {
            int levelCount = config.LevelCatalog != null ? config.LevelCatalog.Count : 0;
            var fulfiller = new ProfileFulfiller(profile, levelCount, analytics, ads);
            return config.SimulatePurchases
                ? new SimulatedPurchaseService(fulfiller)
                : (IPurchaseService)new UnityIapPurchaseService(fulfiller, Debug.LogWarning);
        }

        private static void Save(FilePlayerDataStore store, PlayerProfile profile)
        {
            try
            {
                store.Save(profile.Data);
            }
            catch (IOException e)
            {
                Debug.LogError($"Saving progress failed: {e.Message}");
            }
            catch (System.UnauthorizedAccessException e)
            {
                Debug.LogError($"Saving progress failed: {e.Message}");
            }
        }

        private sealed class ProfileFulfiller : IPurchaseFulfiller
        {
            private readonly PlayerProfile _profile;
            private readonly int _levelCount;
            private readonly IAnalyticsService _analytics;
            private readonly AdService _ads;

            public ProfileFulfiller(PlayerProfile profile, int levelCount, IAnalyticsService analytics, AdService ads)
            {
                _profile = profile;
                _levelCount = levelCount;
                _analytics = analytics;
                _ads = ads;
            }

            public bool Fulfill(string productId, string transactionId)
            {
                FulfillmentResult result = PurchaseFulfillment.Grant(_profile, productId, transactionId, _levelCount);
                _analytics.Log(AnalyticsEvents.Purchase, new Dictionary<string, object>
                {
                    ["product"] = productId,
                    ["result"] = result.ToString(),
                });
                _ads.RefreshBanner(); // hide the banner at once if ads were just removed
                return result != FulfillmentResult.UnknownProduct;
            }
        }
    }
}
