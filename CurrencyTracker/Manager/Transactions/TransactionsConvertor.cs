namespace CurrencyTracker.Manager;

public class TransactionsConvertor
{
    public DateTime TimeStamp { get; set; } // 时间戳 TimeStamp
    public long Change { get; set; } // 收支 Change
    public long Amount { get; set; } // 总金额 Currency Amount
    public string LocationName { get; set; } = string.Empty; // 地名 Location Name
    public string Note { get; set; } = string.Empty; // 备注 Note


    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    // 将单行交易记录解析为字符串 Parse a transaction into string
    public string ToFileLine()
    {
        return $"{TimeStamp.ToString("yyyy/MM/dd HH:mm:ss", InvariantCulture)};{Amount};{Change};{LocationName};{Note}";
    }

    // 将单行字符串解析为交易记录 Parese string into a transaction
    public static TransactionsConvertor FromFileLine(ReadOnlySpan<char> span)
    {
        var parts = new string[5];
        var partIndex = 0;
        var start = 0;

        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] == ';')
            {
                parts[partIndex++] = span.Slice(start, i - start).ToString();
                start = i + 1;
            }
        }
        parts[partIndex] = span.Slice(start).ToString();

        var transaction = new TransactionsConvertor
        {
            TimeStamp = DateTime.ParseExact(parts[0], "yyyy/MM/dd HH:mm:ss", InvariantCulture),
            Amount = long.Parse(parts[1]),
            Change = long.Parse(parts[2]),
            LocationName = parts[3],
            Note = parts[4]
        };

        return transaction;
    }

    // 解析整个数据文件 Parse a data file
    public static List<TransactionsConvertor> FromFile(string filePath)
    {
        var transactions = new List<TransactionsConvertor>();

        if (!File.Exists(filePath))
        {
            return transactions;
        }

        try
        {
            using var sr = new StreamReader(filePath);
            string? line;
            while ((line = sr.ReadLine()) != null)
            {
                var transaction = FromFileLine(line.AsSpan());
                /*
                if (Plugin.Configuration.MaxIgnoreDays != 0 && transaction.TimeStamp < DateTime.Now - TimeSpan.FromDays(Plugin.Configuration.MaxIgnoreDays))
                {
                    continue;
                }
                */
                transactions.Add(transaction);
            }
        }
        catch (IOException ex)
        {
            Transactions.BackupTransactions(Plugin.Instance.PlayerDataFolder, Plugin.Configuration.MaxBackupFilesCount);
            Service.Log.Error($"Error parsing entire data file: {ex.Message}");
        }

        return transactions;
    }

    // 将单个交易记录追加入数据文件 Append a transaction into the data file
    public static void AppendTransactionToFile(string filePath, List<TransactionsConvertor> singleTransaction)
    {
        try
        {
            using (var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (var writer = new StreamWriter(fileStream))
            {
                foreach (var transaction in singleTransaction)
                {
                    writer.WriteLine(transaction.ToFileLine());
                }
            }
            singleTransaction.Clear();
        }
        catch (IOException ex)
        {
            Transactions.BackupTransactions(Plugin.Instance.PlayerDataFolder, Plugin.Configuration.MaxBackupFilesCount);
            Service.Log.Error($"Fail to add individual transaction to the data file retroactively: {ex.Message}");
        }
    }

    // 将整个交易记录覆写进数据文件 Overwrite the data file
    public static void WriteTransactionsToFile(string filePath, List<TransactionsConvertor> transactions)
    {
        try
        {
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(fileStream);
            foreach (var transaction in transactions)
            {
                writer.WriteLine(transaction.ToFileLine());
            }
        }
        catch (IOException ex)
        {
            Transactions.BackupTransactions(Plugin.Instance.PlayerDataFolder, Plugin.Configuration.MaxBackupFilesCount);
            Service.Log.Error($"Failed to overwrite the entire transactions to the data file: {ex.Message}");
        }
    }
}
