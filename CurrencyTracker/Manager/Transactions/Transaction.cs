using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
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
        var invalidChars = Path.GetInvalidPathChars();
        var sanitizedPath = new StringBuilder(filePath.Length);

        foreach (var c in filePath)
        {
            sanitizedPath.Append(Array.IndexOf(invalidChars, c) < 0 ? c : '_');
        }

        return sanitizedPath.ToString();
    }

    // 将单行交易记录解析为字符串 Parse a transaction into string
    public string ToFileLine() 
        => $"{TimeStamp.ToString("yyyy/MM/dd HH:mm:ss", InvariantCulture)};{Amount};{Change};{LocationName};{Note}";

    // 将单行字符串解析为交易记录 Parse string into a transaction
    public static Transaction FromFileLine(ReadOnlySpan<char> span)
    {
        var parts = span.ToString().Split(';', 5);
        if (parts.Length != 5)
        {
            throw new FormatException("Invalid transaction format");
        }

        return new Transaction
        {
            TimeStamp = DateTime.ParseExact(parts[0], "yyyy/MM/dd HH:mm:ss", InvariantCulture),
            Amount = long.Parse(parts[1]),
            Change = long.Parse(parts[2]),
            LocationName = parts[3],
            Note = parts[4],
        };
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
            DService.Log.Error($"Error parsing entire data file: {ex.Message}");
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
            DService.Log.Error($"Error parsing entire data file: {ex.Message}");
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
            DService.Log.Error($"Fail to add individual transaction to the data file retroactively: {ex.Message}");
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
            DService.Log.Error($"Fail to add individual transaction to the data file retroactively: {ex.Message}");
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
            DService.Log.Error($"Failed to overwrite the entire transactions to the data file: {ex.Message}");
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
            DService.Log.Error($"Failed to overwrite the entire transactions to the data file: {ex.Message}");
        }
    }

    public override bool Equals(object? obj) => Equals(obj as Transaction);

    public bool Equals(Transaction? other) =>
        other != null &&
        TimeStamp == other.TimeStamp &&
        Change == other.Change &&
        Amount == other.Amount &&
        LocationName == other.LocationName &&
        Note == other.Note;

    public override int GetHashCode() => HashCode.Combine(TimeStamp, Amount, LocationName, Note);
}
