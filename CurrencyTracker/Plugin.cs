global using static CurrencyTracker.Manager.Tools.Helpers;
global using static CurrencyTracker.Plugin;
global using static CurrencyTracker.Manager.Trackers.Handlers.TerritoryHandler;
global using static OmenTools.Helpers.HelpersOm;
global using static OmenTools.Helpers.ThrottlerHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Transactions;
using CurrencyTracker.Windows;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using TinyPinyin;
using CurrencyTracker.Infos;

namespace CurrencyTracker;

public sealed class Plugin : IDalamudPlugin
{
    public static string Name => "Currency Tracker";
    public const string CommandName = "/ct";

    public CharacterInfo? CurrentCharacter { get; set; }
    public string PlayerDataFolder => GetCurrentCharacterDataFolder();

    public IDalamudPluginInterface PI               { get; init; }
    public Main?                   Main             { get; private set; }
    public Graph?                  Graph            { get; private set; }
    public Settings?               Settings         { get; private set; }
    public CurrencySettings?       CurrencySettings { get; private set; }

    public WindowSystem WindowSystem = new("CurrencyTracker");
    internal static Plugin P = null!;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        P = this;
        PI = pluginInterface;

        Service.Config = PI.GetPluginConfig() as Configuration ?? new Configuration();
        Service.Config.Initialize(PI);

        Service.Init(pluginInterface);

        Service.ClientState.Login  += HandleLogin;
        Service.ClientState.Logout += HandleLogout;

        WindowsHandler();
    }

    private void HandleLogout(int type, int code)
    {
        CurrencyInfo.CurrencyAmountCache.Clear();
        CurrentCharacter = null;
    }

    private void HandleLogin()
    {
        CurrentCharacter = GetCurrentCharacter();

        if (WindowSystem.Windows.Contains(Main) && Main.SelectedCurrencyID != 0)
            Main.currentTransactions = TransactionsHandler.LoadAllTransactions(Main.SelectedCurrencyID).ToDisplayTransaction();
    }

    public CharacterInfo? GetCurrentCharacter()
    {
        if (Service.ClientState.LocalContentId == 0 || Service.ClientState.LocalPlayer == null ||
            !Service.ClientState.IsLoggedIn) return null;

        var playerName = Service.ClientState.LocalPlayer?.Name.ExtractText();
        var serverName = Service.ClientState.LocalPlayer?.HomeWorld.Value.Name.ExtractText();
        var contentID = Service.ClientState.LocalContentId;

        if (string.IsNullOrEmpty(playerName) || string.IsNullOrEmpty(serverName) || contentID == 0)
            Service.Log.Error("Fail to load current character info");

        var dataFolderName = Path.Join(PI.ConfigDirectory.FullName, $"{playerName}_{serverName}");

        if (CurrentCharacter != null && (CurrentCharacter.ContentID == contentID ||
                                         (CurrentCharacter.Name == playerName &&
                                          CurrentCharacter.Server == serverName))) return CurrentCharacter;

        var existingCharacter =
            Service.Config.CurrentActiveCharacter.FirstOrDefault(
                x => x.ContentID == contentID || (x.Name == playerName && x.Server == serverName));
        if (existingCharacter != null)
        {
            existingCharacter.Server = serverName;
            existingCharacter.Name = playerName;
            existingCharacter.ContentID = contentID;
            CurrentCharacter = existingCharacter;
            Service.Log.Debug("Successfully load current character info.");
        }
        else
        {
            CurrentCharacter = new CharacterInfo
            {
                Name = playerName,
                Server = serverName,
                ContentID = contentID
            };
            Service.Config.CurrentActiveCharacter.Add(CurrentCharacter);
        }

        if (!Directory.Exists(dataFolderName))
        {
            Directory.CreateDirectory(dataFolderName);
            Service.Log.Debug("Successfully create character info directory.");
        }

        Service.Config.Save();

        return CurrentCharacter;
    }

    private string GetCurrentCharacterDataFolder()
    {
        CurrentCharacter ??= GetCurrentCharacter();

        if (CurrentCharacter == null) return string.Empty;

        var path = Path.Join(PI.ConfigDirectory.FullName,
                             $"{CurrentCharacter.Name}_{CurrentCharacter.Server}");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            Service.Log.Debug("Successfully create character info directory.");
        }

        return path;
    }

    public void OnCommand(string command, string args)
    {
        if (string.IsNullOrEmpty(args))
        {
            Main.IsOpen ^= true;
            return;
        }

        var matchingCurrencies = FindMatchingCurrencies(Service.Config.AllCurrencies.Values.ToList(), args);
        var matchCount = matchingCurrencies.Count;

        switch (matchCount)
        {
            case 0:
                Service.Chat.PrintError(Service.Lang.GetText("CommandHelp3"));
                break;
            case 1:
                var currencyName = matchingCurrencies[0];
                var currencyPair = Service.Config.AllCurrencies.FirstOrDefault(x => x.Value == currencyName);
                var currencyID = currencyPair.Key;

                if (!Main.IsOpen || currencyID != Main.SelectedCurrencyID)
                {
                    Main.IsOpen = true;
                    Main.SelectedCurrencyID = currencyID;
                    Main.currentTransactions = Main.ApplyFilters(TransactionsHandler.LoadAllTransactions(currencyID)).ToDisplayTransaction();
                }
                else
                    Main.IsOpen = false;

                break;
            default:
                Service.Chat.PrintError($"{Service.Lang.GetText("CommandHelp2")}:");
                foreach (var currency in matchingCurrencies) Service.Chat.PrintError(currency);
                break;
        }

        return;

        static List<string> FindMatchingCurrencies(IReadOnlyCollection<string> currencyList, string partialName)
        {
            partialName = partialName.Normalize(NormalizationForm.FormKC);
            var isCS = Service.Config.SelectedLanguage == "ChineseSimplified";

            var exactMatch = currencyList.FirstOrDefault(currency => string.Equals(currency, partialName, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null) return [exactMatch];

            return currencyList
                   .Where(currency => IsMatch(currency.Normalize(NormalizationForm.FormKC)))
                   .ToList();

            bool IsMatch(string normalizedCurrency) => 
                normalizedCurrency.Contains(partialName, StringComparison.OrdinalIgnoreCase) ||
                (isCS && PinyinHelper.GetPinyin(normalizedCurrency, "").Contains(partialName, StringComparison.OrdinalIgnoreCase));
        }
    }

    private void WindowsHandler()
    {
        PI.UiBuilder.Draw += DrawUI;
        PI.UiBuilder.OpenConfigUi += DrawConfigUI;
        PI.UiBuilder.OpenMainUi += DrawMainUI;

        Main = new Main();
        WindowSystem.AddWindow(Main);

        Graph = new Graph(this);
        WindowSystem.AddWindow(Graph);

        Settings = new Settings(this);
        WindowSystem.AddWindow(Settings);

        CurrencySettings = new CurrencySettings();
        WindowSystem.AddWindow(CurrencySettings);
    }

    private void DrawUI()
    {
        WindowSystem.Draw();
    }

    public void DrawConfigUI()
    {
        var currentCharacter = GetCurrentCharacter();
        if (currentCharacter == null) return;

        Settings.IsOpen ^= true;
    }

    private void DrawMainUI()
    {
        var currentCharacter = GetCurrentCharacter();
        if (currentCharacter == null) return;

        Main.IsOpen ^= true;
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        Main.Dispose();
        Graph.Dispose();
        Settings.Dispose();
        CurrencySettings.Dispose();

        PI.UiBuilder.Draw -= DrawUI;
        PI.UiBuilder.OpenConfigUi -= DrawConfigUI;
        PI.UiBuilder.OpenMainUi -= DrawMainUI;

        Service.ClientState.Login -= HandleLogin;
        Service.ClientState.Logout -= HandleLogout;

        Service.CommandManager.RemoveHandler(CommandName);

        Service.Uninit();
    }
}
