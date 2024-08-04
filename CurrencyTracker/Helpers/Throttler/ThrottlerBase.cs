using System;
using System.Collections.Generic;

namespace CurrencyTracker.Helpers.Throttler;

public abstract class ThrottlerBase<T> : IThrottler<T> where T : notnull
{
    protected readonly Dictionary<T, long> throttlers = [];
    public IReadOnlyCollection<T> ThrottleNames => throttlers.Keys;

    protected abstract long GetCurrentTime();

    public virtual bool Throttle(T name, long duration, bool rethrottle = false)
    {
        var currentTime = GetCurrentTime();
        if (!throttlers.TryGetValue(name, out var expirationTime) || currentTime > expirationTime || rethrottle)
        {
            throttlers[name] = currentTime + duration;
            return true;
        }

        return false;
    }

    public virtual bool Check(T name) =>
        !throttlers.TryGetValue(name, out var expirationTime) || GetCurrentTime() > expirationTime;

    public virtual long GetRemainingTime(T name, bool allowNegative = false)
    {
        var currentTime = GetCurrentTime();
        if (!throttlers.TryGetValue(name, out var expirationTime))
            return allowNegative ? -currentTime : 0;

        var remainingTime = expirationTime - currentTime;
        return allowNegative ? remainingTime : Math.Max(remainingTime, 0);
    }
}
