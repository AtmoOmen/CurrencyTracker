using CurrencyTracker.Manager;
using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace CurrencyTracker
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public List<CharacterInfo> CurrentActiveCharacter { get; set; } = new List<CharacterInfo>();

        // 存储用户自定义货币ID的字典 Dic saving custom currencies' names
        public Dictionary<string, uint> CustomCurrencies { get; set; } = new();

        // 存储最小变更值设置的字典 Dic used to store min track value
        public Dictionary<string, Dictionary<string, int>> MinTrackValueDic { get; set; } = new Dictionary<string, Dictionary<string, int>>
        {
            {
                "InDuty", new Dictionary<string, int> ()
            },

            {
                "OutOfDuty", new Dictionary<string, int> ()
            }
        };

        public bool FisrtOpen = true;

        public List<string> CustomCurrencyType { get; set; } = new List<string>();
        public List<string> OrdedOptions { get; set; } = new List<string>();
        public List<string> HiddenOptions { get; set; } = new List<string>();
        public bool ReverseSort { get; set; } = false;
        public string SelectedLanguage { get; set; } = string.Empty;
        public int TrackMode { get; set; } = 1;
        public int RecordsPerPage { get; set; } = 20;
        public bool ChangeTextColoring { get; set; } = true;
        public Vector4 PositiveChangeColor { get; set; } = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
        public Vector4 NegativeChangeColor { get; set; } = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
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
        public bool RecordTrade {  get; set; } = true;

        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

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
