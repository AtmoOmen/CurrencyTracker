using CurrencyTracker.Manager;
using Dalamud.Configuration;
using Dalamud.Game.ClientState;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace CurrencyTracker
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public ClientState? ClientState { get; init; }
        public int Version { get; set; } = 0;
        public List<CharacterInfo> CurrentActiveCharacter { get; set; } = new List<CharacterInfo>();

        // 存储用户自定义货币ID的字典 Dic saving custom currencies' names
        public Dictionary<string, uint> CustomCurrencies { get; set; } = new();

        public List<string> CustomCurrencyType { get; set; } = new List<string>();
        public bool ReverseSort { get; set; } = false;
        public bool TrackedInDuty { get; set; } = false;
        public string SelectedLanguage { get; set; } = string.Empty;
        public int MinTrackValue { get; set; } = 0;

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
