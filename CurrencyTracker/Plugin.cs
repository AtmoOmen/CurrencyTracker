using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using CurrencyTracker.Windows;
using CurrencyTracker.Manager;
using Dalamud.Game.ClientState;
using System.Linq;
using static FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using System;
using System.Collections.Generic;

namespace CurrencyTracker
{
    public sealed class Plugin : IDalamudPlugin
    {
        // 一些声明
        public string Name => "Currency Trakcer";
        private DalamudPluginInterface PluginInterface { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("CurrencyTracker");
        private Main MainWindow { get; init; }
        public CharacterInfo? CurrentCharacter { get; set; }

        // 插件初始化时执行的代码部分
        public Plugin([RequiredVersion("1.0")] DalamudPluginInterface pluginInterface)
        {
            Service.Initialize(pluginInterface);

            this.PluginInterface = pluginInterface;
            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            MainWindow = new Main(this);
            WindowSystem.AddWindow(MainWindow);

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            Service.Tracker = new Tracker();
        }

        public CharacterInfo GetCurrentCharacter()
        {
            if (CurrentCharacter != null)
            {
                return CurrentCharacter;
            }

            var playerName = Service.ClientState.LocalPlayer?.Name?.TextValue;
            var serverName = Service.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name;
            var dataFolderName = Path.Join(PluginInterface.ConfigDirectory.FullName, $"{playerName}_{serverName}");

            if (string.IsNullOrEmpty(playerName) || string.IsNullOrEmpty(serverName))
            {
                throw new InvalidOperationException("Unable to get current character.");
            }

            if (Configuration.CurrentActiveCharacter == null)
            {
                Configuration.CurrentActiveCharacter = new List<CharacterInfo>();
            }

            var existingCharacter = Configuration.CurrentActiveCharacter.FirstOrDefault(x => x.Name == playerName);
            if (existingCharacter != null)
            {
                existingCharacter.Server = serverName;
                existingCharacter.Name = playerName;
                existingCharacter.PlayerDataFolder = dataFolderName;
                CurrentCharacter = existingCharacter;
            }
            else
            {
                CurrentCharacter = new CharacterInfo
                {
                    Name = playerName,
                    Server = serverName,
                    PlayerDataFolder = dataFolderName
            };
                Configuration.CurrentActiveCharacter.Add(CurrentCharacter);
                Directory.CreateDirectory(dataFolderName);
            }

            Configuration.Save();

            return CurrentCharacter;
        }

        // 插件禁用/卸载时执行的代码部分
        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();
            
            MainWindow.Dispose();
            Service.Tracker.Dispose();
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            var currentCharacter = GetCurrentCharacter();
            if (currentCharacter == null)
                return;

            
            MainWindow.IsOpen = true;
        }
    }
}
