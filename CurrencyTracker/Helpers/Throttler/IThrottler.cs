using System.Collections.Generic;

namespace CurrencyTracker.Helpers.Throttler;

public interface IThrottler<T> where T : notnull
{
    IReadOnlyCollection<T> ThrottleNames { get; }
    bool Throttle(T name, long duration, bool rethrottle = false);
    bool Check(T name);
    long GetRemainingTime(T name, bool allowNegative = false);
}
