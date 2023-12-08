namespace CurrencyTracker.Manager;

public class TransactionsConvertor
{
    public DateTime TimeStamp { get; set; } // 时间戳 TimeStamp
    public long Change { get; set; } // 变化量 Change
    public long Amount { get; set; } // 总金额 Currency Amount
    public string LocationName { get; set; } = string.Empty; // 地名 Location Name
    public string Note { get; set; } = string.Empty; // 备注 Note

    // 将单行交易记录解析为字符串 Parse a transaction into string
    public string ToFileLine()
    {
        return $"{TimeStamp.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture)};{Amount};{Change};{LocationName};{Note}";
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

        parts[partIndex] = span.Slice(start, span.Length - start).ToString();

        var transaction = new TransactionsConvertor
        {
            TimeStamp = DateTime.ParseExact(parts[0], "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal),
            Amount = Convert.ToInt64(parts[1]),
            Change = Convert.ToInt64(parts[2]),
            LocationName = parts.Length > 3 ? parts[3] : Service.Lang.GetText("UnknownLocation"),
            Note = parts.Length > 4 ? parts[4] : string.Empty
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
                transactions.Add(transaction);
            }
        }
        catch (IOException ex)
        {
            Service.Log.Error($"Error parsing entire data file.: {ex.Message}");
        }

        return transactions;
    }


    // 同步将单个交易记录追加入数据文件 Append a transaction into the data file
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
            Service.Log.Error($"Fail to add individual transaction to the data file retroactively: {ex.Message}");
        }
    }

    // 同步将整个交易记录覆写进数据文件 Overwrite the data file
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
            Service.Log.Error($"Failed to overwrite the entire transactions to the data file: {ex.Message}");
        }
    }
}
