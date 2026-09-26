using System;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSort.Game.UI
{
    public static class RewardWheel
    {
        public const float MinValue = 0.1f;
        public const float MaxValue = 0.9f;

        public static int MultiplierAt(float value)
        {
            if (value <= 0.18f) return 2;
            if (value <= 0.44f) return 3;
            if (value <= 0.6f) return 5;
            if (value <= 0.82f) return 3;
            return 2;
        }
    }

    public sealed class LevelCompleteScreen : UIScreen
    {
        [SerializeField] private Image[] _stars = new Image[3];
        [SerializeField] private Sprite _starEarned;
        [SerializeField] private Sprite _starLost;

        [Header("Reward wheel")]
        [SerializeField] private Slider _wheel;
        [SerializeField, Min(0.01f)] private float _wheelSpeed = 1f;
        [SerializeField] private Text _wheelRewardText;
        [SerializeField] private Text _baseRewardText;
        [SerializeField] private Text _collectRewardText;

        [Header("Buttons")]
        [SerializeField] private Button _spin;
        [SerializeField] private Button _continue;
        [SerializeField] private Button _collect;
        [SerializeField] private Button _moreGames;
        [SerializeField] private Button _bottleSkins;
        [SerializeField] private Button _settings;
        [SerializeField] private Button _backgrounds;

        private int _baseReward;
        private int _multiplier = 2;
        private bool _spinning;
        private bool _forward = true;

        public event Action ContinueClicked;
        public event Action<int> SpinStopped;
        public event Action<int> CollectClicked;
        public event Action MoreGamesClicked;
        public event Action BottleSkinsClicked;
        public event Action SettingsClicked;
        public event Action BackgroundsClicked;

        public void Init()
        {
            Bind(_spin, StopWheel);
            Bind(_continue, () => ContinueClicked?.Invoke());
            Bind(_collect, () => CollectClicked?.Invoke(_multiplier));
            Bind(_moreGames, () => MoreGamesClicked?.Invoke());
            Bind(_bottleSkins, () => BottleSkinsClicked?.Invoke());
            Bind(_settings, () => SettingsClicked?.Invoke());
            Bind(_backgrounds, () => BackgroundsClicked?.Invoke());
        }

        public void Show(int stars, int baseReward)
        {
            _baseReward = baseReward;
            for (int i = 0; i < _stars.Length; i++)
                _stars[i].sprite = i < stars ? _starEarned : _starLost;

            _baseRewardText.text = baseReward.ToString();
            _spin.gameObject.SetActive(true);
            _continue.gameObject.SetActive(true);
            _collect.gameObject.SetActive(false);
            _wheel.value = RewardWheel.MinValue;
            _forward = true;
            _spinning = true;
            UpdateMultiplier();
            base.Show();
        }

        public void OfferCollect()
        {
            _spinning = false;
            _spin.gameObject.SetActive(false);
            _continue.gameObject.SetActive(false);
            _collect.gameObject.SetActive(true);
        }

        public void ResumeWheel() => _spinning = true;

        private void StopWheel()
        {
            if (!_spinning)
                return;
            _spinning = false;
            SpinStopped?.Invoke(_multiplier);
        }

        private void Update()
        {
            if (!_spinning)
                return;

            float step = _wheelSpeed * Time.deltaTime;
            float value = _wheel.value + (_forward ? step : -step);
            if (value >= RewardWheel.MaxValue)
            {
                value = RewardWheel.MaxValue;
                _forward = false;
            }
            else if (value <= RewardWheel.MinValue)
            {
                value = RewardWheel.MinValue;
                _forward = true;
            }
            _wheel.value = value;
            UpdateMultiplier();
        }

        private void UpdateMultiplier()
        {
            _multiplier = RewardWheel.MultiplierAt(_wheel.value);
            string amount = (_baseReward * _multiplier).ToString();
            _wheelRewardText.text = amount;
            _collectRewardText.text = amount;
        }
    }
}
