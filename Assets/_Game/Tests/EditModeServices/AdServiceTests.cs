using System.Collections.Generic;
using ColorSort.Services.Ads;
using ColorSort.Services.Analytics;
using ColorSort.Services.Haptics;
using ColorSort.Services.Purchasing;
using NUnit.Framework;

namespace ColorSort.Services.Tests
{
    public class AdServiceTests
    {
        private double _now;
        private bool _adsRemoved;
        private SimulatedAdNetwork _primary;
        private SimulatedAdNetwork _fallback;
        private List<string> _events;
        private AdService _ads;

        [SetUp]
        public void SetUp()
        {
            _now = 1000;
            _adsRemoved = false;
            _primary = new SimulatedAdNetwork();
            _fallback = new SimulatedAdNetwork();
            _events = new List<string>();
            var policy = new AdPolicy { StartupGraceSeconds = 30, MinSecondsBetweenInterstitials = 60 };
            _ads = new AdService(new[] { _primary, _fallback }, policy, () => _adsRemoved, () => _now,
                new LogAnalyticsService(_events.Add));
        }

        [Test]
        public void Rewarded_Completed_ReachesCaller()
        {
            AdResult? result = null;

            _ads.ShowRewarded(AdPlacements.ExtraBottle, r => result = r);

            Assert.AreEqual(AdResult.Completed, result);
            Assert.AreEqual(1, _primary.RewardedShown);
        }

        [Test]
        public void Rewarded_Skipped_IsReportedAsSkipped()
        {
            _primary.Result = AdResult.Skipped;
            AdResult? result = null;

            _ads.ShowRewarded(AdPlacements.ExtraBottle, r => result = r);

            Assert.AreEqual(AdResult.Skipped, result);
        }

        [Test]
        public void Rewarded_FallsBackToNextNetwork()
        {
            _primary.Ready = false;

            _ads.ShowRewarded(AdPlacements.FreeCoins, _ => { });

            Assert.AreEqual(0, _primary.RewardedShown);
            Assert.AreEqual(1, _fallback.RewardedShown);
        }

        [Test]
        public void Rewarded_NothingReady_IsNotAvailable()
        {
            _primary.Ready = _fallback.Ready = false;
            AdResult? result = null;

            _ads.ShowRewarded(AdPlacements.FreeCoins, r => result = r);

            Assert.AreEqual(AdResult.NotAvailable, result);
            Assert.IsFalse(_ads.IsRewardedReady);
        }

        [Test]
        public void Rewarded_StillAvailableAfterRemoveAds()
        {
            _adsRemoved = true;
            AdResult? result = null;

            _ads.ShowRewarded(AdPlacements.DoubleReward, r => result = r);

            Assert.AreEqual(AdResult.Completed, result);
        }

        [Test]
        public void Interstitial_BlockedDuringStartupGrace()
        {
            _now += 29;
            Assert.IsFalse(_ads.TryShowInterstitial(AdPlacements.LevelComplete));

            _now += 1;
            Assert.IsTrue(_ads.TryShowInterstitial(AdPlacements.LevelComplete));
        }

        [Test]
        public void Interstitial_FrequencyCapped()
        {
            _now += 30;
            Assert.IsTrue(_ads.TryShowInterstitial(AdPlacements.LevelComplete));

            _now += 59;
            Assert.IsFalse(_ads.TryShowInterstitial(AdPlacements.Restart));

            _now += 1;
            Assert.IsTrue(_ads.TryShowInterstitial(AdPlacements.Restart));
            Assert.AreEqual(2, _primary.InterstitialsShown);
        }

        [Test]
        public void Interstitial_CappedAfterRewarded()
        {
            _now += 100;
            _ads.ShowRewarded(AdPlacements.FreeCoins, _ => { });

            _now += 10;
            Assert.IsFalse(_ads.TryShowInterstitial(AdPlacements.LevelComplete));
        }

        [Test]
        public void Interstitial_NeverWithRemoveAds()
        {
            _adsRemoved = true;
            _now += 1000;

            Assert.IsFalse(_ads.TryShowInterstitial(AdPlacements.LevelComplete));
            Assert.AreEqual(0, _primary.InterstitialsShown);
        }

        [Test]
        public void Banner_HiddenWithRemoveAds_AndRefreshable()
        {
            _ads.SetBannerVisible(true);
            Assert.IsTrue(_primary.BannerVisible);

            _adsRemoved = true;
            _ads.RefreshBanner();

            Assert.IsFalse(_primary.BannerVisible);
            Assert.IsFalse(_fallback.BannerVisible, "Only the first banner network is used");
        }

        [Test]
        public void EveryShowIsLogged()
        {
            _ads.ShowRewarded(AdPlacements.ExtraBottle, _ => { });

            Assert.AreEqual(1, _events.Count);
            StringAssert.Contains("placement=extra_bottle", _events[0]);
            StringAssert.Contains("result=Completed", _events[0]);
        }
    }

    public class SimpleServiceTests
    {
        [Test]
        public void SettingsGatedHaptics_RespectsSetting()
        {
            var inner = new CountingHaptics();
            bool enabled = false;
            var haptics = new SettingsGatedHaptics(inner, () => enabled);

            haptics.Play(HapticKind.Heavy);
            enabled = true;
            haptics.Play(HapticKind.Heavy);

            Assert.AreEqual(1, inner.Count);
        }

        [Test]
        public void SimulatedPurchase_GrantsThroughFulfiller()
        {
            var fulfiller = new RecordingFulfiller();
            var store = new SimulatedPurchaseService(fulfiller);
            PurchaseResult? result = null;

            store.Buy("coins1k", r => result = r);

            Assert.AreEqual(PurchaseResult.Success, result);
            Assert.AreEqual("coins1k", fulfiller.LastProduct);
            StringAssert.StartsWith("simulated-", fulfiller.LastTransaction);
        }

        [Test]
        public void SimulatedPurchase_Cancelled_GrantsNothing()
        {
            var fulfiller = new RecordingFulfiller();
            var store = new SimulatedPurchaseService(fulfiller) { NextResult = PurchaseResult.Cancelled };
            PurchaseResult? result = null;

            store.Buy("coins1k", r => result = r);

            Assert.AreEqual(PurchaseResult.Cancelled, result);
            Assert.IsNull(fulfiller.LastProduct);
        }

        private sealed class CountingHaptics : IHapticsService
        {
            public int Count { get; private set; }
            public void Play(HapticKind kind) => Count++;
        }

        private sealed class RecordingFulfiller : IPurchaseFulfiller
        {
            public string LastProduct { get; private set; }
            public string LastTransaction { get; private set; }

            public bool Fulfill(string productId, string transactionId)
            {
                LastProduct = productId;
                LastTransaction = transactionId;
                return true;
            }
        }
    }
}
