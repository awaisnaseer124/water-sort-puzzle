using System;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSort.Game.UI
{
    public sealed class HomeScreen : UIScreen
    {
        [Tooltip("The full-screen button behind everything ('TAP TO START').")]
        [SerializeField] private Button _tapToStart;
        [SerializeField] private Button _settings;
        [SerializeField] private Button _backgrounds;
        [SerializeField] private Button _bottleSkins;
        [SerializeField] private Button _moreGames;
        [SerializeField] private Button _removeAds;

        public event Action PlayClicked;
        public event Action SettingsClicked;
        public event Action BackgroundsClicked;
        public event Action BottleSkinsClicked;
        public event Action MoreGamesClicked;
        public event Action RemoveAdsClicked;

        public void Init()
        {
            Bind(_tapToStart, () => PlayClicked?.Invoke());
            Bind(_settings, () => SettingsClicked?.Invoke());
            Bind(_backgrounds, () => BackgroundsClicked?.Invoke());
            Bind(_bottleSkins, () => BottleSkinsClicked?.Invoke());
            Bind(_moreGames, () => MoreGamesClicked?.Invoke());
            Bind(_removeAds, () => RemoveAdsClicked?.Invoke());
        }

        public void SetRemoveAdsVisible(bool visible) => _removeAds.gameObject.SetActive(visible);
    }
}
