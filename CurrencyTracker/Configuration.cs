using CurrencyTracker.Manager;
using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace CurrencyTracker
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public bool FisrtOpen { get; set; } = true;
        public List<CharacterInfo> CurrentActiveCharacter { get; set; } = new List<CharacterInfo>();
        public Dictionary<string, uint> CustomCurrencies { get; set; } = new();
        public Dictionary<string, uint> PresetCurrencies { get; set; } = new();
        public List<string> OrdedOptions { get; set; } = new List<string>();
        public List<string> HiddenOptions { get; set; } = new List<string>();
        public bool ReverseSort { get; set; } = false;
        public string SelectedLanguage { get; set; } = string.Empty;
        public int TrackMode { get; set; } = 1;
        public int RecordsPerPage { get; set; } = 20;
        public bool ChangeTextColoring { get; set; } = true;
        public Vector4 PositiveChangeColor { get; set; } = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
        public Vector4 NegativeChangeColor { get; set; } = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
        public int ExportDataFileType { get; set; } = 0;
        public bool ShowLocationColumn { get; set; } = true;
        public bool ShowNoteColumn { get; set; } = true;
        public bool ShowOrderColumn { get; set; } = true;

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

        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public Dictionary<string, uint> AllCurrencies => MergeCurrencies();

        private Dictionary<string, uint> MergeCurrencies()
        {
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
