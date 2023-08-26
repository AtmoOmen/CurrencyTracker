using CurrencyTracker.Manager;
using CurrencyTracker.Windows;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CurrencyTracker
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Currency Trakcer";
        public DalamudPluginInterface PluginInterface { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("CurrencyTracker");
        private Main MainWindow { get; init; }
        public CharacterInfo? CurrentCharacter { get; set; }
        public static Plugin GetPlugin = null!;

        // 地名/物品名字典 Ditionaries Containing Location Names and Item Names
        internal Dictionary<uint, string> TerritoryNames = new();

        internal Dictionary<uint, string> ItemNames = new();

        private string playerLang = string.Empty;

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

#pragma warning disable CS8604
            TerritoryNames = Service.DataManager.GetExcelSheet<TerritoryType>()
                .Where(x => x.PlaceName?.Value?.Name?.ToString().Length > 0)
                .ToDictionary(
                    x => x.RowId,
                    x => $"{x.PlaceName?.Value?.Name}");
#pragma warning restore CS8604

#pragma warning disable CS8604
            ItemNames = Service.DataManager.GetExcelSheet<Item>()
                .Where(x => x.Name?.ToString().Length > 0)
                .ToDictionary(
                    x => x.RowId,
                    x => $"{x.Name}");
#pragma warning restore CS8604

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

#pragma warning disable CS8604
            if (string.IsNullOrEmpty(playerName) || string.IsNullOrEmpty(serverName))
            {
                throw new InvalidOperationException("无法加载当前角色信息 Can't Load Current Character Info");
            }
#pragma warning restore CS8604

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
                PluginLog.Debug("配置文件激活角色与当前角色一致 Configuration file activation character matches current character");
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

                playerLang = Configuration.SelectedLanguage;
                if (string.IsNullOrEmpty(playerLang))
                {
                    playerLang = Service.ClientState.ClientLanguage.ToString();
                    // 不受支持的语言 => 英语 Not Supported Languages => English
                    if (playerLang != "ChineseSimplified" && playerLang != "English")
                    {
                        playerLang = "English";
                    }
                    Configuration.SelectedLanguage = playerLang;
                }
                PluginLog.Debug("创建文件夹成功 Successfully Create Directory");
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
