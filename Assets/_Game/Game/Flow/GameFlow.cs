using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ColorSort.Core.Board;
using ColorSort.Core.Progression;
using ColorSort.Core.Solving;
using ColorSort.Core.Store;
using ColorSort.Game.Board;
using ColorSort.Game.Data;
using ColorSort.Game.UI;
using ColorSort.Services.Ads;
using ColorSort.Services.Analytics;
using ColorSort.Services.Purchasing;
using DG.Tweening;
using UnityEngine;

namespace ColorSort.Game.Flow
{
    public sealed class FlowScreens
    {
        public HomeScreen Home { get; set; }
        public ModeSelectScreen Modes { get; set; }
        public LevelSelectScreen Levels { get; set; }
        public HudScreen Hud { get; set; }
        public LevelCompleteScreen Complete { get; set; }
        public SettingsScreen Settings { get; set; }
        public SkinShopScreen Backgrounds { get; set; }
        public SkinShopScreen BottleSkins { get; set; }
        public Toast Toast { get; set; }
    }

    public enum FlowState
    {
        Home,
        ModeSelect,
        LevelSelect,
        Playing,
        LevelComplete,
    }

    public sealed class GameFlow : IDisposable
    {
        public const int RewardPerBottle = 20;
        public const int VideoCoinReward = 50;
        private const float CompleteScreenDelay = 1f;
        private const float RestartAfterLossDelay = 2f;

        private readonly FlowScreens _ui;
        private readonly LevelController _level;
        private readonly PlayerProfile _profile;
        private readonly IAdService _ads;
        private readonly IPurchaseService _purchases;
        private readonly IAnalyticsService _analytics;
        private readonly string _moreGamesUrl;
        private readonly List<Action> _unsubscribe = new List<Action>();

        private int _currentLevel;
        private int _baseReward;
        private Tween _pending;
        private bool _findingHint;

        public GameFlow(FlowScreens ui, LevelController level, PlayerProfile profile, IAdService ads,
            IPurchaseService purchases, IAnalyticsService analytics, string moreGamesUrl)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _level = level ? level : throw new ArgumentNullException(nameof(level));
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _ads = ads ?? throw new ArgumentNullException(nameof(ads));
            _purchases = purchases ?? throw new ArgumentNullException(nameof(purchases));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            _moreGamesUrl = moreGamesUrl;
        }

        public FlowState State { get; private set; }

        private GameMode Mode => _profile.Progress.SelectedMode;

        public void Start()
        {
            Subscribe();
            HideAll();
            LoadLevel(_profile.Progress.CurrentLevel(Mode)); // the board is visible behind the home screen
            ShowHome();
        }

        public void Tick()
        {
            if (State == FlowState.Playing && _level.IsTimed)
                _ui.Hud.SetTime(_level.RemainingSeconds);
        }

        public void Dispose()
        {
            _pending?.Kill();
            foreach (Action unsubscribe in _unsubscribe)
                unsubscribe();
            _unsubscribe.Clear();
        }

        private void ShowHome()
        {
            State = FlowState.Home;
            _level.PauseClock();
            HideMain();
            _ui.Home.SetRemoveAdsVisible(!_profile.AdsRemoved);
            _ui.Home.Show();
        }

        private void ShowModes()
        {
            State = FlowState.ModeSelect;
            HideMain();
            _ui.Modes.Show();
        }

        private void ShowLevels()
        {
            State = FlowState.LevelSelect;
            HideMain();
            _ui.Levels.Show(_level.Catalog.Count, i => _profile.Progress.IsUnlocked(Mode, i), _profile.Progress.CurrentLevel(Mode));
        }

        private void Play(int levelIndex)
        {
            _pending?.Kill();
            _profile.Progress.SetCurrentLevel(Mode, levelIndex);
            LoadLevel(levelIndex);
            State = FlowState.Playing;
            HideMain();
            _ui.Hud.Show();
            _level.StartClock();
            Log(AnalyticsEvents.LevelStart);
        }

        private void LoadLevel(int levelIndex)
        {
            _currentLevel = levelIndex;
            bool timed = Mode != GameMode.Moves;
            _level.StartLevel(CatalogIndex(levelIndex), timed);
            _ui.Hud.ShowLevel(levelIndex + 1, _level.Level.Stars, showMoves: Mode != GameMode.Timed, showTimer: timed);
        }

        private int CatalogIndex(int levelIndex)
        {
            int count = _level.Catalog.Count;
            return levelIndex < count ? levelIndex : UnityEngine.Random.Range(count / 2, count);
        }

        private void HideMain()
        {
            _ui.Home.Hide();
            _ui.Modes.Hide();
            _ui.Levels.Hide();
            _ui.Hud.Hide();
            _ui.Complete.Hide();
        }

        private void HideAll()
        {
            HideMain();
            _ui.Settings.Hide();
            _ui.Backgrounds.Hide();
            _ui.BottleSkins.Hide();
        }

        private void OnMovesChanged(int moves)
        {
            _ui.Hud.SetMoves(moves, _level.CurrentStars);
            RefreshUndo();
        }

        private void RefreshUndo() => _ui.Hud.SetUndoAvailable(State == FlowState.Playing && _level.CanUndo);

        private void Undo()
        {
            if (State == FlowState.Playing && _level.Undo())
                Log(AnalyticsEvents.Undo, ("moves", _level.Session.Moves));
        }

        private async void RequestHint()
        {
            if (State != FlowState.Playing || _findingHint)
                return;

            _findingHint = true;
            try
            {
                int version = _level.BoardVersion;
                BoardState snapshot = _level.SnapshotBoard();
                Hint hint = await Task.Run(() => HintFinder.Find(snapshot));

                if (State != FlowState.Playing || version != _level.BoardVersion)
                    return;

                Log(AnalyticsEvents.Hint, ("result", hint.Kind.ToString()));
                switch (hint.Kind)
                {
                    case HintKind.DeadEnd:
                        _ui.Toast.Show("No solution from here. Undo or restart");
                        return;
                    case HintKind.Unknown:
                        _ui.Toast.Show("Try undoing a few moves");
                        return;
                    case HintKind.AlreadySolved:
                        return;
                }

                _ads.ShowRewarded(AdPlacements.Hint, result =>
                {
                    if (result != AdResult.Completed)
                        _ui.Toast.Show(AdMessages.For(result));
                    else if (State == FlowState.Playing && version == _level.BoardVersion)
                        _level.ShowHint(hint.From, hint.To);
                });
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                _findingHint = false;
            }
        }

        private void OnLevelWon(int stars)
        {
            State = FlowState.LevelComplete;
            _baseReward = _level.Session.Bottles.Count(b => b.IsComplete) * RewardPerBottle;

            using (_profile.BatchChanges())
            {
                _profile.Progress.RecordWin(Mode, _currentLevel, stars);
                _profile.Wallet.Add(_baseReward);
            }

            Log(AnalyticsEvents.LevelComplete, ("moves", _level.Session.Moves), ("stars", stars), ("optimal", _level.Level.OptimalMoves));
            _pending = DOVirtual.DelayedCall(CompleteScreenDelay, () =>
            {
                HideMain();
                _ui.Complete.Show(stars, _baseReward);
                _ads.TryShowInterstitial(AdPlacements.LevelComplete);
            });
        }

        private void OnLevelLost()
        {
            Log(AnalyticsEvents.LevelFail, ("moves", _level.Session.Moves));
            _ui.Toast.Show("Time's up!");
            _pending = DOVirtual.DelayedCall(RestartAfterLossDelay, () => Play(_currentLevel));
        }

        private void NextLevel() => Play(_currentLevel + 1); // unlocked by RecordWin

        private void Restart()
        {
            _ads.TryShowInterstitial(AdPlacements.Restart);
            Play(_currentLevel);
        }

        private void LeaveToHome()
        {
            _ads.TryShowInterstitial(AdPlacements.Home);
            ShowHome();
        }

        private void WatchForExtraBottle() =>
            _ads.ShowRewarded(AdPlacements.ExtraBottle, result =>
            {
                if (result != AdResult.Completed)
                {
                    _ui.Toast.Show(AdMessages.For(result));
                    return;
                }
                if (State != FlowState.Playing)
                    return; // the level ended while the ad was open
                _level.AddExtraBottle();
                _ui.Hud.SetExtraBottleAvailable(false);
            });

        private void OnWheelStopped(int multiplier) =>
            _ads.ShowRewarded(AdPlacements.DoubleReward, result =>
            {
                if (result == AdResult.Completed)
                {
                    _ui.Complete.OfferCollect();
                    return;
                }
                _ui.Toast.Show(AdMessages.For(result));
                _ui.Complete.ResumeWheel();
            });

        private void CollectBonus(int multiplier)
        {
            _profile.Wallet.Add(_baseReward * multiplier);
            NextLevel();
        }

        private void WatchForCoins() =>
            _ads.ShowRewarded(AdPlacements.FreeCoins, result =>
            {
                if (result == AdResult.Completed)
                    _profile.Wallet.Add(VideoCoinReward);
                else
                    _ui.Toast.Show(AdMessages.For(result));
            });

        private void BuyRemoveAds() =>
            _purchases.Buy(StoreCatalog.RemoveAds, result =>
            {
                _ui.Toast.Show(PurchaseMessage(result));
                _ui.Home.SetRemoveAdsVisible(!_profile.AdsRemoved);
            });

        private static string PurchaseMessage(PurchaseResult result)
        {
            switch (result)
            {
                case PurchaseResult.Success: return "Thank you for your purchase!";
                case PurchaseResult.Deferred: return "Purchase pending approval";
                case PurchaseResult.NotAvailable: return "Store not available right now";
                case PurchaseResult.Failed: return "Purchase failed";
                default: return null; // Cancelled: the player backed out
            }
        }

        private void OpenMoreGames()
        {
            if (!string.IsNullOrEmpty(_moreGamesUrl))
                Application.OpenURL(_moreGamesUrl);
        }

        private void Subscribe()
        {
            On(h => _ui.Home.PlayClicked += h, h => _ui.Home.PlayClicked -= h, ShowModes);
            On(h => _ui.Home.SettingsClicked += h, h => _ui.Home.SettingsClicked -= h, _ui.Settings.Show);
            On(h => _ui.Home.BackgroundsClicked += h, h => _ui.Home.BackgroundsClicked -= h, _ui.Backgrounds.Show);
            On(h => _ui.Home.BottleSkinsClicked += h, h => _ui.Home.BottleSkinsClicked -= h, _ui.BottleSkins.Show);
            On(h => _ui.Home.MoreGamesClicked += h, h => _ui.Home.MoreGamesClicked -= h, OpenMoreGames);
            On(h => _ui.Home.RemoveAdsClicked += h, h => _ui.Home.RemoveAdsClicked -= h, BuyRemoveAds);

            On<GameMode>(h => _ui.Modes.ModeChosen += h, h => _ui.Modes.ModeChosen -= h, mode =>
            {
                _profile.Progress.SelectedMode = mode;
                ShowLevels();
            });
            On(h => _ui.Modes.Closed += h, h => _ui.Modes.Closed -= h, ShowHome);

            On<int>(h => _ui.Levels.LevelChosen += h, h => _ui.Levels.LevelChosen -= h, Play);
            On(h => _ui.Levels.Closed += h, h => _ui.Levels.Closed -= h, ShowModes);

            On(h => _ui.Hud.HomeClicked += h, h => _ui.Hud.HomeClicked -= h, LeaveToHome);
            On(h => _ui.Hud.RestartClicked += h, h => _ui.Hud.RestartClicked -= h, Restart);
            On(h => _ui.Hud.ExtraBottleClicked += h, h => _ui.Hud.ExtraBottleClicked -= h, WatchForExtraBottle);
            On(h => _ui.Hud.UndoClicked += h, h => _ui.Hud.UndoClicked -= h, Undo);
            On(h => _ui.Hud.HintClicked += h, h => _ui.Hud.HintClicked -= h, RequestHint);

            On(h => _ui.Complete.ContinueClicked += h, h => _ui.Complete.ContinueClicked -= h, NextLevel);
            On<int>(h => _ui.Complete.SpinStopped += h, h => _ui.Complete.SpinStopped -= h, OnWheelStopped);
            On<int>(h => _ui.Complete.CollectClicked += h, h => _ui.Complete.CollectClicked -= h, CollectBonus);
            On(h => _ui.Complete.MoreGamesClicked += h, h => _ui.Complete.MoreGamesClicked -= h, OpenMoreGames);
            On(h => _ui.Complete.BottleSkinsClicked += h, h => _ui.Complete.BottleSkinsClicked -= h, _ui.BottleSkins.Show);
            On(h => _ui.Complete.SettingsClicked += h, h => _ui.Complete.SettingsClicked -= h, _ui.Settings.Show);
            On(h => _ui.Complete.BackgroundsClicked += h, h => _ui.Complete.BackgroundsClicked -= h, _ui.Backgrounds.Show);

            On(h => _ui.Settings.Closed += h, h => _ui.Settings.Closed -= h, _ui.Settings.Hide);
            On(h => _ui.Backgrounds.Closed += h, h => _ui.Backgrounds.Closed -= h, _ui.Backgrounds.Hide);
            On(h => _ui.Backgrounds.WatchVideoClicked += h, h => _ui.Backgrounds.WatchVideoClicked -= h, WatchForCoins);
            On(h => _ui.BottleSkins.Closed += h, h => _ui.BottleSkins.Closed -= h, _ui.BottleSkins.Hide);
            On(h => _ui.BottleSkins.WatchVideoClicked += h, h => _ui.BottleSkins.WatchVideoClicked -= h, WatchForCoins);

            On<int>(h => _level.MovesChanged += h, h => _level.MovesChanged -= h, OnMovesChanged);
            On(h => _level.BottleTapped += h, h => _level.BottleTapped -= h, _ui.Hud.HideInstruction);
            On(h => _level.InputReady += h, h => _level.InputReady -= h, RefreshUndo);
            On<int>(h => _level.LevelWon += h, h => _level.LevelWon -= h, OnLevelWon);
            On(h => _level.LevelLost += h, h => _level.LevelLost -= h, OnLevelLost);
        }

        private void On(Action<Action> add, Action<Action> remove, Action handler)
        {
            add(handler);
            _unsubscribe.Add(() => remove(handler));
        }

        private void On<T>(Action<Action<T>> add, Action<Action<T>> remove, Action<T> handler)
        {
            add(handler);
            _unsubscribe.Add(() => remove(handler));
        }

        private void Log(string eventName, params (string key, object value)[] extra)
        {
            var parameters = new Dictionary<string, object> { ["level"] = _currentLevel + 1, ["mode"] = Mode.ToString() };
            foreach ((string key, object value) in extra)
                parameters[key] = value;
            _analytics.Log(eventName, parameters);
        }
    }
}
