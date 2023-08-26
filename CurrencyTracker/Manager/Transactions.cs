using CurrencyTracker.Manger;
using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CurrencyTracker.Manager
{
    public class Transactions
    {
        private TransactionsConvetor? transactionsConvetor;
        private readonly List<TransactionsConvetor> temporarySingleTransactionList = new List<TransactionsConvetor>();
        private LanguageManager? lang;
        public string PlayerDataFolder = string.Empty;

        public void RemoveTransactions(string CurrencyName, List<TransactionsConvetor> transactionsToRemove)
        {
            transactionsConvetor ??= new TransactionsConvetor();
            string filePath = Path.Combine(PlayerDataFolder ?? "", $"{CurrencyName}.txt");
            var allTransactions = TransactionsConvetor.FromFile(filePath, TransactionsConvetor.FromFileLine);
            allTransactions.RemoveAll(transaction => transactionsToRemove.Contains(transaction));
            transactionsConvetor.WriteTransactionsToFile(filePath, allTransactions);
        }

        public List<TransactionsConvetor> ClusterTransactionsByTime(List<TransactionsConvetor> transactions, TimeSpan interval)
        {
            lang = new LanguageManager();
            lang.LoadLanguage(Plugin.GetPlugin.Configuration.SelectedLanguage);
            var clusteredTransactions = new Dictionary<DateTime, TransactionsConvetor>();

            foreach (var transaction in transactions)
            {
                DateTime clusterTime = transaction.TimeStamp.AddTicks(-(transaction.TimeStamp.Ticks % interval.Ticks));
                if (!clusteredTransactions.ContainsKey(clusterTime))
                {
                    clusteredTransactions.Add(clusterTime, new TransactionsConvetor
                    {
                        TimeStamp = clusterTime,
                        Amount = 0,
                        Change = 0,
                        LocationName = lang.GetText("UnknownLocation")
                    });
                }

                clusteredTransactions[clusterTime].Change += transaction.Change;

                if (clusteredTransactions[clusterTime].TimeStamp <= transaction.TimeStamp)
                {
                    clusteredTransactions[clusterTime].Amount = transaction.Amount;
                }
            }

            return clusteredTransactions.Values.ToList();
        }

        public List<TransactionsConvetor> LoadAllTransactions(string CurrencyName)
        {
            List<TransactionsConvetor> allTransactions = new List<TransactionsConvetor>();

            var playerName = Service.ClientState.LocalPlayer?.Name?.TextValue;
            var serverName = Service.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name;
            string playerDataFolder = Path.Combine(Plugin.GetPlugin.PluginInterface.ConfigDirectory.FullName, $"{playerName}_{serverName}");
            PlayerDataFolder = playerDataFolder;
            string filePath = Path.Combine(PlayerDataFolder ?? "", $"{CurrencyName}.txt");

            try
            {
                if (!File.Exists(filePath))
                {
                    return allTransactions;
                }

                allTransactions = TransactionsConvetor.FromFile(filePath, TransactionsConvetor.FromFileLine);

                if (Plugin.GetPlugin.Configuration.ReverseSort)
                {
                    allTransactions.Reverse();
                }
            }
            catch (Exception ex)
            {
                PluginLog.Debug("从数据文件中获取全部交易记录时出现错误: " + ex.Message);
            }

            return allTransactions;
        }

        public TransactionsConvetor LoadLatestSingleTransaction(string CurrencyName)
        {
            lang = new LanguageManager();
            lang.LoadLanguage(Plugin.GetPlugin.Configuration.SelectedLanguage);
            transactionsConvetor ??= new TransactionsConvetor();

            var playerName = Service.ClientState.LocalPlayer?.Name?.TextValue;
            var serverName = Service.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name;
            string playerDataFolder = Path.Combine(Plugin.GetPlugin.PluginInterface.ConfigDirectory.FullName, $"{playerName}_{serverName}");
            PlayerDataFolder = playerDataFolder;
            string filePath = Path.Combine(PlayerDataFolder ?? "", $"{CurrencyName}.txt");

            List<TransactionsConvetor> allTransactions = TransactionsConvetor.FromFile(filePath, TransactionsConvetor.FromFileLine);

            TransactionsConvetor latestTransaction = allTransactions.LastOrDefault() ?? new TransactionsConvetor
            {
                TimeStamp = DateTime.Now,
                Amount = 0,
                Change = 0,
                LocationName = lang.GetText("UnknownLocation")
            };

            return latestTransaction;
        }

        public void AppendTransaction(DateTime Timestamp, string CurrencyName, long Amount, long Change, string LocationName)
        {
            transactionsConvetor ??= new TransactionsConvetor();
            var singleTransaction = new TransactionsConvetor
            {
                TimeStamp = Timestamp,
                CurrencyName = CurrencyName,
                Amount = Amount,
                Change = Change,
                LocationName = LocationName
            };
            temporarySingleTransactionList.Add(singleTransaction);
            string filePath = Path.Combine(PlayerDataFolder ?? "", $"{CurrencyName}.txt");
            singleTransaction.AppendTransactionToFile(filePath, temporarySingleTransactionList);
            temporarySingleTransactionList.Clear();
        }

        public void AddTransaction(DateTime Timestamp, string CurrencyName, long Amount, long Change, string LocationName)
        {
            var Transaction = new TransactionsConvetor
            {
                TimeStamp = Timestamp,
                CurrencyName = CurrencyName,
                Amount = Amount,
                Change = Change,
                LocationName = LocationName
            };
            temporarySingleTransactionList.Add(Transaction);
            string filePath = Path.Combine(PlayerDataFolder ?? "", $"{CurrencyName}.txt");
            Transaction.WriteTransactionsToFile(filePath, temporarySingleTransactionList);
            temporarySingleTransactionList.Clear();
        }

        public int MergeTransactionsByLocationAndThreshold(string CurrencyName, long threshold)
        {
            transactionsConvetor ??= new TransactionsConvetor();

            var allTransactions = LoadAllTransactions(CurrencyName);

            if (allTransactions.Count <= 1)
            {
                return 0;
            }

            var mergedTransactions = new List<TransactionsConvetor>();
            int currentIndex = 0;
            int mergedCount = 0;

            while (currentIndex < allTransactions.Count)
            {
                var currentTransaction = allTransactions[currentIndex];
                var nextIndex = currentIndex + 1;

                while (nextIndex < allTransactions.Count &&
                       currentTransaction.LocationName == allTransactions[nextIndex].LocationName &&
                       Math.Abs(allTransactions[nextIndex].Change) < threshold)
                {
                    currentTransaction.Amount += allTransactions[nextIndex].Change;
                    currentTransaction.Change += allTransactions[nextIndex].Change;
                    currentTransaction.TimeStamp = allTransactions[nextIndex].TimeStamp;

                    nextIndex++;
                    mergedCount++;
                }

                mergedTransactions.Insert(0, currentTransaction);
                currentIndex = nextIndex;
            }

            string filePath = Path.Combine(PlayerDataFolder ?? "", $"{CurrencyName}.txt");
            transactionsConvetor.WriteTransactionsToFile(filePath, mergedTransactions);

            return mergedCount;
        }
    }
}
