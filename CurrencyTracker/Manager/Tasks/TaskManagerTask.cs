using System;

namespace CurrencyTracker.Manager.Tasks;

public record TaskManagerTask
{
    internal Func<bool?> Action;
    internal int TimeLimitMS;
    internal bool AbortOnTimeout;
    internal string? Name;

    internal TaskManagerTask(Func<bool?> action, int timeLimitMS, bool abortOnTimeout, string name)
    {
        Action = action;
        TimeLimitMS = timeLimitMS;
        AbortOnTimeout = abortOnTimeout;
        Name = name; // ?? new StackTrace().GetFrames().Select(x => x.GetMethod()?.Name ?? "<unknown>").Join(" <- ");
    }
}
