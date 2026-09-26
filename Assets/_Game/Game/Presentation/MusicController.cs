using System;
using ColorSort.Core.Progression;
using UnityEngine;

namespace ColorSort.Game.Presentation
{
    public sealed class MusicController : MonoBehaviour
    {
        [SerializeField] private AudioSource _music;

        private PlayerProfile _profile;

        public void Init(PlayerProfile profile)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _profile.Changed += Apply;
            Apply();
        }

        private void OnDestroy()
        {
            if (_profile != null)
                _profile.Changed -= Apply;
        }

        private void Apply()
        {
            bool enabled = _profile.Settings.MusicEnabled;
            if (enabled && !_music.isPlaying)
                _music.Play();
            else if (!enabled && _music.isPlaying)
                _music.Stop();
        }
    }
}
