using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.IO;
using Dalamud.Logging;
using CurrencyTracker.Manger;

namespace CurrencyTracker.Manager;

public class TransactionsConvetor
{
    public string CurrencyName { get; set; } = null!; // 货币类型
    public DateTime TimeStamp { get; set; } // 时间戳
    public long Change { get; set; } // 变化量
    public long Amount { get; set; } // 总金额
    public string LocationName { get; set; } = string.Empty;

    private static readonly LanguageManager lang = new LanguageManager();


    // 将字典改变为字符串，存储至数据文件
    public string ToFileLine()
    {
        return $"{TimeStamp.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture)};{Amount};{Change};{LocationName}";
    }


    // 解析文件中的一行数据
    public static TransactionsConvetor FromFileLine(string line)
    {
        lang.LoadLanguage(Plugin.GetPlugin.Configuration.SelectedLanguage);

        string[] parts = line.Split(";");

        TransactionsConvetor transaction = new TransactionsConvetor
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
            transaction.LocationName = lang.GetText("UnknonLocation");
        }

        return transaction;
    }



    // 解析整个数据文件
    public static List<TransactionsConvetor> FromFile(string filePath, Func<string, TransactionsConvetor> parseLine)
    {
        List<TransactionsConvetor> transactions = new List<TransactionsConvetor>();

        try
        {
            // 提前检查文件是否存在，如果不存在直接返回空列表
            if (!File.Exists(filePath))
            {
                return transactions;
            }

            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                TransactionsConvetor transaction = parseLine(line);
                transactions.Add(transaction);
            }
        }
        catch (Exception ex)
        {
            PluginLog.Debug("解析整个数据文件时出现错误 / Error parsing entire data file.: " + ex.Message);
        }

        return transactions;
    }


    // 同步将单个交易记录追加入数据文件(正常情况)
    public void AppendTransactionToFile(string filePath, List<TransactionsConvetor> singleTransaction)
    {
        try
        {
            using (var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (var writer = new StreamWriter(fileStream))
            {
                // 虽然用foreach，但保持singletransaction里时刻只有最新一条待处理的记录
                foreach (var transaction in singleTransaction)
                {
                    writer.WriteLine(transaction.ToFileLine());
                }
            }
            singleTransaction.Clear();
        }
        catch (IOException ex)
        {
            PluginLog.Debug("将单个交易记录追加入数据文件时失败 / Failure to add individual transaction to the data file retroactively: " + ex.Message);
        }
    }

    // 同步将整个交易记录覆写进数据文件(异常数据处理)
    public void WriteTransactionsToFile(string filePath, List<TransactionsConvetor> transactions)
    {
        try
        {
            // 提前检查文件是否存在，如果不存在则创建一个空的文件
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, string.Empty);
            }

            // 然后进行正常的写入操作
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
            PluginLog.Debug("将整个交易记录覆写进数据文件失败 / Failed to overwrite the entire transactions to the data file: " + ex.Message);
        }
    }


}
