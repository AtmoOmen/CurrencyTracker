using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Trackers;
using CurrencyTracker.Windows;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TinyPinyin;

namespace CurrencyTracker
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Currency Tracker";
        public DalamudPluginInterface PluginInterface { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("CurrencyTracker");
        internal Main Main { get; init; }
        internal Graph Graph { get; init; }
        /*
        internal RecordSettings RecordSettings { get; init; }
        */
        public CharacterInfo? CurrentCharacter { get; set; }
        public static Plugin Instance = null!;
        internal const string CommandName = "/ct";
        private HookManager hookManager;

        public string PlayerDataFolder = string.Empty;
        private string playerLang = string.Empty;

        public Plugin(DalamudPluginInterface pluginInterface)
        {
            Instance = this;
            PluginInterface = pluginInterface;

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            Service.Initialize(pluginInterface);
            InitLanguage();

            // 已登录 Have Logged in
            if (Service.ClientState.LocalPlayer != null || Service.ClientState.LocalContentId != 0)
            {
                CurrentCharacter = GetCurrentCharacter();
            }

            Service.ClientState.Login += HandleLogin;
            Service.ClientState.Logout += HandleLogout;
            Service.Tracker = new Tracker();

            Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = Service.Lang.GetText("CommandHelp") + "\n" + Service.Lang.GetText("CommandHelp1")
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            hookManager = new HookManager(this);

            Main = new Main(this);
            WindowSystem.AddWindow(Main);
            Service.Tracker.OnCurrencyChanged += Main.UpdateTransactionsEvent;

            Graph = new Graph(this);
            WindowSystem.AddWindow(Graph);

            /*
            RecordSettings = new RecordSettings(this);
            WindowSystem.AddWindow(RecordSettings);
            */
        }

        private void HandleLogout()
        {
            Main.isFirstTime = false;
            Service.Tracker.Dispose();
        }

        private void HandleLogin()
        {
            if (Configuration.CurrentActiveCharacter == null)
            {
                Configuration.CurrentActiveCharacter = new List<CharacterInfo>();
            }

            CurrentCharacter = GetCurrentCharacter();
            GetCurrentCharcterDataFolder();

            if (WindowSystem.Windows.Contains(Main) && !Main.selectedCurrencyName.IsNullOrEmpty())
            {
                Main.currentTypeTransactions = Transactions.LoadAllTransactions(Main.selectedCurrencyName);
                Main.lastTransactions = Main.currentTypeTransactions;
            }

            Service.Tracker.InitializeTracking();
        }

        public CharacterInfo GetCurrentCharacter()
        {
            if (Service.ClientState.LocalContentId == 0) return null;

            if (CurrentCharacter != null && CurrentCharacter.Name == Service.ClientState.LocalPlayer.Name.TextValue)
            {
                return CurrentCharacter;
            }

            var playerName = Service.ClientState.LocalPlayer?.Name?.TextValue;
            var serverName = Service.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name;
            var dataFolderName = Path.Join(PluginInterface.ConfigDirectory.FullName, $"{playerName}_{serverName}");

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
                Service.PluginLog.Debug("Activation character in configuration matches current character");
            }
            else
            {
                CurrentCharacter = new CharacterInfo
                {
                    Name = playerName,
                    Server = serverName,
                };
                Configuration.CurrentActiveCharacter.Add(CurrentCharacter);
            }

            if (!Directory.Exists(dataFolderName))
            {
                Directory.CreateDirectory(dataFolderName);
                Service.PluginLog.Debug("Successfully Create Directory");
            }

            PlayerDataFolder = dataFolderName;

            Configuration.Save();

            return CurrentCharacter;
        }

        private void GetCurrentCharcterDataFolder()
        {
            if (Service.ClientState.LocalPlayer != null)
            {
                var playerName = Service.ClientState.LocalPlayer?.Name?.TextValue;
                var serverName = Service.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name;
                var dataFolderName = Path.Join(PluginInterface.ConfigDirectory.FullName, $"{playerName}_{serverName}");

                if (!Directory.Exists(dataFolderName))
                {
                    Directory.CreateDirectory(dataFolderName);
                    Service.PluginLog.Debug("Successfully Create Directory");
                }

                PlayerDataFolder = dataFolderName;
            }
        }

        internal void OnCommand(string command, string args)
        {
            var currencyName = string.Empty;
            if (string.IsNullOrEmpty(args))
            {
                Main.IsOpen = !Main.IsOpen;
            }
            else
            {
                var matchingCurrencies = FindMatchingCurrencies(Main.options, args);
                if (matchingCurrencies.Count > 1)
                {
                    Service.Chat.PrintError($"{Service.Lang.GetText("CommandHelp2")}:");
                    foreach (var currency in matchingCurrencies)
                    {
                        Service.Chat.PrintError(currency);
                    }
                    return;
                }
                else if (matchingCurrencies.Count == 0)
                {
                    Service.Chat.PrintError(Service.Lang.GetText("CommandHelp3"));
                    return;
                }
                else
                {
                    currencyName = matchingCurrencies.FirstOrDefault();
                }

                if (!Main.IsOpen)
                {
                    Main.selectedCurrencyName = currencyName;
                    Main.selectedOptionIndex = Main.options.IndexOf(currencyName);
                    Main.currentTypeTransactions = Transactions.LoadAllTransactions(Main.selectedCurrencyName);
                    Main.lastTransactions = Main.currentTypeTransactions;
                    Main.IsOpen = true;
                }
                else
                {
                    if (currencyName == Main.selectedCurrencyName)
                    {
                        Main.IsOpen = !Main.IsOpen;
                    }
                    else
                    {
                        Main.selectedCurrencyName = currencyName;
                        Main.selectedOptionIndex = Main.options.IndexOf(currencyName);
                        Main.currentTypeTransactions = Transactions.LoadAllTransactions(Main.selectedCurrencyName);
                        Main.lastTransactions = Main.currentTypeTransactions;
                    }
                }
            }
        }

        private void InitLanguage()
        {
            playerLang = Configuration.SelectedLanguage;
            if (string.IsNullOrEmpty(playerLang))
            {
                playerLang = Service.ClientState.ClientLanguage.ToString();
                if (!LanguageManager.LanguageNames.Any(x => x.Language == playerLang))
                {
                    playerLang = "English";
                }
                Configuration.SelectedLanguage = playerLang;
                Configuration.Save();
            }

            Service.Lang = new LanguageManager(playerLang);
        }

        private List<string> FindMatchingCurrencies(List<string> currencyList, string partialName)
        {
            var isChineseSimplified = Configuration.SelectedLanguage == "ChineseSimplified";

            partialName = partialName.Normalize(NormalizationForm.FormKC);

            return currencyList
                .Where(currency =>
                {
                    var normalizedCurrency = currency.Normalize(NormalizationForm.FormKC);

                    if (isChineseSimplified)
                    {
                        var pinyin = PinyinHelper.GetPinyin(normalizedCurrency, "");
                        return normalizedCurrency.Contains(partialName, StringComparison.OrdinalIgnoreCase) || pinyin.Contains(partialName, StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        return normalizedCurrency.IndexOf(partialName, StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                })
                .ToList();
        }

        private void DrawUI()
        {
            WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            var currentCharacter = GetCurrentCharacter();
            if (currentCharacter == null)
            {
                return;
            }

            Main.IsOpen = !Main.IsOpen;
        }

        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();

            Main.Dispose();
            Graph.Dispose();
            /*
            RecordSettings.Dispose();
            */

            hookManager.Dispose();

            Service.Tracker.OnCurrencyChanged -= Main.UpdateTransactionsEvent;
            Service.Tracker.Dispose();
            Service.ClientState.Login -= HandleLogin;
            Service.ClientState.Logout -= HandleLogout;

            Service.CommandManager.RemoveHandler(CommandName);
        }
    }
}
