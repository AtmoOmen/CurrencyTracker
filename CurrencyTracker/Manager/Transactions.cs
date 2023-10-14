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
        private TransactionsConvertor transactionsConvertor = new TransactionsConvertor();
        private readonly List<TransactionsConvertor> temporarySingleTransactionList = new List<TransactionsConvertor>();
        internal static LanguageManager Lang = new LanguageManager(Plugin.Instance.Configuration.SelectedLanguage);

        public List<TransactionsConvertor> ClusterTransactionsByTime(List<TransactionsConvertor> transactions, TimeSpan interval)
        {
            var clusteredTransactions = new Dictionary<DateTime, TransactionsConvertor>();

            foreach (var transaction in transactions)
            {
                DateTime clusterTime = transaction.TimeStamp.AddTicks(-(transaction.TimeStamp.Ticks % interval.Ticks));
                if (!clusteredTransactions.ContainsKey(clusterTime))
                {
                    clusteredTransactions.Add(clusterTime, new TransactionsConvertor
                    {
                        TimeStamp = clusterTime,
                        Amount = 0,
                        Change = 0,
                        LocationName = string.Empty
                    });
                }

                if (!string.IsNullOrWhiteSpace(transaction.LocationName) && !transaction.LocationName.Equals("Unknown") && !transaction.LocationName.Equals("未知地点"))
                {
                    var cluster = clusteredTransactions[clusterTime];
                    if (string.IsNullOrWhiteSpace(cluster.LocationName))
                    {
                        cluster.LocationName = transaction.LocationName;
                    }
                    else
                    {
                        // 添加前三个LocationName
                        var locationNames = cluster.LocationName.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                        if (locationNames.Length < 3)
                        {
                            cluster.LocationName += $", {transaction.LocationName}";
                        }
                    }
                }

                clusteredTransactions[clusterTime].Change += transaction.Change;

                if (clusteredTransactions[clusterTime].TimeStamp <= transaction.TimeStamp)
                {
                    clusteredTransactions[clusterTime].Amount = transaction.Amount;
                }
            }

            foreach (var cluster in clusteredTransactions.Values)
            {
                if (!cluster.LocationName.EndsWith("..."))
                {
                    cluster.LocationName = cluster.LocationName.TrimEnd() + "...";
                }
            }

            return clusteredTransactions.Values.ToList();
        }

        public List<TransactionsConvertor> LoadAllTransactions(string CurrencyName)
        {
            List<TransactionsConvertor> allTransactions = new List<TransactionsConvertor>();

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

        public List<TransactionsConvertor> LoadLatestTransaction(string CurrencyName)
        {
            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Load Lastest Transaction: Player Data Folder Path Missed.");
                return new List<TransactionsConvertor>() ;
            }

            string filePath = Path.Combine(Plugin.Instance.PlayerDataFolder ?? "", $"{CurrencyName}.txt");

            List<TransactionsConvertor> allTransactions = TransactionsConvertor.FromFile(filePath, TransactionsConvertor.FromFileLine);

            List<TransactionsConvertor> latestTransactions = new List<TransactionsConvertor>();

            if (allTransactions.Count > 0)
            {
                TransactionsConvertor latestTransaction = allTransactions.Last();
                latestTransactions.Add(latestTransaction);
            }
            else
            {
                TransactionsConvertor defaultTransaction = new TransactionsConvertor
                {
                    TimeStamp = DateTime.Now,
                    Amount = 0,
                    Change = 0,
                    LocationName = Lang.GetText("UnknownLocation")
                };
                latestTransactions.Add(defaultTransaction);
            }

            return latestTransactions;
        }

        public List<TransactionsConvertor> LoadTransactionsInRange(string CurrencyName, int startIndex, int endIndex)
        {
            List<TransactionsConvertor> allTransactions = LoadAllTransactions(CurrencyName);

            if (startIndex < 0 || startIndex >= allTransactions.Count || endIndex < 0 || endIndex >= allTransactions.Count)
            {
                throw new ArgumentException("Invalid index range.");
            }

            List<TransactionsConvertor> transactionsInRange = new List<TransactionsConvertor>();
            for (int i = startIndex; i <= endIndex; i++)
            {
                transactionsInRange.Add(allTransactions[i]);
            }

            return transactionsInRange;
        }

        public TransactionsConvertor LoadLatestSingleTransaction(string CurrencyName)
        {
            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Load Lastest Single Transaction: Player Data Folder Path Missed.");
                return new TransactionsConvertor();
            }


            string filePath = Path.Combine(Plugin.Instance.PlayerDataFolder ?? "", $"{CurrencyName}.txt");

            List<TransactionsConvertor> allTransactions = TransactionsConvertor.FromFile(filePath, TransactionsConvertor.FromFileLine);

            TransactionsConvertor latestTransaction = allTransactions.LastOrDefault() ?? new TransactionsConvertor
            {
                TimeStamp = DateTime.Now,
                Amount = 0,
                Change = 0,
                LocationName = Lang.GetText("UnknownLocation")
            };

            return latestTransaction;
        }

        public void AppendTransaction(DateTime Timestamp, string CurrencyName, long Amount, long Change, string LocationName, string Note)
        {
            var singleTransaction = new TransactionsConvertor
            {
                TimeStamp = Timestamp,
                CurrencyName = CurrencyName,
                Amount = Amount,
                Change = Change,
                LocationName = LocationName,
                Note = Note
            };

            temporarySingleTransactionList.Add(singleTransaction);

            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Append Transaction: Player Data Folder Path Missed.");
                return;
            }

            string filePath = Path.Combine(Plugin.Instance.PlayerDataFolder ?? "", $"{CurrencyName}.txt");
            singleTransaction.AppendTransactionToFile(filePath, temporarySingleTransactionList);
            temporarySingleTransactionList.Clear();
        }

        public void AddTransaction(DateTime Timestamp, string CurrencyName, long Amount, long Change, string LocationName, string Note)
        {
            var Transaction = new TransactionsConvertor
            {
                TimeStamp = Timestamp,
                CurrencyName = CurrencyName,
                Amount = Amount,
                Change = Change,
                LocationName = LocationName,
                Note = Note
            };
            temporarySingleTransactionList.Add(Transaction);

            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Add Transaction: Player Data Folder Path Missed.");
                return;
            }

            string filePath = Path.Combine(Plugin.Instance.PlayerDataFolder ?? "", $"{CurrencyName}.txt");
            Transaction.WriteTransactionsToFile(filePath, temporarySingleTransactionList);
            temporarySingleTransactionList.Clear();
        }

        public int MergeTransactionsByLocationAndThreshold(string CurrencyName, long threshold, bool isOneWayMerge)
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
                    currentTransaction.Note = $"({Lang.GetText("MergedSpecificHelp")} {seperateMergedCount + 1} {Lang.GetText("MergedSpecificHelp1")})";
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

            string filePath = Path.Combine(Plugin.Instance.PlayerDataFolder ?? "", $"{CurrencyName}.txt");
            transactionsConvertor.WriteTransactionsToFile(filePath, mergedTransactions);

            return mergedCount;
        }

        public int MergeSpecificTransactions(string CurrencyName, string LocationName, List<TransactionsConvertor> selectedTransactions)
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
                        finalTransaction.Note = $"({Lang.GetText("MergedSpecificHelp")} {selectedTransactions.Count} {Lang.GetText("MergedSpecificHelp1")})";
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

            string filePath = Path.Combine(Plugin.Instance.PlayerDataFolder ?? "", $"{CurrencyName}.txt");
            transactionsConvertor.WriteTransactionsToFile(filePath, allTransactions);

            return selectedTransactions.Count;
        }

        public int ClearExceptionRecords(string selectedCurrencyName)
        {
            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Clear Transactions: Player Data Folder Path Missed.");
                return 0;
            }


            string filePath = Path.Join(Plugin.Instance.PlayerDataFolder ?? "", $"{selectedCurrencyName}.txt");

            List<TransactionsConvertor> allTransactions = TransactionsConvertor.FromFile(filePath, TransactionsConvertor.FromFileLine);
            List<TransactionsConvertor> recordsToRemove = new List<TransactionsConvertor>();

            for (int i = 0; i < allTransactions.Count; i++)
            {
                var transaction = allTransactions[i];

                if (i == 0 && transaction.Change == transaction.Amount)
                {
                    continue;
                }

                if (transaction.Change == 0 || transaction.Change == transaction.Amount)
                {
                    recordsToRemove.Add(transaction);
                }
            }

            if (recordsToRemove.Count > 0)
            {
                foreach (var record in recordsToRemove)
                {
                    allTransactions.Remove(record);
                }

                transactionsConvertor.WriteTransactionsToFile(filePath, allTransactions);

                return recordsToRemove.Count;
            }
            else
            {
                return 0;
            }
        }

        public string ExportData(List<TransactionsConvertor> data, string fileName, string selectedCurrencyName, int exportType)
        {
            string fileExtension;
            string headers;

            if (exportType == 0)
            {
                fileExtension = "csv";
                headers = Lang.GetText("ExportFileCSVHeader");
            }
            else if (exportType == 1)
            {
                fileExtension = "md";
                headers = $"{Lang.GetText("ExportFileMDHeader")} {selectedCurrencyName}\n\n" +
                          $"{Lang.GetText("ExportFileMDHeader1")}";
            }
            else
            {
                Service.Chat.PrintError(Lang.GetText("ExportFileHelp2"));
                return "Fail";
            }

            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Export Transactions: Player Data Folder Path Missed.");
                return "Fail";
            }

            string playerDataFolder = Path.Combine(Plugin.Instance.PlayerDataFolder, "Exported");
            Directory.CreateDirectory(playerDataFolder);

            string nowTime = DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss");

            string finalFileName;
            if (string.IsNullOrWhiteSpace(fileName)) finalFileName = $"{selectedCurrencyName}_{nowTime}.{fileExtension}";
            else finalFileName = $"{fileName}_{selectedCurrencyName}_{nowTime}.{fileExtension}";

            string filePath = Path.Combine(playerDataFolder, finalFileName);

            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.WriteLine(headers);

                foreach (var transaction in data)
                {
                    if (exportType == 0) // .csv
                    {
                        string line = $"{transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss")},{transaction.Amount},{transaction.Change},{transaction.LocationName},{transaction.Note}";
                        writer.WriteLine(line);
                    }
                    else if (exportType == 1) // .md
                    {
                        string line = $"| {transaction.TimeStamp.ToString("yyyy/MM/dd HH:mm:ss")} | {transaction.Amount} | {transaction.Change} | {transaction.LocationName} | {transaction.Note} |";
                        writer.WriteLine(line);
                    }
                }
            }
            return filePath;
        }

    }
}
