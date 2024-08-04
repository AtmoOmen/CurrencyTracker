using CurrencyTracker.Manager;

namespace CurrencyTracker.Helpers.Throttler;

public static class ThrottlerHelper
{
    public static Throttler<string> Throttler { get; } = new();
    public static FrameThrottler<string> FrameThrottler { get; } = new(() => (long)Service.UiBuilder.FrameCount);
}
