using ColorSort.Services.Haptics;
using MoreMountains.NiceVibrations;

namespace ColorSort.Platform.Haptics
{
    public sealed class NiceVibrationsHaptics : IHapticsService
    {
        public void Play(HapticKind kind) => MMVibrationManager.Haptic(ToNative(kind));

        private static HapticTypes ToNative(HapticKind kind)
        {
            switch (kind)
            {
                case HapticKind.Selection: return HapticTypes.Selection;
                case HapticKind.Light: return HapticTypes.LightImpact;
                case HapticKind.Medium: return HapticTypes.MediumImpact;
                case HapticKind.Heavy: return HapticTypes.HeavyImpact;
                case HapticKind.Success: return HapticTypes.Success;
                case HapticKind.Warning: return HapticTypes.Warning;
                case HapticKind.Failure: return HapticTypes.Failure;
                default: return HapticTypes.None;
            }
        }
    }
}
