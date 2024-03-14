using System.Collections.Generic;

namespace CurrencyTracker.Manager.Tasks;

public static class FrameThrottler
{
    internal static FrameThrottler<string> Throttler = new();

    public static IReadOnlyCollection<string> ThrottleNames => Throttler.ThrottleNames;

    public static bool Throttle(string name, int frames = 60, bool reThrottle = false)
    {
        return Throttler.Throttle(name, frames, reThrottle);
    }

    public static bool Check(string name)
    {
        return Throttler.Check(name);
    }

    public static long GetRemainingTime(string name, bool allowNegative = false)
    {
        return Throttler.GetRemainingTime(name, allowNegative);
    }
}
