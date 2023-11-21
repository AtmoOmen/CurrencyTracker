using CurrencyTracker.Manager;
using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace CurrencyTracker
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public bool FisrtOpen { get; set; } = true;
        public List<CharacterInfo> CurrentActiveCharacter { get; set; } = new();
        private Dictionary<uint, string> presetCurrencies = new();
        public Dictionary<uint, string> PresetCurrencies
        {
            set
            {
                presetCurrencies = value;
                isUpdated = true;
            }
            get
            {
                return presetCurrencies;
            }
        }
        private Dictionary<uint, string> customCurrencies = new();
        public Dictionary<uint, string> CustomCurrencies
        {
            set
            {
                customCurrencies = value;
                isUpdated = true;
            }
            get
            {
                return customCurrencies;
            }
        }
        public List<uint> OrderedOptions { get; set; } = new();
        public bool ReverseSort { get; set; } = false;
        public string SelectedLanguage { get; set; } = string.Empty;
        public int RecordsPerPage { get; set; } = 20;
        public bool ChangeTextColoring { get; set; } = true;
        public Vector4 PositiveChangeColor { get; set; } = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
        public Vector4 NegativeChangeColor { get; set; } = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
        public int ExportDataFileType { get; set; } = 0;
        public bool ShowTimeColumn { get; set; } = true;
        public bool ShowAmountColumn { get; set; } = true;
        public bool ShowChangeColumn { get; set; } = true;
        public bool ShowLocationColumn { get; set; } = true;
        public bool ShowNoteColumn { get; set; } = true;
        public bool ShowOrderColumn { get; set; } = true;
        public bool ShowCheckboxColumn { get; set; } = true;
        public int ChildWidthOffset { get; set; } = 0;

        // 备注选项 Note Settings
        public bool RecordContentName { get; set; } = true;
        public bool RecordTeleportDes { get; set; } = true;
        public bool WaitExComplete { get; set; } = true;
        public bool RecordTeleport { get; set; } = true;
        public bool TrackedInDuty { get; set; } = true;
        public bool RecordMGPSource { get; set; } = true;
        public bool RecordTripleTriad { get; set; } = true;
        public bool RecordQuestName { get; set; } = true;
        public bool RecordTrade { get; set; } = true;
        public bool RecordFate { get; set; } = true;
        public bool RecordIsland { get; set; } = true;

        [Newtonsoft.Json.JsonIgnore]
        public List<CurrencyIcon> AllCurrencyIcons
        {
            get
            {
                if (allCurrencyIcons == null || isUpdated)
                {
                    allCurrencyIcons = GetAllCurrencyIcons();
                }
                return allCurrencyIcons;
            }
        }

        [Newtonsoft.Json.JsonIgnore]
        public Dictionary<uint, string> AllCurrencies
        {
            get
            {
                if (allCurrencies == null || isUpdated)
                {
                    allCurrencies = GetAllCurrencies();
                }
                return allCurrencies;
            }
        }

        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        [Newtonsoft.Json.JsonIgnore]
        private List<CurrencyIcon>? allCurrencyIcons;

        private Dictionary<uint, string>? allCurrencies;
        public bool isUpdated;

        private List<CurrencyIcon> GetAllCurrencyIcons()
        {
            List<CurrencyIcon> allCurrencies = new();

            foreach (var currency in PresetCurrencies)
            {
                allCurrencies.Add(new CurrencyIcon
                {
                    CurrencyID = currency.Key,
                    CurrencyName = currency.Value,
                });
            }

            foreach (var currency in CustomCurrencies)
            {
                allCurrencies.Add(new CurrencyIcon
                {
                    CurrencyID = currency.Key,
                    CurrencyName = currency.Value,
                });
            }

            isUpdated = false;

            Service.PluginLog.Debug("重新获取全部货币图标成功");

            return allCurrencies;
        }

        private Dictionary<uint, string> GetAllCurrencies()
        {
            Service.PluginLog.Debug("重新获取全部货币数据成功");
            isUpdated = false;
            return PresetCurrencies.Concat(CustomCurrencies).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            pluginInterface!.SavePluginConfig(this);
        }
    }
}
