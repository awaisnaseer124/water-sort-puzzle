using System;
using System.Collections.Generic;
using ColorSort.Services.Analytics;

namespace ColorSort.Services.Ads
{
    public enum AdResult
    {
        Completed,
        Skipped,
        Failed,
        NotAvailable,
        Busy,
    }

    public static class AdPlacements
    {
        public const string ExtraBottle = "extra_bottle";
        public const string DoubleReward = "double_reward";
        public const string FreeCoins = "free_coins";
        public const string Hint = "hint";
        public const string LevelComplete = "level_complete";
        public const string Restart = "restart";
        public const string Home = "home";
    }

    public interface IAdService
    {
        void Initialize();
        bool IsRewardedReady { get; }
        void ShowRewarded(string placement, Action<AdResult> onFinished);

        bool TryShowInterstitial(string placement);

        void SetBannerVisible(bool visible);
    }

    public interface IAdNetwork
    {
        string Name { get; }
        void Initialize();
        bool IsInterstitialReady { get; }
        bool IsRewardedReady { get; }
        void ShowInterstitial(Action<AdResult> onClosed);
        void ShowRewarded(Action<AdResult> onClosed);
        bool SupportsBanner { get; }
        void SetBannerVisible(bool visible);
    }

    public sealed class AdPolicy
    {
        public double StartupGraceSeconds { get; set; } = 30;

        public double MinSecondsBetweenInterstitials { get; set; } = 60;
    }

    public sealed class AdService : IAdService
    {
        private readonly IReadOnlyList<IAdNetwork> _networks;
        private readonly AdPolicy _policy;
        private readonly Func<bool> _adsRemoved;
        private readonly Func<double> _clock;
        private readonly IAnalyticsService _analytics;
        private readonly double _startTime;

        private double _lastFullScreenTime = double.NegativeInfinity;
        private bool _showing;
        private bool _bannerWanted;

        public AdService(IReadOnlyList<IAdNetwork> networks, AdPolicy policy, Func<bool> adsRemoved, Func<double> clock, IAnalyticsService analytics = null)
        {
            _networks = networks ?? throw new ArgumentNullException(nameof(networks));
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _adsRemoved = adsRemoved ?? throw new ArgumentNullException(nameof(adsRemoved));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _analytics = analytics ?? new NullAnalyticsService();
            _startTime = clock();
        }

        public bool IsRewardedReady => !_showing && FirstReady(n => n.IsRewardedReady) != null;

        public void Initialize()
        {
            foreach (IAdNetwork network in _networks)
                network.Initialize();
        }

        public void ShowRewarded(string placement, Action<AdResult> onFinished)
        {
            if (onFinished == null)
                throw new ArgumentNullException(nameof(onFinished));

            if (_showing)
            {
                Finish(placement, "rewarded", null, AdResult.Busy, onFinished);
                return;
            }

            IAdNetwork network = FirstReady(n => n.IsRewardedReady);
            if (network == null)
            {
                Finish(placement, "rewarded", null, AdResult.NotAvailable, onFinished);
                return;
            }

            _showing = true;
            network.ShowRewarded(result =>
            {
                _showing = false;
                _lastFullScreenTime = _clock();
                Finish(placement, "rewarded", network, result, onFinished);
            });
        }

        public bool TryShowInterstitial(string placement)
        {
            double now = _clock();
            if (_adsRemoved() || _showing
                || now - _startTime < _policy.StartupGraceSeconds
                || now - _lastFullScreenTime < _policy.MinSecondsBetweenInterstitials)
                return false;

            IAdNetwork network = FirstReady(n => n.IsInterstitialReady);
            if (network == null)
                return false;

            _showing = true;
            _lastFullScreenTime = now;
            network.ShowInterstitial(result =>
            {
                _showing = false;
                _lastFullScreenTime = _clock();
                Log(placement, "interstitial", network, result);
            });
            return true;
        }

        public void SetBannerVisible(bool visible)
        {
            _bannerWanted = visible;
            bool show = visible && !_adsRemoved();
            IAdNetwork bannerNetwork = null;
            foreach (IAdNetwork network in _networks)
            {
                if (network.SupportsBanner && bannerNetwork == null)
                    bannerNetwork = network;
            }
            bannerNetwork?.SetBannerVisible(show);
        }

        public void RefreshBanner() => SetBannerVisible(_bannerWanted);

        private IAdNetwork FirstReady(Func<IAdNetwork, bool> ready)
        {
            foreach (IAdNetwork network in _networks)
            {
                if (ready(network))
                    return network;
            }
            return null;
        }

        private void Finish(string placement, string format, IAdNetwork network, AdResult result, Action<AdResult> onFinished)
        {
            Log(placement, format, network, result);
            onFinished(result);
        }

        private void Log(string placement, string format, IAdNetwork network, AdResult result) =>
            _analytics.Log(AnalyticsEvents.Ad, new Dictionary<string, object>
            {
                ["placement"] = placement,
                ["format"] = format,
                ["network"] = network?.Name ?? "none",
                ["result"] = result.ToString(),
            });
    }
}
