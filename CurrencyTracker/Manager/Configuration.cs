using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CurrencyTracker.Manager.Infos;
using CurrencyTracker.Manager.Tools;
using Dalamud.Configuration;
using Dalamud.Interface.Internal;
using Dalamud.Plugin;
using Newtonsoft.Json;

namespace CurrencyTracker;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public bool FisrtOpen { get; set; } = true;
    public List<CharacterInfo> CurrentActiveCharacter { get; set; } = new();

    public UpdateDictionary<uint, string> PresetCurrencies
    {
        set
        {
            presetCurrencies = value;
            IsUpdated = true;
        }
        get => presetCurrencies;
    }

    public UpdateDictionary<uint, string> CustomCurrencies
    {
        set
        {
            customCurrencies = value;
            IsUpdated = true;
        }
        get => customCurrencies;
    }

    public List<uint> OrderedOptions { get; set; } = new();
    public bool ReverseSort { get; set; } = false;
    public string SelectedLanguage { get; set; } = string.Empty;
    public int MaxBackupFilesCount { get; set; } = 10;
    public bool AutoSaveMessage { get; set; } = false;
    public int AutoSaveMode { get; set; } = 0;      // 0 - Save Current ; 1 - Save All
    public int AutoSaveInterval { get; set; } = 60; // Minutes
    public uint ServerBarDisplayCurrency { get; set; } = 1;
    public int ServerBarDisplayMode { get; set; } = 0; // 0 - Today; 1 - Past 24 Hours; 2 - Past 3 Days; 3 - Past 7 Days;
    public bool AlertNotificationChat { get; set; } = false;
    public int RecordsPerPage { get; set; } = 20;
    public bool ChangeTextColoring { get; set; } = true;
    public Vector4 PositiveChangeColor { get; set; } = new(0.0f, 1.0f, 0.0f, 1.0f);
    public Vector4 NegativeChangeColor { get; set; } = new(1.0f, 0.0f, 0.0f, 1.0f);
    public int ChildWidthOffset { get; set; } = 0;
    public int ExportDataFileType { get; set; } = 0;

    public Dictionary<ulong, Dictionary<ulong, string>> CharacterRetainers { get; set; } =
        new(); // Content ID - Retainer ID : Retainer Name

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
        { "CurrencyUIEdit", true },
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

    public Dictionary<string, string> CustomNoteContents { get; set; } = new();
    public Dictionary<uint, CurrencyRule> CurrencyRules { get; set; } = new();


    [JsonIgnore]
    public static bool IsUpdated = true;

    [JsonIgnore]
    public Dictionary<uint, IDalamudTextureWrap?> AllCurrencyIcons
    {
        get
        {
            if (allCurrencyIcons == null || IsUpdated) GetAllCurrencyIcons();
            return allCurrencyIcons;
        }
    }

    [JsonIgnore]
    public Dictionary<uint, string> AllCurrencies
    {
        get
        {
            if (allCurrencies == null || IsUpdated)
            {
                allCurrencies = GetAllCurrencies();
                GetAllCurrencyIcons();
                allCurrencyID = allCurrencies.Keys.ToArray();
            }

            return allCurrencies;
        }
    }

    [JsonIgnore]
    public uint[] AllCurrencyID
    {
        get
        {
            if (allCurrencies == null || allCurrencyID == null || IsUpdated)
            {
                allCurrencies = GetAllCurrencies();
                GetAllCurrencyIcons();
                allCurrencyID = allCurrencies.Keys.ToArray();
            }

            return allCurrencyID;
        }
    }

    private Dictionary<uint, IDalamudTextureWrap?>? allCurrencyIcons = new();
    private Dictionary<uint, string>? allCurrencies = new();
    private uint[]? allCurrencyID;
    internal UpdateDictionary<uint, string> presetCurrencies = new();
    internal UpdateDictionary<uint, string> customCurrencies = new();

    [NonSerialized]
    private DalamudPluginInterface? pluginInterface;


    public void GetAllCurrencyIcons()
    {
        allCurrencyIcons.Clear();

        foreach (var currency in allCurrencies) allCurrencyIcons.Add(currency.Key, CurrencyInfo.GetIcon(currency.Key));

        IsUpdated = false;

        Service.Log.Debug("Successfully reacquire all currency icons");
    }

    private Dictionary<uint, string> GetAllCurrencies()
    {
        Service.Log.Debug("Successfully reacquire all currencies");
        IsUpdated = false;

        PresetCurrencies.Keys.ToList().ForEach(k => CustomCurrencies.Remove(k));
        Save();

        return PresetCurrencies.Concat(CustomCurrencies)
                               .ToDictionary(kv => kv.Key, kv => kv.Value);
    }


    public void Initialize(DalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
        presetCurrencies.OnUpdate += SetUpdateFlag;
        customCurrencies.OnUpdate += SetUpdateFlag;
    }

    public void Uninitialize()
    {
        presetCurrencies.OnUpdate -= SetUpdateFlag;
        customCurrencies.OnUpdate -= SetUpdateFlag;
    }

    private static void SetUpdateFlag() => IsUpdated = true;

    public void Save()
    {
        pluginInterface!.SavePluginConfig(this);
    }
}
