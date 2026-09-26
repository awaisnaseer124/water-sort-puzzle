using System;

namespace ColorSort.Services.Ads
{
    public sealed class SimulatedAdNetwork : IAdNetwork
    {
        private readonly Action<string> _log;

        public SimulatedAdNetwork(Action<string> log = null)
        {
            _log = log ?? (_ => { });
        }

        public string Name => "simulated";
        public bool IsInterstitialReady => Ready;
        public bool IsRewardedReady => Ready;
        public bool SupportsBanner => true;

        public bool Ready { get; set; } = true;
        public AdResult Result { get; set; } = AdResult.Completed;
        public bool BannerVisible { get; private set; }
        public int InterstitialsShown { get; private set; }
        public int RewardedShown { get; private set; }

        public void Initialize() => _log("[Ads] Simulated network initialized.");

        public void ShowInterstitial(Action<AdResult> onClosed)
        {
            InterstitialsShown++;
            _log("[Ads] Simulated interstitial.");
            onClosed(AdResult.Completed);
        }

        public void ShowRewarded(Action<AdResult> onClosed)
        {
            RewardedShown++;
            _log($"[Ads] Simulated rewarded ad: {Result}.");
            onClosed(Result);
        }

        public void SetBannerVisible(bool visible) => BannerVisible = visible;
    }
}
