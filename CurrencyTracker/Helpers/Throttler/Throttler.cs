using System;

namespace CurrencyTracker.Helpers.Throttler;

public class Throttler<T> : ThrottlerBase<T> where T : notnull
{
    protected override long GetCurrentTime() => Environment.TickCount64;

    public bool Throttle(T name, int milliseconds = 500, bool rethrottle = false) =>
        base.Throttle(name, milliseconds, rethrottle);
}
