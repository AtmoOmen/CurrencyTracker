using System.Collections.Generic;
using IntervalUtility;

namespace CurrencyTracker.Infos;

public class CurrencyRule
{
    /// <summary>
    /// false - Blacklist; true - Whitelist
    /// </summary>
    public bool RegionRulesMode { get; set; }

    /// <summary>
    /// Territory Type
    /// </summary>
    public HashSet<uint> RestrictedAreas { get; set; } = [];

    // Character ID / Retainer ID - ulong.ToString; (Premium)Saddle Bag - Character ID_(P)SB
    public Dictionary<string, List<Interval<int>>> AlertedAmountIntervals { get; set; } = [];

    public Dictionary<string, List<Interval<int>>> AlertedChangeIntervals { get; set; } = [];
}
