using System;
using ColorSort.Core.Board;
using ColorSort.Game.Board;
using ColorSort.Services.Haptics;
using UnityEngine;

namespace ColorSort.Game.Presentation
{
    public sealed class GameFeedback : MonoBehaviour
    {
        [SerializeField] private AudioSource _pour;
        [SerializeField] private AudioSource _reject;
        [SerializeField] private GameObject _levelCompleteParticles;
        [SerializeField] private GameObject _bottleCompleteParticles;
        [SerializeField, Min(0f)] private float _bottleParticleSeconds = 3f;

        private LevelController _level;
        private IHapticsService _haptics;

        public void Init(LevelController level, IHapticsService haptics)
        {
            _level = level ? level : throw new ArgumentNullException(nameof(level));
            _haptics = haptics ?? throw new ArgumentNullException(nameof(haptics));

            _level.LevelStarted += OnLevelStarted;
            _level.PourStarted += OnPourStarted;
            _level.PourFinished += OnPourFinished;
            _level.PourRejected += OnPourRejected;
            _level.BottleCompleted += OnBottleCompleted;
            _level.LevelWon += OnLevelWon;
        }

        private void OnDestroy()
        {
            if (_level == null)
                return;
            _level.LevelStarted -= OnLevelStarted;
            _level.PourStarted -= OnPourStarted;
            _level.PourFinished -= OnPourFinished;
            _level.PourRejected -= OnPourRejected;
            _level.BottleCompleted -= OnBottleCompleted;
            _level.LevelWon -= OnLevelWon;
        }

        private void OnLevelStarted(Data.LevelDefinition level)
        {
            CancelInvoke();
            _levelCompleteParticles.SetActive(false);
            _bottleCompleteParticles.SetActive(false);
        }

        private void OnPourStarted()
        {
            if (!_pour.isPlaying)
                _pour.Play();
        }

        private void OnPourFinished() => _pour.Stop();

        private void OnPourRejected(PourCheck reason)
        {
            if (!_reject.isPlaying)
                _reject.Play();
            _haptics.Play(HapticKind.Heavy);
        }

        private void OnBottleCompleted(int bottleIndex)
        {
            CancelInvoke(nameof(HideBottleParticles));
            _bottleCompleteParticles.SetActive(true);
            _haptics.Play(HapticKind.Success);
            Invoke(nameof(HideBottleParticles), _bottleParticleSeconds);
        }

        private void HideBottleParticles() => _bottleCompleteParticles.SetActive(false);

        private void OnLevelWon(int stars) => _levelCompleteParticles.SetActive(true);
    }
}
