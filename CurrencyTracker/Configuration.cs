using CurrencyTracker.Manager;
using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace CurrencyTracker
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public List<CharacterInfo> CurrentActiveCharacter { get; set; } = new List<CharacterInfo>();
        // 存储用户自定义货币ID的字典 Dic saving custom currencies' names
        public Dictionary<string, uint> CustomCurrencies { get; set; } = new();
        // 存储最小变更值设置的字典
        public Dictionary<string, Dictionary<string, int>> MinTrackValueDic { get; set; } = new Dictionary<string, Dictionary<string, int>>
        {
            {
                "InDuty", new Dictionary<string, int> ()
            },

            {
                "OutOfDuty", new Dictionary<string, int> ()
            }

        };
        public List<string> CustomCurrencyType { get; set; } = new List<string>();
        public bool ReverseSort { get; set; } = false;
        public bool TrackedInDuty { get; set; } = false;
        public string SelectedLanguage { get; set; } = string.Empty;
        public int TrackMode { get; set; } = 0;
        public int RecordsPerPage { get; set; } = 20;
        public int TimerInterval { get; set; } = 500;

        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }
    }
}
