using System;
using ColorSort.Core.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSort.Game.UI
{
    public sealed class SettingsScreen : UIScreen
    {
        [SerializeField] private Button _musicOn;
        [SerializeField] private Button _musicOff;
        [SerializeField] private Button _vibrationOn;
        [SerializeField] private Button _vibrationOff;
        [SerializeField] private Button _close;

        private UserSettings _settings;

        public event Action Closed;

        public void Init(UserSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Bind(_musicOn, ToggleMusic);
            Bind(_musicOff, ToggleMusic);
            Bind(_vibrationOn, ToggleVibration);
            Bind(_vibrationOff, ToggleVibration);
            Bind(_close, () => Closed?.Invoke());
        }

        public override void Show()
        {
            Refresh();
            base.Show();
        }

        private void ToggleMusic()
        {
            _settings.MusicEnabled = !_settings.MusicEnabled;
            Refresh();
        }

        private void ToggleVibration()
        {
            _settings.VibrationEnabled = !_settings.VibrationEnabled;
            Refresh();
        }

        private void Refresh()
        {
            _musicOn.gameObject.SetActive(_settings.MusicEnabled);
            _musicOff.gameObject.SetActive(!_settings.MusicEnabled);
            _vibrationOn.gameObject.SetActive(_settings.VibrationEnabled);
            _vibrationOff.gameObject.SetActive(!_settings.VibrationEnabled);
        }
    }
}
