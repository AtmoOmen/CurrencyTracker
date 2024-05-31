using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CurrencyTracker.Manager.Transactions;

public class Transaction : IEquatable<Transaction>
{
    public DateTime TimeStamp { get; set; }                  // 时间戳 TimeStamp
    public long Change { get; set; }                         // 收支 Change
    public long Amount { get; set; }                         // 总金额 Currency Amount
    public string LocationName { get; set; } = string.Empty; // 地名 Location Name
    public string Note { get; set; } = string.Empty;         // 备注 Note

    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    // 清理文件名 Sanitize the file path
    public static string SanitizeFilePath(string filePath)
    {
        var invalidChars = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).Distinct();
        var fileName = Path.GetFileName(filePath);
        var path = Path.GetDirectoryName(filePath);

        foreach (var c in invalidChars) fileName = fileName.Replace(c.ToString(), "");

        return Path.Combine(path, fileName);
    }

    // 将单行交易记录解析为字符串 Parse a transaction into string
    public string ToFileLine()
    {
        return $"{TimeStamp.ToString("yyyy/MM/dd HH:mm:ss", InvariantCulture)};{Amount};{Change};{LocationName};{Note}";
    }

    // 将单行字符串解析为交易记录 Parse string into a transaction
    public static Transaction FromFileLine(ReadOnlySpan<char> span)
    {
        var parts = new string[5];
        var partIndex = 0;
        var start = 0;

        for (var i = 0; i < span.Length; i++)
            if (span[i] == ';')
            {
                parts[partIndex++] = span[start..i].ToString();
                start = i + 1;
            }

        parts[partIndex] = span[start..].ToString();

        if (!DateTime.TryParseExact(parts[0], "yyyy/MM/dd HH:mm:ss", InvariantCulture, DateTimeStyles.None,
                                    out var timeStamp))
            Service.Log.Error("Failed when try parse transaction's DateTime");

        if (!long.TryParse(parts[1], out var amount)) Service.Log.Error("Failed when try parse transaction's Amount");

        if (!long.TryParse(parts[2], out var change)) Service.Log.Error("Failed when try parse transaction's Change");

        var transaction = new Transaction
        {
            TimeStamp = timeStamp,
            Amount = amount,
            Change = change,
            LocationName = parts[3],
            Note = parts[4]
        };

        return transaction;
    }

    // 解析整个数据文件 Parse a data file
    public static List<Transaction> FromFile(string filePath)
    {
        filePath = SanitizeFilePath(filePath);
        if (!File.Exists(filePath)) return [];

        var transactions = new List<Transaction>();

        try
        {
            using var sr = new StreamReader(filePath);
            while (sr.ReadLine() is { } line)
            {
                var transaction = FromFileLine(line.AsSpan());
                transactions.Add(transaction);
            }
        }
        catch (IOException ex)
        {
            TransactionsHandler.BackupTransactions(P.PlayerDataFolder, Service.Config.MaxBackupFilesCount);
            Service.Log.Error($"Error parsing entire data file: {ex.Message}");
        }

        return transactions;
    }

    public static async Task<List<Transaction>> FromFileAsync(string filePath)
    {
        filePath = SanitizeFilePath(filePath);
        var transactions = new List<Transaction>();

        if (!File.Exists(filePath)) return transactions;

        try
        {
            using var sr = new StreamReader(filePath);
            while (await sr.ReadLineAsync() is { } line)
            {
                var transaction = FromFileLine(line.AsSpan());
                transactions.Add(transaction);
            }
        }
        catch (IOException ex)
        {
            await TransactionsHandler.BackupTransactionsAsync(P.PlayerDataFolder, Service.Config.MaxBackupFilesCount);
            Service.Log.Error($"Error parsing entire data file: {ex.Message}");
        }

        return transactions;
    }

    // 将单个交易记录追加入数据文件 Append a transaction into the data file
    public static void AppendTransactionToFile(string filePath, List<Transaction> singleTransaction)
    {
        filePath = SanitizeFilePath(filePath);
        try
        {
            using (var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (var writer = new StreamWriter(fileStream))
            {
                foreach (var transaction in singleTransaction) writer.WriteLine(transaction.ToFileLine());
            }

            singleTransaction.Clear();
        }
        catch (IOException ex)
        {
            TransactionsHandler.BackupTransactions(P.PlayerDataFolder, Service.Config.MaxBackupFilesCount);
            Service.Log.Error($"Fail to add individual transaction to the data file retroactively: {ex.Message}");
        }
    }

    public static async Task AppendTransactionToFileAsync(string filePath, List<Transaction> singleTransaction)
    {
        filePath = SanitizeFilePath(filePath);
        try
        {
            await using (var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            await using (var writer = new StreamWriter(fileStream))
            {
                foreach (var transaction in singleTransaction)
                {
                    await writer.WriteLineAsync(transaction.ToFileLine());
                }
            }

            singleTransaction.Clear();
        }
        catch (IOException ex)
        {
            await TransactionsHandler.BackupTransactionsAsync(P.PlayerDataFolder, Service.Config.MaxBackupFilesCount);
            Service.Log.Error($"Fail to add individual transaction to the data file retroactively: {ex.Message}");
        }
    }


    // 将整个交易记录覆写进数据文件 Overwrite the data file
    public static void WriteTransactionsToFile(string filePath, List<Transaction> transactions)
    {
        filePath = SanitizeFilePath(filePath);
        try
        {
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(fileStream);
            foreach (var transaction in transactions) writer.WriteLine(transaction.ToFileLine());
        }
        catch (IOException ex)
        {
            TransactionsHandler.BackupTransactions(P.PlayerDataFolder, Service.Config.MaxBackupFilesCount);
            Service.Log.Error($"Failed to overwrite the entire transactions to the data file: {ex.Message}");
        }
    }

    public static async Task WriteTransactionsToFileAsync(string filePath, List<Transaction> transactions)
    {
        filePath = SanitizeFilePath(filePath);
        try
        {
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            await using var writer = new StreamWriter(fileStream);
            foreach (var transaction in transactions)
            {
                await writer.WriteLineAsync(transaction.ToFileLine());
            }
        }
        catch (IOException ex)
        {
            await TransactionsHandler.BackupTransactionsAsync(P.PlayerDataFolder, Service.Config.MaxBackupFilesCount);
            Service.Log.Error($"Failed to overwrite the entire transactions to the data file: {ex.Message}");
        }
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Transaction);
    }

    public bool Equals(Transaction? other)
    {
        return other != null && TimeStamp == other.TimeStamp && Change == other.Change && Amount == other.Amount && LocationName == other.LocationName && Note == other.Note;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TimeStamp, Amount, LocationName, Note);
    }
}
