using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Transactions;

namespace CurrencyTracker.Manager.Trackers.Components;

public class AutoSave : ITrackerComponent
{
    public bool Initialized { get; set; }
    public static DateTime LastAutoSaveTime { get; set; } = DateTime.MinValue;
    public static DateTime NextAutoSaveTime { get; set; } = DateTime.MaxValue;

    internal static Timer? AutoSaveTimer;

    public void Init()
    {
        LastAutoSaveTime = DateTime.Now;
        NextAutoSaveTime = LastAutoSaveTime + TimeSpan.FromMinutes(Service.Config.AutoSaveInterval);

        AutoSaveTimer ??= new Timer(1000);
        AutoSaveTimer.Elapsed += OnAutoSave;
        AutoSaveTimer.AutoReset = true;
        AutoSaveTimer.Enabled = true;
    }

    private static void OnAutoSave(object? sender, ElapsedEventArgs e)
    {
        if (DateTime.Now >= LastAutoSaveTime + TimeSpan.FromMinutes(Service.Config.AutoSaveInterval))
        {
            AutoSaveHandlerAsync();
            LastAutoSaveTime = DateTime.Now;
            NextAutoSaveTime = LastAutoSaveTime + TimeSpan.FromMinutes(Service.Config.AutoSaveInterval);
        }
    }

    public static void AutoSaveHandlerAsync()
    {
        switch (Service.Config.AutoSaveMode)
        {
            case 0:
                Task.Run(async () =>
                {
                    var filePath = await 
                        TransactionsHandler.BackupTransactionsAsync(P.PlayerDataFolder, Service.Config.MaxBackupFilesCount);
                    if (Service.Config.AutoSaveMessage)
                        Service.Chat.Print(Service.Lang.GetText("BackupHelp4", filePath));
                });
                break;
            case 1:
                Task.Run(async () =>
                {
                    var failCharactersTasks = Service.Config.CurrentActiveCharacter.Select(async c =>
                    {
                        var result = await TransactionsHandler.BackupTransactionsAsync(
                                         Path.Combine(P.PluginInterface.ConfigDirectory.FullName, $"{c.Name}_{c.Server}"),
                                         Service.Config.MaxBackupFilesCount);

                        return string.IsNullOrEmpty(result) ? $"{c.Name}@{c.Server}" : null;
                    });

                    var failCharactersResults = await Task.WhenAll(failCharactersTasks);
                    var failCharacters = failCharactersResults.Where(c => c != null).ToList();

                    var successCount = Service.Config.CurrentActiveCharacter.Count - failCharacters.Count;
                    if (Service.Config.AutoSaveMessage)
                    {
                        Service.Chat.Print(Service.Lang.GetText("BackupHelp1", successCount) +
                                           (failCharacters.Count != 0
                                                ? Service.Lang.GetText("BackupHelp2", failCharacters.Count)
                                                : ""));
                        if (failCharacters.Count != 0)
                        {
                            Service.Chat.PrintError(Service.Lang.GetText("BackupHelp3"));
                            failCharacters.ForEach(x => Service.Chat.PrintError(x));
                        }
                    }
                });
                break;
        }
    }


    public void Uninit()
    {
        TransactionsHandler.BackupTransactions(P.PlayerDataFolder, Service.Config.MaxBackupFilesCount);

        AutoSaveTimer?.Stop();
        if (AutoSaveTimer != null) AutoSaveTimer.Elapsed -= OnAutoSave;
        AutoSaveTimer?.Dispose();
        AutoSaveTimer = null;

        LastAutoSaveTime = DateTime.MinValue;
        NextAutoSaveTime = DateTime.MaxValue;
    }
}
