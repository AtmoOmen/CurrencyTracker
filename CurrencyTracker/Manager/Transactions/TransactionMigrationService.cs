using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CurrencyTracker.Infos;
using CurrencyTracker.Internal;
using Microsoft.Data.Sqlite;

namespace CurrencyTracker.Manager.Transactions;

internal static class TransactionMigrationService
{
    public static void MigrateLegacyFiles(SqliteConnection connection, string dataFolder, ulong characterContentId)
    {
        if (characterContentId == 0 || string.IsNullOrWhiteSpace(dataFolder))
            return;

        foreach (var file in EnumerateLegacyFiles(dataFolder, characterContentId))
            MigrateLegacyFile(connection, file, characterContentId);
    }

    private static void MigrateLegacyFile(SqliteConnection connection, LegacyTransactionFile file, ulong characterContentId)
    {
        var lastWriteUtcTicks = File.GetLastWriteTimeUtc(file.FilePath).Ticks;

        using var queryCommand = connection.CreateCommand();
        queryCommand.CommandText =
            """
            SELECT LastWriteUtcTicks
            FROM MigrationHistory
            WHERE SourceFileKey = $sourceFileKey;
            """;
        queryCommand.Parameters.AddWithValue("$sourceFileKey", file.FileKey);

        var   scalar        = queryCommand.ExecuteScalar();
        long? existingTicks = scalar is long value ? value : null;
        if (existingTicks == lastWriteUtcTicks)
        {
            DService.Instance().Log.Debug($"跳过已迁移的数据文件：{Path.GetFileName(file.FilePath)}");
            return;
        }

        if (existingTicks.HasValue)
        {
            DService.Instance().Log.Warning($"检测到已迁移的旧文本文件发生变化，已跳过重复迁移：{Path.GetFileName(file.FilePath)}");

            using var updateCommand = connection.CreateCommand();
            updateCommand.CommandText =
                """
                UPDATE MigrationHistory
                SET LastWriteUtcTicks = $lastWriteUtcTicks
                WHERE SourceFileKey = $sourceFileKey;
                """;
            updateCommand.Parameters.AddWithValue("$lastWriteUtcTicks", lastWriteUtcTicks);
            updateCommand.Parameters.AddWithValue("$sourceFileKey",      file.FileKey);
            updateCommand.ExecuteNonQuery();
            return;
        }

        DService.Instance().Log.Information($"开始迁移旧数据文件：{Path.GetFileName(file.FilePath)}");

        var importedTransactions = new List<Transaction>();
        var failedCount          = 0;
        var lineNumber           = 0;

        using (var reader = new StreamReader(file.FilePath))
        {
            while (reader.ReadLine() is { } line)
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    importedTransactions.Add(Transaction.FromFileLine(line.AsSpan()));
                }
                catch (Exception ex)
                {
                    failedCount++;
                    DService.Instance().Log.Warning($"迁移旧数据文件时跳过无效记录：{Path.GetFileName(file.FilePath)} 第 {lineNumber} 行，{ex.Message}");
                }
            }
        }

        using var sqliteTransaction = connection.BeginTransaction();
        TransactionStore.InsertTransactions
        (
            connection,
            sqliteTransaction,
            characterContentId,
            file.CurrencyId,
            file.Category,
            file.CategoryOwnerId,
            importedTransactions,
            file.FileKey
        );

        using (var updateHistoryCommand = connection.CreateCommand())
        {
            updateHistoryCommand.Transaction = sqliteTransaction;
            updateHistoryCommand.CommandText =
                """
                INSERT INTO MigrationHistory
                (
                    SourceFileKey,
                    SourceFilePath,
                    LastWriteUtcTicks,
                    ImportedRowCount,
                    MigratedAtUtcTicks
                )
                VALUES
                (
                    $sourceFileKey,
                    $sourceFilePath,
                    $lastWriteUtcTicks,
                    $importedRowCount,
                    $migratedAtUtcTicks
                );
                """;
            updateHistoryCommand.Parameters.AddWithValue("$sourceFileKey",      file.FileKey);
            updateHistoryCommand.Parameters.AddWithValue("$sourceFilePath",     file.FilePath);
            updateHistoryCommand.Parameters.AddWithValue("$lastWriteUtcTicks",  lastWriteUtcTicks);
            updateHistoryCommand.Parameters.AddWithValue("$importedRowCount",   importedTransactions.Count);
            updateHistoryCommand.Parameters.AddWithValue("$migratedAtUtcTicks", DateTime.UtcNow.Ticks);
            updateHistoryCommand.ExecuteNonQuery();
        }

        sqliteTransaction.Commit();

        if (failedCount > 0)
            DService.Instance().Log.Warning($"旧数据文件迁移完成，共导入 {importedTransactions.Count} 条，跳过 {failedCount} 条：{Path.GetFileName(file.FilePath)}");
        else
            DService.Instance().Log.Information($"旧数据文件迁移完成，共导入 {importedTransactions.Count} 条：{Path.GetFileName(file.FilePath)}");
    }

    private static IEnumerable<LegacyTransactionFile> EnumerateLegacyFiles(string dataFolder, ulong characterContentId)
    {
        var retainers = PluginConfig.Instance().CharacterRetainers.TryGetValue(characterContentId, out var retainerMap)
                            ? retainerMap.Keys.ToArray()
                            : Array.Empty<ulong>();

        foreach (var currencyId in PluginConfig.Instance().AllCurrencies.Keys)
        {
            foreach (var category in new[]
                     {
                         TransactionFileCategory.Inventory,
                         TransactionFileCategory.SaddleBag,
                         TransactionFileCategory.PremiumSaddleBag
                     })
            {
                var filePath = TransactionsHandler.GetLegacyTransactionFilePath(dataFolder, currencyId, category);
                if (!File.Exists(filePath)) continue;

                yield return new LegacyTransactionFile
                (
                    $"{currencyId}:{(int)category}:0",
                    filePath,
                    currencyId,
                    category,
                    0
                );
            }

            foreach (var retainerId in retainers)
            {
                var filePath = TransactionsHandler.GetLegacyTransactionFilePath
                (
                    dataFolder,
                    currencyId,
                    TransactionFileCategory.Retainer,
                    retainerId
                );

                if (!File.Exists(filePath)) continue;

                yield return new LegacyTransactionFile
                (
                    $"{currencyId}:{(int)TransactionFileCategory.Retainer}:{retainerId}",
                    filePath,
                    currencyId,
                    TransactionFileCategory.Retainer,
                    retainerId
                );
            }
        }
    }

    private sealed record LegacyTransactionFile
    (
        string                  FileKey,
        string                  FilePath,
        uint                    CurrencyId,
        TransactionFileCategory Category,
        ulong                   CategoryOwnerId
    );
}
