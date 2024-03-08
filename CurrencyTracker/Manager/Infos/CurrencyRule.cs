using System.Collections.Generic;
using IntervalUtility;

namespace CurrencyTracker.Manager.Infos;

public class CurrencyRule
{
    public bool RegionRulesMode { get; set; } = false;       // false - Blacklist ; true - Whitelist
    public List<uint> RestrictedAreas { get; set; } = new(); // Area IDs

    // Character ID / Retainer ID - ulong.ToString; (Premium)Saddle Bag - Character ID_(P)SB
    public Dictionary<string, List<Interval<int>>> AlertedAmountIntervals { get; set; } = new();
    public Dictionary<string, List<Interval<int>>> AlertedChangeIntervals { get; set; } = new();
}
