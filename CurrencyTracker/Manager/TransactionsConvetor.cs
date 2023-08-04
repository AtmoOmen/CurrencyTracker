using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Dalamud.Logging;

namespace CurrencyTracker.Manager;

public class TransactionsConvetor
{
    public string CurrencyType { get; set; } = null!; // 货币类型
    public DateTime TimeStamp { get; set; } // 时间戳
    public long Change { get; set; } // 变化量
    public long Amount { get; set; } // 总金额

    // 将字典改变为字符串，主界面显示数据用
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(TimeStamp);
        sb.Append("\t ");
        sb.Append(Amount.ToString("#,##0"));
        sb.Append("\t ");
        sb.Append(Change.ToString("+ #,##0;- #,##0;0"));
        return sb.ToString();
    }

    // 将字典改变为字符串，存储至数据文件
    public string ToFileLine()
    {
        var sb = new StringBuilder();
        sb.Append(TimeStamp.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture));
        sb.Append(";");
        sb.Append(Amount);
        sb.Append(";");
        sb.Append(Change);
        return sb.ToString();
    }

    // 从文件中读取一行数据
    public static TransactionsConvetor FromFileLine(string line)
    {
        string[] parts = line.Split(";");
        return new TransactionsConvetor
        {
            TimeStamp = DateTime.ParseExact(parts[0], "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal),
            Amount = Convert.ToInt64(parts[1]),
            Change = Convert.ToInt64(parts[2])
        };
    }

    // 解析一整个文件
    public static List<TransactionsConvetor> FromFile(string filePath)
    {
        List<TransactionsConvetor> transactions = new List<TransactionsConvetor>();

        try
        {
            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                TransactionsConvetor transaction = TransactionsConvetor.FromFileLine(line);
                transactions.Add(transaction);
            }
        }
        catch (Exception ex)
        {
            PluginLog.Debug("解析整个数据文件时出现错误: " + ex.Message);
        }

        return transactions;
    }

    // 异步将单个交易记录追加入数据文件(正常情况)
    public async Task AppendTransactionToFileAsync(string filePath, List<TransactionsConvetor> singleTransaction)
    {
        try
        {
            using (var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (var writer = new StreamWriter(fileStream))
            {
                // 虽然用foreach，但保持singletransaction里时刻只有最新一条待处理的记录
                foreach (var transaction in singleTransaction)
                {
                    await writer.WriteLineAsync(transaction.ToFileLine());
                }
            }
            singleTransaction.Clear();
        }
        catch (IOException ex)
        {
            PluginLog.Debug("将单个交易记录追加入数据文件时失败: " + ex.Message);
        }
    }

    // 异步将整个交易记录覆写进数据文件(异常数据处理)
    public async Task WriteTransactionsToFileAsync(string filePath, List<TransactionsConvetor> transactions)
    {
        try
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            using (var writer = new StreamWriter(fileStream))
            {
                foreach (var transaction in transactions)
                {
                    await writer.WriteLineAsync(transaction.ToFileLine());
                }
            }
        }
        catch (IOException ex)
        {
            PluginLog.Debug("将整个交易记录覆写进数据文件失败: " + ex.Message);
        }
    }
}
