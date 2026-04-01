using System.Numerics;
using CurrencyTracker.Infos;
using CurrencyTracker.Trackers.Components;
using Dalamud.Configuration;
using Newtonsoft.Json;

namespace CurrencyTracker.Internal;

[Serializable]
public class PluginConfig : IPluginConfiguration
{
    public int                 Version                { get; set; } = 0;
    public bool                FirstOpen              { get; set; } = true;
    public List<CharacterInfo> CurrentActiveCharacter { get; set; } = [];

    public Dictionary<uint, string> PresetCurrencies { get; set; } = [];
    public Dictionary<uint, string> CustomCurrencies { get; set; } = [];

    public List<uint>         OrderedOptions           { get; set; } = [];
    public bool               ReverseSort              { get; set; }
    public string             SelectedLanguage         { get; set; } = string.Empty;
    public int                MaxBackupFilesCount      { get; set; } = 10;
    public bool               AutoSaveMessage          { get; set; }
    public int                AutoSaveMode             { get; set; }       // 0 - Save Current ; 1 - Save All
    public int                AutoSaveInterval         { get; set; } = 60; // Minutes
    public uint               ServerBarDisplayCurrency { get; set; } = 1;
    public ServerBarCycleMode ServerBarCycleMode       { get; set; } = 0;
    public bool               AlertNotificationChat    { get; set; }
    public int                RecordsPerPage           { get; set; } = 20;
    public bool               ChangeTextColoring       { get; set; } = true;
    public Vector4            PositiveChangeColor      { get; set; } = new(0.0f, 1.0f, 0.0f, 1.0f);
    public Vector4            NegativeChangeColor      { get; set; } = new(1.0f, 0.0f, 0.0f, 1.0f);
    public int                ChildWidthOffset         { get; set; }

    public int ExportDataFileType { get; set; } = 0;

    // Content ID - Retainer ID : Retainer Name
    public Dictionary<ulong, Dictionary<ulong, string>> CharacterRetainers { get; set; } = [];

    public Dictionary<string, bool> ColumnsVisibility { get; set; } = new()
    {
        { "Order", true },
        { "Time", true },
        { "Amount", true },
        { "Change", true },
        { "Location", true },
        { "Note", true },
        { "Checkbox", true }
    };

    public Dictionary<string, bool> ComponentEnabled { get; set; } = new()
    {
        { "AutoSave", false },
        { "ServerBar", false },
        { "CurrencyAddonExpand", true },
        { "MoneyAddonExpand", false },
        { "DutyRewards", true },
        { "Exchange", true },
        { "FateRewards", true },
        { "GoldSaucer", true },
        { "LetterAttachments", true },
        { "IslandSanctuary", true },
        { "MobDrops", true },
        { "PremiumSaddleBag", true },
        { "QuestRewards", true },
        { "Retainer", true },
        { "SaddleBag", true },
        { "SpecialExchange", true },
        { "TeleportCosts", true },
        { "Trade", true },
        { "TripleTriad", true },
        { "WarpCosts", true }
    };

    public Dictionary<string, bool> ComponentProp { get; set; } = new()
    {
        // DutyRewards
        { "RecordContentName", true },
        // TeleportCosts
        { "RecordDesAetheryteName", false },
        { "RecordDesAreaName", true }
    };

    public Dictionary<string, string>     CustomNoteContents { get; set; } = [];
    public Dictionary<uint, CurrencyRule> CurrencyRules      { get; set; } = [];

    [JsonIgnore]
    public Dictionary<uint, string> AllCurrencies
    {
        get
        {
            if (field == null || IsUpdated)
                field = GetAllCurrencies();

            return field;
        }
    }
    
    private static bool IsUpdated = true;

    private Dictionary<uint, string> GetAllCurrencies()
    {
        DService.Instance().Log.Debug("Successfully reacquire all currencies");

        var hasDuplicateCurrency = false;
        foreach (var currencyId in PresetCurrencies.Keys)
            hasDuplicateCurrency |= CustomCurrencies.Remove(currencyId);

        if (hasDuplicateCurrency) Save();

        IsUpdated = false;

        return PresetCurrencies.Concat(CustomCurrencies)
                               .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    public void ReplacePresetCurrencies(IEnumerable<KeyValuePair<uint, string>> currencies)
    {
        PresetCurrencies = currencies.ToDictionary(kv => kv.Key, kv => kv.Value);
        MarkCurrenciesUpdated();
    }

    public bool TryAddPresetCurrency(uint currencyId, string currencyName)
    {
        var isAdded = PresetCurrencies.TryAdd(currencyId, currencyName);
        if (isAdded) MarkCurrenciesUpdated();

        return isAdded;
    }

    public bool TryAddCustomCurrency(uint currencyId, string currencyName)
    {
        var isAdded = CustomCurrencies.TryAdd(currencyId, currencyName);
        if (isAdded) MarkCurrenciesUpdated();

        return isAdded;
    }

    public bool RemoveCustomCurrency(uint currencyId)
    {
        var isRemoved = CustomCurrencies.Remove(currencyId);
        if (isRemoved) MarkCurrenciesUpdated();

        return isRemoved;
    }

    public bool TryRenameCurrency(uint currencyId, string newName)
    {
        if (PresetCurrencies.ContainsKey(currencyId))
        {
            PresetCurrencies[currencyId] = newName;
            MarkCurrenciesUpdated();
            return true;
        }

        if (!CustomCurrencies.ContainsKey(currencyId)) return false;

        CustomCurrencies[currencyId] = newName;
        MarkCurrenciesUpdated();
        return true;
    }

    private static PluginConfig? InstanceInternal;

    public static PluginConfig Instance()
    {
        if (InstanceInternal != null) return InstanceInternal;

        Reload();

        return InstanceInternal;
    }

    private static void MarkCurrenciesUpdated() =>
        IsUpdated = true;

    internal static void Reload()
    {
        InstanceInternal                  =   DService.Instance().PI.GetPluginConfig() as PluginConfig ?? new();
        InstanceInternal.PresetCurrencies ??= [];
        InstanceInternal.CustomCurrencies ??= [];
        InstanceInternal.Save();
    }

    internal void Save() =>
        DService.Instance().PI.SavePluginConfig(this);
}
