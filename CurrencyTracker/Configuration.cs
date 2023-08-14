using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using CurrencyTracker.Manager;
using Dalamud.Game.ClientState;

namespace CurrencyTracker
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public ClientState? ClientState { get; init; }
        public int Version { get; set; } = 0;
        public List<CharacterInfo> CurrentActiveCharacter { get; set; } = new List<CharacterInfo>();
        // 存储用户自定义货币ID的字典（string为用户自定义的名称）
        public Dictionary<string, uint> CustomCurrecies { get; set; } = new();
        public List<string> CustomCurrencyType { get; set; } = new List<string> ();
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
