using System;
using ColorSort.Game.Board;
using ColorSort.Game.Data;
using ColorSort.Game.Flow;
using ColorSort.Game.Presentation;
using ColorSort.Game.UI;
using UnityEngine;

namespace ColorSort.App
{
    [DefaultExecutionOrder(-100)]
    public sealed class SceneInstaller : MonoBehaviour
    {
        [SerializeField] private LevelController _level;
        [SerializeField] private SkinCatalog _skins;
        [Tooltip("Store page opened by the cross-promo buttons.")]
        [SerializeField] private string _moreGamesUrl = "https://play.google.com/store/apps/details?id=com.vnstartllc.sort.water";

        [Header("Screens")]
        [SerializeField] private HomeScreen _home;
        [SerializeField] private ModeSelectScreen _modes;
        [SerializeField] private LevelSelectScreen _levels;
        [SerializeField] private HudScreen _hud;
        [SerializeField] private LevelCompleteScreen _complete;
        [SerializeField] private SettingsScreen _settings;
        [SerializeField] private SkinShopScreen _backgrounds;
        [SerializeField] private SkinShopScreen _bottleSkins;
        [SerializeField] private Toast _toast;

        [Header("Presentation")]
        [SerializeField] private CoinsLabel[] _coinLabels = Array.Empty<CoinsLabel>();
        [SerializeField] private BackgroundView _background;
        [SerializeField] private BottleSkinView _bottleSkin;
        [SerializeField] private MusicController _music;
        [SerializeField] private GameFeedback _feedback;

        private GameFlow _flow;

        private void Awake()
        {
            GameServices services = GameBootstrap.Services
                ?? throw new InvalidOperationException("GameBootstrap did not run before the scene loaded.");

            _home.Init();
            _modes.Init();
            _levels.Init();
            _hud.Init();
            _complete.Init();
            _settings.Init(services.Profile.Settings);
            _backgrounds.Init(services.Profile.Backgrounds, services.Profile.Wallet, _skins.Backgrounds, SkinCatalog.UnlockPrice, _toast.Show);
            _bottleSkins.Init(services.Profile.BottleSkins, services.Profile.Wallet, _skins.BottlePreviews, SkinCatalog.UnlockPrice, _toast.Show);

            foreach (CoinsLabel label in _coinLabels)
                label.Init(services.Profile.Wallet);
            _background.Init(services.Profile, _skins);
            _bottleSkin.Init(services.Profile, _skins);
            _music.Init(services.Profile);
            _feedback.Init(_level, services.Haptics);

            var screens = new FlowScreens
            {
                Home = _home,
                Modes = _modes,
                Levels = _levels,
                Hud = _hud,
                Complete = _complete,
                Settings = _settings,
                Backgrounds = _backgrounds,
                BottleSkins = _bottleSkins,
                Toast = _toast,
            };
            _flow = new GameFlow(screens, _level, services.Profile, services.Ads, services.Purchases, services.Analytics, _moreGamesUrl);
        }

        private void Start() => _flow.Start();

        private void Update() => _flow.Tick();

        private void OnDestroy() => _flow?.Dispose();
    }
}
