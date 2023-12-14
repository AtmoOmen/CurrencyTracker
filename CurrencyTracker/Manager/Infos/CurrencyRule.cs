namespace CurrencyTracker.Manager.Infos
{
    public class CurrencyRule
    {
        public bool RegionRulesMode { get; set; } = false; // false - Blacklist ; true - Whitelist
        public List<uint> RestrictedAreas { get; set; } = new(); // Area IDs
    }
}
