using Dalamud.IoC;
using Dalamud.Game.Gui;
using Dalamud.Logging;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using CurrencyTracker.Windows;
using CurrencyTracker.Manager;
using System.Linq;
using System;
using System.Collections.Generic;

namespace CurrencyTracker
{
    public sealed class Plugin : IDalamudPlugin
    {
        // 一些声明
        public string Name => "Currency Trakcer";
        public DalamudPluginInterface PluginInterface { get; init; }
        public ChatGui ChatGui { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("CurrencyTracker");
        private Main MainWindow { get; init; }
        public CharacterInfo? CurrentCharacter { get; set; }
        public static Plugin GetPlugin = null!;  // 插件的静态实例

        // 插件初始化时执行的代码部分
        public Plugin([RequiredVersion("1.0")] DalamudPluginInterface pluginInterface)
        {
            GetPlugin = this;
            Service.Initialize(pluginInterface);

            this.PluginInterface = pluginInterface;
            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            MainWindow = new Main(this);
            WindowSystem.AddWindow(MainWindow);

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            if (Configuration.CurrentActiveCharacter == null)
            {
                Configuration.CurrentActiveCharacter = new List<CharacterInfo>();
            }

            Service.Tracker = new Tracker();
            Service.Transactions = new Transactions();
            Service.ClientState.Login += isLogin;
        }

        private void isLogin(object? sender, EventArgs e)
        {
            CurrentCharacter = GetCurrentCharacter();
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
                throw new InvalidOperationException("无法获取当前角色");
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
                CurrentCharacter = existingCharacter;
                PluginLog.Debug("配置文件激活角色与当前角色一致");
            }
            else
            {
                CurrentCharacter = new CharacterInfo
                {
                    Name = playerName,
                    Server = serverName,
            };
                Configuration.CurrentActiveCharacter.Add(CurrentCharacter);
                Directory.CreateDirectory(dataFolderName);
                PluginLog.Debug("创建文件夹成功");
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
            Service.ClientState.Login -= isLogin;
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
