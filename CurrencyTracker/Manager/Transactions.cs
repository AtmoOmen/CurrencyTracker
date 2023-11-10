using CurrencyTracker.Windows;
using Dalamud.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CurrencyTracker.Manager
{
    public partial class Transactions
    {
        // 加载全部记录 Load All Transactions
        public static List<TransactionsConvertor> LoadAllTransactions(string CurrencyName)
        {
            var allTransactions = new List<TransactionsConvertor>();

            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Load All Transactions: Player Data Folder Path Missed.");
                return allTransactions;
            }
            var filePath = Path.Combine(Plugin.Instance.PlayerDataFolder ?? "", $"{CurrencyName}.txt");

            try
            {
                if (!File.Exists(filePath))
                {
                    return allTransactions;
                }

                allTransactions = TransactionsConvertor.FromFile(filePath, TransactionsConvertor.FromFileLine);
            }
            catch (Exception ex)
            {
                Service.PluginLog.Debug($"Error Loding All Transactionsa from the data file: {ex.Message}");
            }

            return allTransactions;
        }

        // 以列表形式加载最新一条记录 Load Latest Transaction in the Form of List
        public static List<TransactionsConvertor> LoadLatestTransaction(string CurrencyName)
        {
            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Load Lastest Transaction: Player Data Folder Path Missed.");
                return new List<TransactionsConvertor>();
            }

            var filePath = Path.Combine(Plugin.Instance.PlayerDataFolder ?? "", $"{CurrencyName}.txt");

            var allTransactions = TransactionsConvertor.FromFile(filePath, TransactionsConvertor.FromFileLine);

            var latestTransactions = new List<TransactionsConvertor>();

            if (allTransactions.Count > 0)
            {
                var latestTransaction = allTransactions.Last();
                latestTransactions.Add(latestTransaction);
            }
            else
            {
                var defaultTransaction = new TransactionsConvertor
                {
                    TimeStamp = DateTime.Now,
                    Amount = 0,
                    Change = 0,
                    LocationName = Service.Lang.GetText("UnknownLocation")
                };
                latestTransactions.Add(defaultTransaction);
            }

            return latestTransactions;
        }

        // 加载最新一条记录 Load Latest Transaction
        public static TransactionsConvertor LoadLatestSingleTransaction(string CurrencyName)
        {
            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Load Lastest Single Transaction: Player Data Folder Path Missed.");
                return new TransactionsConvertor();
            }

            var filePath = Path.Combine(Plugin.Instance.PlayerDataFolder ?? "", $"{CurrencyName}.txt");

            if (!File.Exists(filePath))
            {
                return new TransactionsConvertor();
            }

            var lastLine = File.ReadLines(filePath).Last();

            var latestTransaction = TransactionsConvertor.FromFileLine(lastLine);

            return latestTransaction;
        }

        // 加载指定范围内的记录 Load Transactions in Specific Range
        public static List<TransactionsConvertor> LoadTransactionsInRange(string CurrencyName, int startIndex, int endIndex)
        {
            List<TransactionsConvertor> allTransactions = LoadAllTransactions(CurrencyName);

            if (startIndex < 0 || startIndex >= allTransactions.Count || endIndex < 0 || endIndex >= allTransactions.Count)
            {
                throw new ArgumentException("Invalid index range.");
            }

            List<TransactionsConvertor> transactionsInRange = new List<TransactionsConvertor>();
            for (var i = startIndex; i <= endIndex; i++)
            {
                transactionsInRange.Add(allTransactions[i]);
            }

            return transactionsInRange;
        }

        // 删除最新一条记录 Delete Latest Transaction
        public static uint DeleteLatestTransaction(string CurrencyName)
        {
            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Delete Lastest Single Transaction: Player Data Folder Path Missed.");
                return 0;
            }

            var filePath = Path.Combine(Plugin.Instance.PlayerDataFolder ?? "", $"{CurrencyName}.txt");

            if (!File.Exists(filePath))
            {
                return 0;
            }

            var lines = File.ReadAllLines(filePath);

            if (lines.Length == 0)
            {
                return 0;
            }

            File.WriteAllLines(filePath, lines.Take(lines.Length - 1).ToArray());
            return 1;
        }

        // 编辑最新一条记录 Edit Latest Transaction
        public static void EditLatestTransaction(string CurrencyName, string LocationName = "None", string Note = "None", bool forceEdit = false)
        {
            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Edit Transaction: Player Data Folder Path Missed.");
                return;
            }

            if (!Plugin.Instance.Configuration.AllCurrencies.TryGetValue(CurrencyName, out _))
            {
                return;
            }

            var editedTransaction = LoadLatestSingleTransaction(CurrencyName);

            if (editedTransaction == null)
            {
                return;
            }

            if (!forceEdit)
            {
                if ((DateTime.Now - editedTransaction.TimeStamp).TotalSeconds > 10)
                {
                    return;
                }
            }

            if (DeleteLatestTransaction(CurrencyName) == 0)
            {
                return;
            }

            AppendTransaction(editedTransaction.TimeStamp, CurrencyName, editedTransaction.Amount, editedTransaction.Change, (LocationName == "None") ? editedTransaction.LocationName : LocationName, (Note == "None") ? editedTransaction.Note : Note);
        }

        // 在数据末尾追加最新一条记录 Append One Transaction
        public static void AppendTransaction(DateTime Timestamp, string CurrencyName, long Amount, long Change, string LocationName, string Note)
        {
            var singleTransaction = new TransactionsConvertor
            {
                TimeStamp = Timestamp,
                Amount = Amount,
                Change = Change,
                LocationName = LocationName,
                Note = Note
            };

            var tempList = new List<TransactionsConvertor>
            {
                singleTransaction
            };

            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Append Transaction: Player Data Folder Path Missed.");
                return;
            }

            var filePath = Path.Combine(Plugin.Instance.PlayerDataFolder ?? "", $"{CurrencyName}.txt");
            TransactionsConvertor.AppendTransactionToFile(filePath, tempList);
        }

        // 新建一条数据记录 Create One New Transaction
        public static void AddTransaction(DateTime Timestamp, string CurrencyName, long Amount, long Change, string LocationName, string Note)
        {
            var Transaction = new TransactionsConvertor
            {
                TimeStamp = Timestamp,
                Amount = Amount,
                Change = Change,
                LocationName = LocationName,
                Note = Note
            };

            var tempList = new List<TransactionsConvertor>
            {
                Transaction
            };

            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Add Transaction: Player Data Folder Path Missed.");
                return;
            }

            var filePath = Path.Combine(Plugin.Instance.PlayerDataFolder ?? "", $"{CurrencyName}.txt");
            TransactionsConvertor.WriteTransactionsToFile(filePath, tempList);
            tempList.Clear();
        }

        // 根据时间重新排序文件内记录 Sort Transactions in File by Time
        public static void ReorderTransactions(string CurrencyName)
        {
            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Reorder Transactions: Player Data Folder Path Missed.");
                return;
            }

            var allTransactions = LoadAllTransactions(CurrencyName);

            allTransactions = allTransactions.OrderBy(x => x.TimeStamp).ToList();

            var filePath = Path.Combine(Plugin.Instance.PlayerDataFolder ?? "", $"{CurrencyName}.txt");

            TransactionsConvertor.WriteTransactionsToFile(filePath, allTransactions);
        }

        // 按照临界值合并记录 Merge Transactions By Threshold
        public static int MergeTransactionsByLocationAndThreshold(string CurrencyName, long threshold, bool isOneWayMerge)
        {
            var allTransactions = LoadAllTransactions(CurrencyName);

            if (allTransactions.Count <= 1)
            {
                return 0;
            }

            var mergedTransactions = new List<TransactionsConvertor>();
            var currentIndex = 0;
            var mergedCount = 0;
            var seperateMergedCount = 0;

            while (currentIndex < allTransactions.Count)
            {
                var currentTransaction = allTransactions[currentIndex];
                var nextIndex = currentIndex + 1;

                while (nextIndex < allTransactions.Count &&
                    currentTransaction.LocationName == allTransactions[nextIndex].LocationName &&
                    Math.Abs(allTransactions[nextIndex].Change) < threshold)
                {
                    var nextTransaction = allTransactions[nextIndex];

                    if (!isOneWayMerge || (isOneWayMerge &&
                        (currentTransaction.Change >= 0 && nextTransaction.Change >= 0) ||
                        (currentTransaction.Change < 0 && nextTransaction.Change < 0)))
                    {
                        if (allTransactions[nextIndex].TimeStamp > currentTransaction.TimeStamp)
                        {
                            currentTransaction.Amount = nextTransaction.Amount;
                        }
                        currentTransaction.Change += nextTransaction.Change;
                        if (allTransactions[nextIndex].TimeStamp > currentTransaction.TimeStamp)
                        {
                            currentTransaction.TimeStamp = allTransactions[nextIndex].TimeStamp;
                        }

                        nextIndex++;
                        mergedCount += 2;
                        seperateMergedCount++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (seperateMergedCount > 0)
                {
                    currentTransaction.Note = $"({Service.Lang.GetText("MergedSpecificHelp", seperateMergedCount + 1)})";
                    seperateMergedCount = 0;
                }

                mergedTransactions.Add(currentTransaction);
                currentIndex = nextIndex;
            }

            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Merge Transactions: Player Data Folder Path Missed.");
                return 0;
            }

            var filePath = Path.Combine(Plugin.Instance.PlayerDataFolder ?? "", $"{CurrencyName}.txt");
            TransactionsConvertor.WriteTransactionsToFile(filePath, mergedTransactions);

            return mergedCount;
        }

        // 合并特定的记录 Merge Specific Transactions
        public static int MergeSpecificTransactions(string CurrencyName, string LocationName, List<TransactionsConvertor> selectedTransactions, string NoteContent = "-1")
        {
            var allTransactions = LoadAllTransactions(CurrencyName);
            var latestTime = DateTime.MinValue;
            long overallChange = 0;
            long finalAmount = 0;
            var currentIndex = 0;

            if (allTransactions.Count <= 1)
            {
                return 0;
            }

            foreach (var transaction in selectedTransactions)
            {
                var foundTransaction = allTransactions.FirstOrDefault(t => Widgets.IsTransactionEqual(t, transaction));

                if (foundTransaction == null)
                {
                    continue;
                }

                if (latestTime < foundTransaction.TimeStamp)
                {
                    latestTime = foundTransaction.TimeStamp;
                    finalAmount = foundTransaction.Amount;
                }

                overallChange += foundTransaction.Change;

                if (currentIndex != selectedTransactions.Count - 1)
                {
                    allTransactions.Remove(foundTransaction);
                }
                else if (currentIndex == selectedTransactions.Count - 1)
                {
                    var finalTransaction = allTransactions.FirstOrDefault(t => Widgets.IsTransactionEqual(t, transaction));
                    if (finalTransaction != null)
                    {
                        finalTransaction.TimeStamp = latestTime;
                        finalTransaction.Change = overallChange;
                        finalTransaction.LocationName = LocationName;
                        finalTransaction.Amount = finalAmount;
                        if (NoteContent != "-1")
                        {
                            finalTransaction.Note = NoteContent;
                        }
                        else
                        {
                            finalTransaction.Note = $"({Service.Lang.GetText("MergedSpecificHelp", selectedTransactions.Count)})";
                        }
                    }
                    else
                    {
                        Service.Chat.PrintError("Fail to Edit");
                    }
                }

                currentIndex++;
            }

            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Merge Transactions: Player Data Folder Path Missed.");
                return 0;
            }

            var filePath = Path.Combine(Plugin.Instance.PlayerDataFolder ?? "", $"{CurrencyName}.txt");
            TransactionsConvertor.WriteTransactionsToFile(filePath, allTransactions);

            return selectedTransactions.Count;
        }

        // 清除异常记录 Clear Exceptional Records
        public static int ClearExceptionRecords(string selectedCurrencyName)
        {
            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Clear Transactions: Player Data Folder Path Missed.");
                return 0;
            }

            var filePath = Path.Join(Plugin.Instance.PlayerDataFolder, $"{selectedCurrencyName}.txt");

            var allTransactions = TransactionsConvertor.FromFile(filePath, TransactionsConvertor.FromFileLine);

            var initialCount = allTransactions.Count;
            var index = 0;

            allTransactions.RemoveAll(transaction =>
                (index++ != 0 && transaction.Change == transaction.Amount) || transaction.Change == 0);

            if (allTransactions.Count != initialCount)
            {
                TransactionsConvertor.WriteTransactionsToFile(filePath, allTransactions);
                return initialCount - allTransactions.Count;
            }
            else
            {
                return 0;
            }
        }

        // 导出数据 Export Transactions Data
        public static string ExportData(List<TransactionsConvertor> data, string fileName, string selectedCurrencyName, int exportType)
        {
            string fileExtension;
            string headers;
            string lineTemplate;

            if (exportType == 0)
            {
                fileExtension = "csv";
                headers = Service.Lang.GetText("ExportFileCSVHeader");
                lineTemplate = "{0},{1},{2},{3},{4}";
            }
            else if (exportType == 1)
            {
                fileExtension = "md";
                headers = $"{Service.Lang.GetText("ExportFileMDHeader")} {selectedCurrencyName}\n\n" +
                          $"{Service.Lang.GetText("ExportFileMDHeader1")}";
                lineTemplate = "| {0} | {1} | {2} | {3} | {4} |";
            }
            else
            {
                Service.Chat.PrintError(Service.Lang.GetText("ExportFileHelp2"));
                return "Fail";
            }

            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Export Transactions: Player Data Folder Path Missed.");
                return "Fail";
            }

            var playerDataFolder = Path.Combine(Plugin.Instance.PlayerDataFolder, "Exported");
            if (!Directory.Exists(playerDataFolder))
            {
                Directory.CreateDirectory(playerDataFolder);
            }

            var nowTime = DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss");

            string finalFileName = string.IsNullOrWhiteSpace(fileName)
                ? $"{selectedCurrencyName}_{nowTime}.{fileExtension}"
                : $"{fileName}_{selectedCurrencyName}_{nowTime}.{fileExtension}";

            var filePath = Path.Combine(playerDataFolder, finalFileName);

            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.WriteLine(headers);

                foreach (var transaction in data)
                {
                    var line = string.Format(lineTemplate, transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss"), transaction.Amount, transaction.Change, transaction.LocationName, transaction.Note);
                    writer.WriteLine(line);
                }
            }
            return filePath;
        }
    }
}
