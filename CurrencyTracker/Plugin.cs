using CurrencyTracker.Manager;
using CurrencyTracker.Windows;
using Dalamud.Game.Command;
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
        public string Name => "Currency Tracker";
        public DalamudPluginInterface PluginInterface { get; init; }
        public CommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("CurrencyTracker");
        internal Main MainWindow { get; init; }
        public CharacterInfo? CurrentCharacter { get; set; }
        public static Plugin Instance = null!;
        private const string CommandName = "/ct";

        internal Dictionary<uint, string> TerritoryNames = new();
        internal Dictionary<uint, string> ItemNames = new();
        private string playerLang = string.Empty;

        public Plugin([RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
                      [RequiredVersion("1.0")] CommandManager commandManager)
        {
            Instance = this;
            Service.Initialize(pluginInterface);

            this.PluginInterface = pluginInterface;
            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);
            this.CommandManager = commandManager;

            MainWindow = new Main(this);
            WindowSystem.AddWindow(MainWindow);
            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open the main window of the plugin\n/ct CurrencyName -> Open the main window with specific currency shown."
            });

            if (Configuration.CurrentActiveCharacter == null)
            {
                Configuration.CurrentActiveCharacter = new List<CharacterInfo>();
            }

            Service.Tracker = new Tracker();
            Service.Transactions = new Transactions();

            UpdateTerritoryNames();
            UpdateItemNames();

            Service.ClientState.Login += HandleLogin;
        }

        private void HandleLogin(object? sender, EventArgs e)
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
                throw new InvalidOperationException("Can't Load Current Character Info");
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
                PluginLog.Debug("Configuration file activation character matches current character");
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
                    if (playerLang != "ChineseSimplified" && playerLang != "English")
                    {
                        playerLang = "English";
                    }
                    Configuration.SelectedLanguage = playerLang;
                }
                PluginLog.Debug("Successfully Create Directory");
            }

            Configuration.Save();

            return CurrentCharacter;
        }

        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();

            MainWindow.Dispose();
            Service.Tracker.Dispose();
            Service.ClientState.Login -= HandleLogin;
            this.CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            var currencyName = string.Empty;
            if (string.IsNullOrEmpty(args))
            {
                MainWindow.IsOpen = !MainWindow.IsOpen;
            }
            else
            {
                var matchingCurrencies = FindMatchingCurrencies(MainWindow.options, args);
                if (matchingCurrencies.Count > 1)
                {
                    Service.Chat.PrintError("Mutiple Currencies Found:");
                    foreach (var currency in matchingCurrencies)
                    {
                        Service.Chat.PrintError(currency);
                    }
                    return;
                }
                else if (matchingCurrencies.Count == 0)
                {
                    Service.Chat.PrintError("No Currency Found");
                    return;
                }
                else
                {
                    currencyName = matchingCurrencies.FirstOrDefault();
                }

                if (!MainWindow.IsOpen)
                {
                    MainWindow.selectedCurrencyName = currencyName;
                    MainWindow.selectedOptionIndex = MainWindow.options.IndexOf(currencyName);
                    MainWindow.IsOpen = true;
                }
                else
                {
                    if (currencyName == MainWindow.selectedCurrencyName)
                    {
                        MainWindow.IsOpen = !MainWindow.IsOpen;
                    }
                    else
                    {
                        MainWindow.selectedCurrencyName = currencyName;
                        MainWindow.selectedOptionIndex = MainWindow.options.IndexOf(currencyName);
                    }
                }
            }
        }

        private List<string> FindMatchingCurrencies(List<string> currencyList, string partialName)
        {
            return currencyList
                .Where(currency => currency.Contains(partialName))
                .ToList();
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            var currentCharacter = GetCurrentCharacter();
            if (currentCharacter == null)
            {
                return;
            }

            MainWindow.IsOpen = !MainWindow.IsOpen;
        }

        private void UpdateTerritoryNames()
        {
            TerritoryNames = Service.DataManager.GetExcelSheet<TerritoryType>()
                .Where(x => !string.IsNullOrEmpty(x.PlaceName?.Value?.Name?.ToString()))
                .ToDictionary(
                    x => x.RowId,
                    x => $"{x.PlaceName?.Value?.Name}");
        }

        private void UpdateItemNames()
        {
            ItemNames = Service.DataManager.GetExcelSheet<Item>()
                .Where(x => !string.IsNullOrEmpty(x.Name?.ToString()))
                .ToDictionary(
                    x => x.RowId,
                    x => $"{x.Name}");
        }
    }
}
