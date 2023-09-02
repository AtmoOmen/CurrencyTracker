using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CurrencyTracker.Manager
{
    public partial class Transactions
    {
        private TransactionsConvertor? transactionsConvertor;
        private readonly List<TransactionsConvertor> temporarySingleTransactionList = new List<TransactionsConvertor>();
        private static readonly LanguageManager Lang = new LanguageManager();
        public string PlayerDataFolder = string.Empty;

        public List<TransactionsConvertor> ClusterTransactionsByTime(List<TransactionsConvertor> transactions, TimeSpan interval)
        {
            Lang.LoadLanguage(Plugin.Instance.Configuration.SelectedLanguage);
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
                        LocationName = Lang.GetText("UnknownLocation")
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

        public List<TransactionsConvertor> LoadAllTransactions(string CurrencyName)
        {
            List<TransactionsConvertor> allTransactions = new List<TransactionsConvertor>();

            var playerName = Service.ClientState.LocalPlayer?.Name?.TextValue;
            var serverName = Service.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name;
            var playerDataFolder = Path.Combine(Plugin.Instance.PluginInterface.ConfigDirectory.FullName, $"{playerName}_{serverName}");
            PlayerDataFolder = playerDataFolder;
            var filePath = Path.Combine(PlayerDataFolder ?? "", $"{CurrencyName}.txt");

            try
            {
                if (!File.Exists(filePath))
                {
                    return allTransactions;
                }

                allTransactions = TransactionsConvertor.FromFile(filePath, TransactionsConvertor.FromFileLine);

                if (Plugin.Instance.Configuration.ReverseSort)
                {
                    allTransactions.Reverse();
                }
            }
            catch (Exception ex)
            {
                PluginLog.Debug($"Error Loding All Transactionsa from the data file: {ex.Message}");
            }

            return allTransactions;
        }

        public TransactionsConvertor LoadLatestSingleTransaction(string CurrencyName)
        {
            Lang.LoadLanguage(Plugin.Instance.Configuration.SelectedLanguage);
            transactionsConvertor ??= new TransactionsConvertor();

            var playerName = Service.ClientState.LocalPlayer?.Name?.TextValue;
            var serverName = Service.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name;
            string playerDataFolder = Path.Combine(Plugin.Instance.PluginInterface.ConfigDirectory.FullName, $"{playerName}_{serverName}");
            PlayerDataFolder = playerDataFolder;
            string filePath = Path.Combine(PlayerDataFolder ?? "", $"{CurrencyName}.txt");

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

        public void AppendTransaction(DateTime Timestamp, string CurrencyName, long Amount, long Change, string LocationName)
        {
            transactionsConvertor ??= new TransactionsConvertor();
            var singleTransaction = new TransactionsConvertor
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
            var Transaction = new TransactionsConvertor
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

        public int MergeTransactionsByLocationAndThreshold(string CurrencyName, long threshold, bool isOneWayMerge)
        {
            transactionsConvertor ??= new TransactionsConvertor();

            var allTransactions = LoadAllTransactions(CurrencyName);

            if (allTransactions.Count <= 1)
            {
                return 0;
            }

            var mergedTransactions = new List<TransactionsConvertor>();
            var currentIndex = 0;
            var mergedCount = 0;

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
                        mergedCount++;
                    }
                    else
                    {
                        break;
                    }
                }

                mergedTransactions.Insert(0, currentTransaction);
                currentIndex = nextIndex;
            }

            string filePath = Path.Combine(PlayerDataFolder ?? "", $"{CurrencyName}.txt");
            transactionsConvertor.WriteTransactionsToFile(filePath, mergedTransactions);

            return mergedCount;
        }






    }
}
