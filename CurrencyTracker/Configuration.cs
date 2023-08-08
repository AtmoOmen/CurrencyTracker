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
        public bool ReverseSort { get; set; } = false;
        public bool TrackedInDuty { get; set; } = false;
        public string SelectedLanguage { get; set; } = string.Empty;

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
