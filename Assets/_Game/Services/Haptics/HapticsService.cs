using System;

namespace ColorSort.Services.Haptics
{
    public enum HapticKind
    {
        Selection,
        Light,
        Medium,
        Heavy,
        Success,
        Warning,
        Failure,
    }

    public interface IHapticsService
    {
        void Play(HapticKind kind);
    }

    public sealed class NullHapticsService : IHapticsService
    {
        public void Play(HapticKind kind)
        {
        }
    }

    public sealed class SettingsGatedHaptics : IHapticsService
    {
        private readonly IHapticsService _inner;
        private readonly Func<bool> _enabled;

        public SettingsGatedHaptics(IHapticsService inner, Func<bool> enabled)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _enabled = enabled ?? throw new ArgumentNullException(nameof(enabled));
        }

        public void Play(HapticKind kind)
        {
            if (_enabled())
                _inner.Play(kind);
        }
    }
}
