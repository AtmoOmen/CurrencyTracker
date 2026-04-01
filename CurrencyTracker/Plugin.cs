using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CurrencyTracker.Infos;
using CurrencyTracker.Internal;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Transactions;
using CurrencyTracker.Utilities;
using CurrencyTracker.Windows;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using OmenTools.OmenService;
using TinyPinyin;

namespace CurrencyTracker;

public sealed class Plugin : IDalamudPlugin
{
    public static string Name => "Currency Tracker";
    
    public const  string COMMAND_NAME = "/ct";

    public CharacterInfo? CurrentCharacter { get; set; }
    public string         PlayerDataFolder => GetCurrentCharacterDataFolder();


    public          WindowSystem WindowSystem = new("CurrencyTracker");
    internal static Plugin       P            = null!;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        P  = this;

        Service.Init(pluginInterface);

        DService.Instance().ClientState.Login  += HandleLogin;
        DService.Instance().ClientState.Logout += HandleLogout;
    }

    private void HandleLogout(int type, int code)
    {
        CurrencyInfo.CurrencyAmountCache.Clear();
        CurrentCharacter = null;
    }

    private void HandleLogin()
    {
        CurrentCharacter = GetCurrentCharacter();

        if (Main.SelectedCurrencyID != 0)
            Main.currentTransactions = TransactionsHandler.LoadAllTransactions(Main.SelectedCurrencyID).ToDisplayTransaction();
    }

    public CharacterInfo? GetCurrentCharacter()
    {
        if (LocalPlayerState.ContentID == 0 ||
            !DService.Instance().ClientState.IsLoggedIn)
            return null;

        var playerName = LocalPlayerState.Name;
        var serverName = GameState.HomeWorldData.Name.ToString();
        var contentID  = LocalPlayerState.ContentID;

        if (string.IsNullOrEmpty(playerName) || string.IsNullOrEmpty(serverName) || contentID == 0)
        {
            DService.Instance().Log.Error("Fail to load current character info");
            return null;
        }

        var dataFolderName = Path.Join(DService.Instance().PI.ConfigDirectory.FullName, $"{playerName}_{serverName}");

        if (CurrentCharacter != null &&
            (CurrentCharacter.ContentID == contentID ||
             CurrentCharacter.Name   == playerName &&
             CurrentCharacter.Server == serverName))
        {
            TransactionsHandler.EnsureCharacterDataReady(CurrentCharacter);
            return CurrentCharacter;
        }

        var existingCharacter =
            PluginConfig.Instance().CurrentActiveCharacter.FirstOrDefault(x => x.ContentID == contentID || x.Name == playerName && x.Server == serverName);

        if (existingCharacter != null)
        {
            existingCharacter.Server    = serverName;
            existingCharacter.Name      = playerName;
            existingCharacter.ContentID = contentID;
            CurrentCharacter            = existingCharacter;
            DService.Instance().Log.Debug("Successfully load current character info.");
        }
        else
        {
            CurrentCharacter = new CharacterInfo
            {
                Name      = playerName,
                Server    = serverName,
                ContentID = contentID
            };
            PluginConfig.Instance().CurrentActiveCharacter.Add(CurrentCharacter);
        }

        if (!Directory.Exists(dataFolderName))
        {
            Directory.CreateDirectory(dataFolderName);
            DService.Instance().Log.Debug("Successfully create character info directory.");
        }

        PluginConfig.Instance().Save();
        TransactionsHandler.EnsureCharacterDataReady(CurrentCharacter);

        return CurrentCharacter;
    }

    private string GetCurrentCharacterDataFolder()
    {
        CurrentCharacter ??= GetCurrentCharacter();

        if (CurrentCharacter == null) return string.Empty;

        var path = Path.Join
        (
            DService.Instance().PI.ConfigDirectory.FullName,
            $"{CurrentCharacter.Name}_{CurrentCharacter.Server}"
        );

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            DService.Instance().Log.Debug("Successfully create character info directory.");
        }

        return path;
    }

    public void Dispose()
    {
        DService.Instance().ClientState.Login  -= HandleLogin;
        DService.Instance().ClientState.Logout -= HandleLogout;

        DService.Instance().Command.RemoveHandler(COMMAND_NAME);

        Service.Uninit();
    }
}
