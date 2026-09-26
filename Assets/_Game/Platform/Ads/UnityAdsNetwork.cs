using System;
using ColorSort.Services.Ads;
using UnityEngine.Advertisements;

namespace ColorSort.Platform.Ads
{
    public sealed class UnityAdsNetwork : IAdNetwork
    {
        private readonly string _gameId;
        private readonly bool _testMode;
        private readonly string _interstitialPlacement;
        private readonly string _rewardedPlacement;
        private readonly Action<string> _warn;
        private readonly Listener _listener;

        private bool _interstitialLoaded;
        private bool _rewardedLoaded;
        private Action<AdResult> _pendingClose;

        public UnityAdsNetwork(string gameId, bool testMode, string interstitialPlacement, string rewardedPlacement, Action<string> warn)
        {
            _gameId = gameId;
            _testMode = testMode;
            _interstitialPlacement = interstitialPlacement;
            _rewardedPlacement = rewardedPlacement;
            _warn = warn ?? (_ => { });
            _listener = new Listener(this);
        }

        public string Name => "unity_ads";
        public bool IsInterstitialReady => _interstitialLoaded;
        public bool IsRewardedReady => _rewardedLoaded;
        public bool SupportsBanner => false;

        public void Initialize()
        {
            if (string.IsNullOrEmpty(_gameId))
            {
                _warn("Unity Ads game id is not set; network disabled.");
                return;
            }
            if (!Advertisement.isInitialized)
                Advertisement.Initialize(_gameId, _testMode, _listener);
        }

        public void ShowInterstitial(Action<AdResult> onClosed) => Show(_interstitialPlacement, ref _interstitialLoaded, onClosed);

        public void ShowRewarded(Action<AdResult> onClosed) => Show(_rewardedPlacement, ref _rewardedLoaded, onClosed);

        public void SetBannerVisible(bool visible)
        {
        }

        private void Show(string placement, ref bool loaded, Action<AdResult> onClosed)
        {
            if (!loaded)
            {
                onClosed(AdResult.NotAvailable);
                return;
            }
            loaded = false;
            _pendingClose = onClosed;
            Advertisement.Show(placement, _listener);
        }

        private void Load(string placement)
        {
            if (Advertisement.isInitialized && !string.IsNullOrEmpty(placement))
                Advertisement.Load(placement, _listener);
        }

        private void Close(AdResult result)
        {
            Action<AdResult> close = _pendingClose;
            _pendingClose = null;
            close?.Invoke(result);
        }

        private void OnInitialized()
        {
            Load(_interstitialPlacement);
            Load(_rewardedPlacement);
        }

        private void OnLoaded(string placementId)
        {
            if (placementId == _interstitialPlacement)
                _interstitialLoaded = true;
            else if (placementId == _rewardedPlacement)
                _rewardedLoaded = true;
        }

        private void OnShowComplete(string placementId, bool watchedToEnd)
        {
            Load(placementId);
            Close(watchedToEnd ? AdResult.Completed : AdResult.Skipped);
        }

        private void OnShowFailed(string placementId, string reason)
        {
            _warn($"Unity Ads failed to show {placementId}: {reason}");
            Load(placementId);
            Close(AdResult.Failed);
        }

        private sealed class Listener : IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
        {
            private readonly UnityAdsNetwork _network;

            public Listener(UnityAdsNetwork network) => _network = network;

            public void OnInitializationComplete() => _network.OnInitialized();

            public void OnInitializationFailed(UnityAdsInitializationError error, string message) =>
                _network._warn($"Unity Ads initialization failed: {error} {message}");

            public void OnUnityAdsAdLoaded(string placementId) => _network.OnLoaded(placementId);

            public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message) =>
                _network._warn($"Unity Ads failed to load {placementId}: {error} {message}");

            public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState state) =>
                _network.OnShowComplete(placementId, state == UnityAdsShowCompletionState.COMPLETED);

            public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message) =>
                _network.OnShowFailed(placementId, $"{error} {message}");

            public void OnUnityAdsShowStart(string placementId)
            {
            }

            public void OnUnityAdsShowClick(string placementId)
            {
            }
        }
    }
}
