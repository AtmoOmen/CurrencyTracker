using System.Collections.Generic;

namespace CurrencyTracker.Manager.Tasks;

public static class EzThrottler
{
    internal static EzThrottler<string> Throttler = new();

    public static IReadOnlyCollection<string> ThrottleNames => Throttler.ThrottleNames;

    public static bool Throttle(string name, int milliseconds = 500, bool reThrottle = false)
    {
        return Throttler.Throttle(name, milliseconds, reThrottle);
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
