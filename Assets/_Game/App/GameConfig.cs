using ColorSort.Game.Data;
using UnityEngine;

namespace ColorSort.App
{
    [CreateAssetMenu(menuName = "ColorSort/Game Config", fileName = ResourceName)]
    public sealed class GameConfig : ScriptableObject
    {
        public const string ResourceName = "GameConfig";

        [SerializeField] private LevelCatalog _levelCatalog;

        [Header("Ads")]
        [Tooltip("Editor only: use the always-ready simulated network instead of real SDKs.")]
        [SerializeField] private bool _simulateAdsInEditor = true;

        [Tooltip("Test ads are always used in the editor and development builds; this forces them in release builds too.")]
        [SerializeField] private bool _forceTestAds;

        [SerializeField, Min(0f)] private float _startupGraceSeconds = 30f;
        [SerializeField, Min(0f)] private float _minSecondsBetweenInterstitials = 60f;

        [Header("Unity Ads")]
        [SerializeField] private string _unityAdsGameIdAndroid = "4479419";
        [SerializeField] private string _unityAdsGameIdIos = "";
        [SerializeField] private string _unityAdsInterstitial = "video";
        [SerializeField] private string _unityAdsRewarded = "rewardedVideo";

        [Header("Purchases")]
        [Tooltip("Editor only: purchases succeed instantly instead of going through Unity IAP's fake store.")]
        [SerializeField] private bool _simulatePurchasesInEditor = true;

        [Header("Analytics")]
        [SerializeField] private bool _logAnalytics = true;

        public LevelCatalog LevelCatalog => _levelCatalog;
        public bool SimulateAds => Application.isEditor && _simulateAdsInEditor;
        public bool SimulatePurchases => Application.isEditor && _simulatePurchasesInEditor;

        public bool UseTestAds => _forceTestAds || Debug.isDebugBuild;

        public float StartupGraceSeconds => _startupGraceSeconds;
        public float MinSecondsBetweenInterstitials => _minSecondsBetweenInterstitials;
        public string UnityAdsGameId => Application.platform == RuntimePlatform.IPhonePlayer ? _unityAdsGameIdIos : _unityAdsGameIdAndroid;
        public string UnityAdsInterstitial => _unityAdsInterstitial;
        public string UnityAdsRewarded => _unityAdsRewarded;
        public bool LogAnalytics => _logAnalytics;

#if UNITY_EDITOR
        public void EditorSetLevelCatalog(LevelCatalog catalog) => _levelCatalog = catalog;
#endif
    }
}
