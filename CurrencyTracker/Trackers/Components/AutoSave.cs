using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using CurrencyTracker.Internal;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Transactions;
using Timer = System.Timers.Timer;

namespace CurrencyTracker.Trackers.Components;

public class AutoSave : TrackerComponentBase
{
    public static DateTime LastAutoSaveTime { get; set; } = DateTime.MinValue;
    public static DateTime NextAutoSaveTime { get; set; } = DateTime.MaxValue;

    internal static Timer? AutoSaveTimer;

    protected override void OnInit()
    {
        LastAutoSaveTime = DateTime.Now;
        NextAutoSaveTime = LastAutoSaveTime + TimeSpan.FromMinutes(PluginConfig.Instance().AutoSaveInterval);

        AutoSaveTimer           ??= new(1000);
        AutoSaveTimer.Elapsed   +=  OnAutoSave;
        AutoSaveTimer.AutoReset =   true;
        AutoSaveTimer.Enabled   =   true;
    }

    private static void OnAutoSave(object? sender, ElapsedEventArgs e)
    {
        if (DateTime.Now >= LastAutoSaveTime + TimeSpan.FromMinutes(PluginConfig.Instance().AutoSaveInterval))
        {
            AutoSaveHandlerAsync();
            LastAutoSaveTime = DateTime.Now;
            NextAutoSaveTime = LastAutoSaveTime + TimeSpan.FromMinutes(PluginConfig.Instance().AutoSaveInterval);
        }
    }

    public static void AutoSaveHandlerAsync()
    {
        switch (PluginConfig.Instance().AutoSaveMode)
        {
            case 0:
                Task.Run
                (async () =>
                    {
                        var filePath = await
                                           TransactionsHandler.BackupTransactionsAsync(P.PlayerDataFolder, PluginConfig.Instance().MaxBackupFilesCount);
                        if (PluginConfig.Instance().AutoSaveMessage)
                            DService.Instance().Chat.Print(Service.Lang.GetText("BackupHelp4", filePath));
                    }
                );
                break;
            case 1:
                Task.Run
                (async () =>
                    {
                        var failCharactersTasks = PluginConfig.Instance().CurrentActiveCharacter.Select
                        (async c =>
                            {
                                var result = await TransactionsHandler.BackupTransactionsAsync
                                             (
                                                 Path.Combine(DService.Instance().PI.ConfigDirectory.FullName, $"{c.Name}_{c.Server}"),
                                                 PluginConfig.Instance().MaxBackupFilesCount
                                             );

                                return string.IsNullOrEmpty(result) ? $"{c.Name}@{c.Server}" : null;
                            }
                        );

                        var failCharactersResults = await Task.WhenAll(failCharactersTasks);
                        var failCharacters        = failCharactersResults.Where(c => c != null).ToList();

                        var successCount = PluginConfig.Instance().CurrentActiveCharacter.Count - failCharacters.Count;

                        if (PluginConfig.Instance().AutoSaveMessage)
                        {
                            DService.Instance().Chat.Print
                            (
                                Service.Lang.GetText("BackupHelp1", successCount) +
                                (failCharacters.Count != 0
                                     ? Service.Lang.GetText("BackupHelp2", failCharacters.Count)
                                     : "")
                            );

                            if (failCharacters.Count != 0)
                            {
                                DService.Instance().Chat.PrintError(Service.Lang.GetText("BackupHelp3"));
                                failCharacters.ForEach(x => DService.Instance().Chat.PrintError(x));
                            }
                        }
                    }
                );
                break;
        }
    }


    protected override void OnUninit()
    {
        TransactionsHandler.BackupTransactions(P.PlayerDataFolder, PluginConfig.Instance().MaxBackupFilesCount);

        AutoSaveTimer?.Stop();
        if (AutoSaveTimer != null) AutoSaveTimer.Elapsed -= OnAutoSave;
        AutoSaveTimer?.Dispose();
        AutoSaveTimer = null;

        LastAutoSaveTime = DateTime.MinValue;
        NextAutoSaveTime = DateTime.MaxValue;
    }
}
