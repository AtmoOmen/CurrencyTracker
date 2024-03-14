using System;
using System.Collections.Generic;

namespace CurrencyTracker.Manager.Tasks;

public class EzThrottler<T>
{
    private readonly Dictionary<T, long> throttlers = new();
    public IReadOnlyCollection<T> ThrottleNames => throttlers.Keys;

    public bool Throttle(T name, int milliseconds = 500, bool reThrottle = false)
    {
        if (!throttlers.ContainsKey(name))
        {
            throttlers[name] = Environment.TickCount64 + milliseconds;
            return true;
        }

        if (Environment.TickCount64 > throttlers[name])
        {
            throttlers[name] = Environment.TickCount64 + milliseconds;
            return true;
        }

        if (reThrottle) throttlers[name] = Environment.TickCount64 + milliseconds;
        return false;
    }

    public bool Check(T name)
    {
        if (!throttlers.ContainsKey(name)) return true;
        return Environment.TickCount64 > throttlers[name];
    }

    public long GetRemainingTime(T name, bool allowNegative = false)
    {
        if (!throttlers.ContainsKey(name)) return allowNegative ? -Environment.TickCount64 : 0;
        var ret = throttlers[name] - Environment.TickCount64;
        if (allowNegative)
            return ret;
        return ret > 0 ? ret : 0;
    }
}
