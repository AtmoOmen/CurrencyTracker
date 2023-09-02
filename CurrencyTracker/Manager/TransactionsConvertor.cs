using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Transactions;

namespace CurrencyTracker.Manager;

public class TransactionsConvertor
{
    public string CurrencyName { get; set; } = null!; // 货币类型 Currency Type
    public DateTime TimeStamp { get; set; } // 时间戳 TimeStamp
    public long Change { get; set; } // 变化量 Change
    public long Amount { get; set; } // 总金额 Currency Amount
    public string LocationName { get; set; } = string.Empty;

    private static readonly LanguageManager lang = new LanguageManager();

    // 将字典改变为字符串，存储至数据文件 Change the dic into strings and save the strings to the data file
    public string ToFileLine()
    {
        return $"{TimeStamp.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture)};{Amount};{Change};{LocationName}";
    }

    // 解析文件中的一行数据 Parse a line of transactions in the data file
    public static TransactionsConvertor FromFileLine(string line)
    {
        lang.LoadLanguage(Plugin.Instance.Configuration.SelectedLanguage);

        string[] parts = line.Split(";");

        TransactionsConvertor transaction = new TransactionsConvertor
        {
            TimeStamp = DateTime.ParseExact(parts[0], "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal),
            Amount = Convert.ToInt64(parts[1]),
            Change = Convert.ToInt64(parts[2])
        };

        if (parts.Length > 3)
        {
            transaction.LocationName = parts[3];
        }
        else
        {
            transaction.LocationName = lang.GetText("UnknownLocation");
        }

        return transaction;
    }

    // 解析整个数据文件 Parse a specific data file
    public static List<TransactionsConvertor> FromFile(string filePath, Func<string, TransactionsConvertor> parseLine)
    {
        List<TransactionsConvertor> transactions = new List<TransactionsConvertor>();

        try
        {
            if (!File.Exists(filePath))
            {
                return transactions;
            }

            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                TransactionsConvertor transaction = parseLine(line);
                transactions.Add(transaction);
            }
        }
        catch (Exception ex)
        {
            PluginLog.Debug($"Error parsing entire data file.: {ex.Message}");
        }

        return transactions;
    }

    // 同步将单个交易记录追加入数据文件(正常情况) Append a transaction into the data file (Normal Circumstances)
    public void AppendTransactionToFile(string filePath, List<TransactionsConvertor> singleTransaction)
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
            PluginLog.Debug($"Failure to add individual transaction to the data file retroactively: {ex.Message}");
        }
    }

    // 同步将整个交易记录覆写进数据文件(异常数据处理) Overwrite the data file (Exceptions)
    public void WriteTransactionsToFile(string filePath, List<TransactionsConvertor> transactions)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, string.Empty);
            }

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            using (var writer = new StreamWriter(fileStream))
            {
                foreach (var transaction in transactions)
                {
                    writer.WriteLine(transaction.ToFileLine());
                }
            }
        }
        catch (IOException ex)
        {
            PluginLog.Debug($"Failed to overwrite the entire transactions to the data file: {ex.Message}");
        }
    }
}
