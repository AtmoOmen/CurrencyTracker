using System.Collections.Generic;

namespace CurrencyTracker.Manager.Tasks;

public class FrameThrottler<T>
{
    private readonly Dictionary<T, long> throttlers = new();
    private long SFrameCount => (long)P.PluginInterface.UiBuilder.FrameCount;

    public IReadOnlyCollection<T> ThrottleNames => throttlers.Keys;

    public bool Throttle(T name, int frames = 60, bool reThrottle = false)
    {
        if (!throttlers.ContainsKey(name))
        {
            throttlers[name] = SFrameCount + frames;
            return true;
        }

        if (SFrameCount > throttlers[name])
        {
            throttlers[name] = SFrameCount + frames;
            return true;
        }

        if (reThrottle) throttlers[name] = SFrameCount + frames;
        return false;
    }

    public bool Check(T name)
    {
        if (!throttlers.ContainsKey(name)) return true;
        return SFrameCount > throttlers[name];
    }

    public long GetRemainingTime(T name, bool allowNegative = false)
    {
        if (!throttlers.ContainsKey(name)) return allowNegative ? -SFrameCount : 0;
        var ret = throttlers[name] - SFrameCount;
        if (allowNegative)
            return ret;
        return ret > 0 ? ret : 0;
    }
}
