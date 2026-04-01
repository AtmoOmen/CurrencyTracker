using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CurrencyTracker.Infos;
using CurrencyTracker.Utilities;

namespace CurrencyTracker.Manager.Transactions;

public static class TransactionsHandler
{
    public static string GetDatabasePath(string? dataFolder = null) =>
        TransactionStore.GetDatabasePath(dataFolder ?? P.PlayerDataFolder);

    public static string GetTransactionFilePath(uint currencyID, TransactionFileCategory category, ulong ID = 0) =>
        GetDatabasePath();

    internal static string GetLegacyTransactionFilePath
    (
        string                  dataFolder,
        uint                    currencyID,
        TransactionFileCategory category,
        ulong                   ID = 0
    )
    {
        var suffix       = GetTransactionFileSuffix(category, ID);
        var currencyName = CurrencyInfo.GetName(currencyID);
        var path         = Path.Join(dataFolder, $"{currencyName}{suffix}.txt");
        return Transaction.SanitizeFilePath(path);
    }

    public static string GetTransactionFileSuffix(TransactionFileCategory category, ulong ID = 0) =>
        category switch
        {
            TransactionFileCategory.Inventory        => string.Empty,
            TransactionFileCategory.Retainer         => $"_{ID}",
            TransactionFileCategory.SaddleBag        => "_SB",
            TransactionFileCategory.PremiumSaddleBag => "_PSB",
            _                                        => string.Empty
        };

    public static void EnsureCharacterDataReady(CharacterInfo? characterInfo = null)
    {
        if (!TryGetContext(characterInfo, out var dataFolder, out var characterContentId))
            return;

        TransactionStore.EnsureDatabaseReady(dataFolder, characterContentId);
    }

    public static List<Transaction> LoadAllTransactions
    (
        uint                    currencyID,
        TransactionFileCategory category = 0,
        ulong                   ID       = 0
    )
    {
        if (!TryGetContext(null, out var dataFolder, out var characterContentId))
            return [];

        return TransactionStore.LoadTransactions(dataFolder, characterContentId, currencyID, category, ID);
    }

    public static Task<List<Transaction>> LoadAllTransactionsAsync
    (
        uint                    currencyID,
        TransactionFileCategory category = 0,
        ulong                   ID       = 0
    ) => Task.FromResult(LoadAllTransactions(currencyID, category, ID));

    public static Transaction? LoadLatestSingleTransaction
    (
        uint                    currencyID,
        CharacterInfo?          characterInfo = null,
        TransactionFileCategory category      = 0,
        ulong                   ID            = 0
    )
    {
        if (!TryGetContext(characterInfo, out var dataFolder, out var characterContentId))
            return null;

        return TransactionStore.LoadLatestTransaction(dataFolder, characterContentId, currencyID, category, ID);
    }

    public static int EditSpecificTransactions
    (
        uint                    currencyID,
        List<Transaction>       selectedTransactions,
        string                  locationName = "None",
        string                  noteContent  = "None",
        TransactionFileCategory category     = 0,
        ulong                   ID           = 0
    )
    {
        if (selectedTransactions.Count == 0)
            return 0;

        if (!TryGetContext(null, out var dataFolder, out var characterContentId))
            return selectedTransactions.Count;

        var editedTransactions = TransactionStore.LoadTransactions(dataFolder, characterContentId, currencyID, category, ID);
        var failCount          = 0;
        var isLocationEdited   = locationName != "None";
        var isNoteEdited       = noteContent  != "None";

        foreach (var transaction in selectedTransactions)
        {
            var index = editedTransactions.FindIndex(t => t.Equals(transaction));

            if (index == -1)
            {
                failCount++;
                continue;
            }

            if (isLocationEdited) editedTransactions[index].LocationName = locationName;
            if (isNoteEdited) editedTransactions[index].Note             = noteContent;
        }

        TransactionStore.ReplaceTransactions(dataFolder, characterContentId, currencyID, category, ID, editedTransactions);

        return failCount;
    }

    public static int DeleteSpecificTransactions
    (
        uint                    currencyID,
        List<Transaction>       selectedTransactions,
        TransactionFileCategory category = 0,
        ulong                   ID       = 0
    )
    {
        if (selectedTransactions.Count == 0)
            return 0;

        if (!TryGetContext(null, out var dataFolder, out var characterContentId))
            return selectedTransactions.Count;

        var editedTransactions = TransactionStore.LoadTransactions(dataFolder, characterContentId, currencyID, category, ID);
        var failCount          = RemoveMatchingTransactions(editedTransactions, selectedTransactions);

        TransactionStore.ReplaceTransactions(dataFolder, characterContentId, currencyID, category, ID, editedTransactions);

        return failCount;
    }

    public static void AppendTransaction
    (
        uint                    currencyID,
        DateTime                TimeStamp,
        long                    Amount,
        long                    Change,
        string                  LocationName,
        string                  Note,
        TransactionFileCategory category = 0,
        ulong                   ID       = 0
    )
    {
        if (!TryGetContext(null, out var dataFolder, out var characterContentId))
            return;

        TransactionStore.InsertTransaction
        (
            dataFolder,
            characterContentId,
            currencyID,
            category,
            ID,
            new()
            {
                TimeStamp    = TimeStamp,
                Amount       = Amount,
                Change       = Change,
                LocationName = LocationName,
                Note         = Note
            }
        );
    }

    public static void AddTransaction
    (
        uint                    currencyID,
        DateTime                timeStamp,
        long                    amount,
        long                    change,
        string                  locationName,
        string                  note,
        TransactionFileCategory category = 0,
        ulong                   ID       = 0
    ) => AppendTransaction(currencyID, timeStamp, amount, change, locationName, note, category, ID);

    public static void ReorderTransactions(uint currencyID, TransactionFileCategory category = 0, ulong ID = 0) { }

    public static int MergeTransactionsByLocationAndThreshold
    (
        uint                    currencyID,
        long                    threshold,
        bool                    isOneWayMerge,
        TransactionFileCategory category = 0,
        ulong                   ID       = 0
    )
    {
        if (!TryGetContext(null, out var dataFolder, out var characterContentId))
            return 0;

        var allTransactions = TransactionStore.LoadTransactions(dataFolder, characterContentId, currencyID, category, ID);
        if (allTransactions.Count <= 1)
            return 0;

        var mergedTransactions = new List<Transaction>();
        var mergedCount        = 0;

        for (var i = 0; i < allTransactions.Count;)
        {
            var currentTransaction  = allTransactions[i];
            var separateMergedCount = 0;

            while (++i                                 < allTransactions.Count            &&
                   currentTransaction.LocationName     == allTransactions[i].LocationName &&
                   Math.Abs(allTransactions[i].Change) < threshold)
            {
                var nextTransaction = allTransactions[i];

                if (!isOneWayMerge ||
                    isOneWayMerge                  &&
                    currentTransaction.Change >= 0 &&
                    nextTransaction.Change    >= 0 ||
                    currentTransaction.Change < 0 && nextTransaction.Change < 0)
                {
                    if (nextTransaction.TimeStamp > currentTransaction.TimeStamp)
                    {
                        currentTransaction.Amount    = nextTransaction.Amount;
                        currentTransaction.TimeStamp = nextTransaction.TimeStamp;
                    }

                    currentTransaction.Change += nextTransaction.Change;

                    mergedCount += 2;
                    separateMergedCount++;
                }
                else
                    break;
            }

            if (separateMergedCount > 0)
                currentTransaction.Note = $"({Service.Lang.GetText("MergedSpecificHelp", separateMergedCount + 1)})";

            mergedTransactions.Add(currentTransaction);
        }

        TransactionStore.ReplaceTransactions(dataFolder, characterContentId, currencyID, category, ID, mergedTransactions);

        return mergedCount;
    }

    public static int MergeSpecificTransactions
    (
        uint                    currencyID,
        string                  locationName,
        List<Transaction>       selectedTransactions,
        string                  noteContent = "-1",
        TransactionFileCategory category    = 0,
        ulong                   ID          = 0
    )
    {
        if (selectedTransactions.Count <= 1)
            return 0;

        if (!TryGetContext(null, out var dataFolder, out var characterContentId))
            return 0;

        var allTransactions = TransactionStore.LoadTransactions(dataFolder, characterContentId, currencyID, category, ID);

        var latestTime    = DateTime.MinValue;
        long overallChange = 0;
        long finalAmount   = 0;
        var  mergedCount   = 0;

        foreach (var transaction in selectedTransactions)
        {
            var foundIndex = allTransactions.FindIndex(t => t.Equals(transaction));
            if (foundIndex == -1) continue;

            var foundTransaction = allTransactions[foundIndex];

            if (latestTime < foundTransaction.TimeStamp)
            {
                latestTime  = foundTransaction.TimeStamp;
                finalAmount = foundTransaction.Amount;
            }

            overallChange += foundTransaction.Change;
            allTransactions.RemoveAt(foundIndex);
            mergedCount++;
        }

        var finalTransaction = new Transaction
        {
            TimeStamp    = latestTime,
            Change       = overallChange,
            LocationName = locationName,
            Amount       = finalAmount,
            Note         = noteContent != "-1" ? noteContent : $"({Service.Lang.GetText("MergedSpecificHelp", mergedCount)})"
        };

        allTransactions.Add(finalTransaction);
        allTransactions = [.. allTransactions.OrderBy(x => x.TimeStamp)];

        TransactionStore.ReplaceTransactions(dataFolder, characterContentId, currencyID, category, ID, allTransactions);

        return mergedCount;
    }

    public static string ExportData
    (
        List<Transaction>       data,
        string                  fileName,
        uint                    currencyID,
        int                     exportType,
        TransactionFileCategory category = 0,
        ulong                   ID       = 0
    )
    {
        if (!TryGetContext(null, out var dataFolder, out _))
            return "Fail";

        var currencyName  = Service.Config.AllCurrencies[currencyID];
        var fileExtension = exportType == 0 ? "csv" : "md";
        var headers = exportType == 0
                          ? Service.Lang.GetText("ExportFileCSVHeader")
                          : $"{Service.Lang.GetText("ExportFileMDHeader")} {currencyName}\n\n{Service.Lang.GetText("ExportFileMDHeader1")}";
        var lineTemplate = exportType == 0 ? "{0},{1},{2},{3},{4}" : "| {0} | {1} | {2} | {3} | {4} |";

        if (exportType != 0 && exportType != 1)
            return "Fail";

        var exportFolder = Path.Combine(dataFolder, "Exported");
        Directory.CreateDirectory(exportFolder);

        var nowTime = DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss");
        var finalFileName = string.IsNullOrWhiteSpace(fileName)
                                ? $"{currencyName}_{nowTime}.{fileExtension}"
                                : $"{fileName}_{currencyName}_{nowTime}.{fileExtension}";
        var filePath = Path.Combine(exportFolder, finalFileName);

        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        writer.WriteLine(headers);

        foreach (var transaction in data)
        {
            var line = string.Format
            (
                lineTemplate,
                transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss"),
                transaction.Amount,
                transaction.Change,
                transaction.LocationName,
                transaction.Note
            );
            writer.WriteLine(line);
        }

        return filePath;
    }

    public static string BackupTransactions(string dataFolder, int maxBackupFilesCount)
    {
        if (string.IsNullOrEmpty(dataFolder))
            return "Fail";

        var backupFolder = Path.Combine(dataFolder, "Backups");
        Directory.CreateDirectory(backupFolder);

        if (maxBackupFilesCount > 0)
        {
            var backupFiles = Directory.GetFiles(backupFolder, "*.zip")
                                       .OrderBy(f => new FileInfo(f).CreationTime)
                                       .ToList();

            while (backupFiles.Count >= maxBackupFilesCount)
            {
                if (!FileHelper.IsFileLocked(new FileInfo(backupFiles[0]))) File.Delete(backupFiles[0]);
                backupFiles.RemoveAt(0);
            }
        }

        var tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempFolder);

        string zipFilePath;

        try
        {
            foreach (var file in Directory.GetFiles(dataFolder))
                File.Copy(file, Path.Combine(tempFolder, Path.GetFileName(file)), true);

            zipFilePath = Path.Combine(backupFolder, $"Backup_{DateTime.Now:yyyyMMddHHmmss}.zip");
            ZipFile.CreateFromDirectory(tempFolder, zipFilePath);
        }
        finally
        {
            Directory.Delete(tempFolder, true);
        }

        return zipFilePath;
    }

    public static async Task<string> BackupTransactionsAsync(string dataFolder, int maxBackupFilesCount)
    {
        if (string.IsNullOrEmpty(dataFolder))
            return "Fail";

        var backupFolder = Path.Combine(dataFolder, "Backups");
        Directory.CreateDirectory(backupFolder);

        if (maxBackupFilesCount > 0)
        {
            var backupFiles = Directory.GetFiles(backupFolder, "*.zip")
                                       .OrderBy(f => new FileInfo(f).CreationTime)
                                       .ToList();

            while (backupFiles.Count >= maxBackupFilesCount)
            {
                var fileInfo = new FileInfo(backupFiles[0]);
                if (!FileHelper.IsFileLocked(fileInfo))
                    File.Delete(backupFiles[0]);
                backupFiles.RemoveAt(0);
            }
        }

        var tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempFolder);

        string zipFilePath;

        try
        {
            foreach (var file in Directory.GetFiles(dataFolder))
            {
                var             destFile          = Path.Combine(tempFolder, Path.GetFileName(file));
                await using var sourceStream      = File.Open(file, FileMode.Open);
                await using var destinationStream = File.Create(destFile);
                await sourceStream.CopyToAsync(destinationStream);
            }

            zipFilePath = Path.Combine(backupFolder, $"Backup_{DateTime.Now:yyyyMMddHHmmss}.zip");
            ZipFile.CreateFromDirectory(tempFolder, zipFilePath);
        }
        finally
        {
            Directory.Delete(tempFolder, true);
        }

        return zipFilePath;
    }

    private static int RemoveMatchingTransactions(List<Transaction> allTransactions, IEnumerable<Transaction> selectedTransactions)
    {
        var failCount = 0;

        foreach (var transaction in selectedTransactions)
        {
            var foundIndex = allTransactions.FindIndex(t => t.Equals(transaction));
            if (foundIndex == -1)
            {
                failCount++;
                continue;
            }

            allTransactions.RemoveAt(foundIndex);
        }

        return failCount;
    }

    private static bool TryGetContext(CharacterInfo? characterInfo, out string dataFolder, out ulong characterContentId)
    {
        dataFolder         = string.Empty;
        characterContentId = 0;

        if (characterInfo != null)
        {
            if (string.IsNullOrWhiteSpace(characterInfo.Name) ||
                string.IsNullOrWhiteSpace(characterInfo.Server) ||
                characterInfo.ContentID == 0)
                return false;

            dataFolder = Path.Join(P.PI.ConfigDirectory.FullName, $"{characterInfo.Name}_{characterInfo.Server}");
            Directory.CreateDirectory(dataFolder);
            characterContentId = characterInfo.ContentID;
            return true;
        }

        if (string.IsNullOrWhiteSpace(P.PlayerDataFolder) || P.CurrentCharacter == null)
        {
            DService.Instance().Log.Warning("当前角色数据目录不可用。");
            return false;
        }

        dataFolder         = P.PlayerDataFolder;
        characterContentId = P.CurrentCharacter.ContentID;
        return true;
    }
}
