using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CurrencyTracker.Manager
{
    public class Transactions
    {
        public Configuration? Configuration { get; set; }
        private TransactionsConvetor? transactionsConvetor;
        // 临时存放单一交易记录
        private List<TransactionsConvetor> temporarySingleTransactionList = new List<TransactionsConvetor>();
        // 存放单种货币的所有交易记录
        public List<TransactionsConvetor> transactionsList = new List<TransactionsConvetor>();
        // 存放单种货币的最新一条交易记录
        public List<TransactionsConvetor> singleTransactionList = new List<TransactionsConvetor>();
        // 存放玩家数据文件夹路径
        public string PlayerDataFolder = string.Empty;

        // 按时间聚类
        public List<TransactionsConvetor> ClusterTransactionsByTime(List<TransactionsConvetor> transactions, TimeSpan interval)
        {
            Dictionary<DateTime, TransactionsConvetor> clusteredTransactions = new Dictionary<DateTime, TransactionsConvetor>();

            foreach (var transaction in transactions)
            {
                DateTime clusterTime = transaction.TimeStamp.AddTicks(-(transaction.TimeStamp.Ticks % interval.Ticks));
                if (!clusteredTransactions.ContainsKey(clusterTime))
                {
                    clusteredTransactions.Add(clusterTime, new TransactionsConvetor
                    {
                        TimeStamp = clusterTime,
                        Amount = 0,
                        Change = 0
                    });
                }

                clusteredTransactions[clusterTime].Amount += transaction.Amount;
                clusteredTransactions[clusterTime].Change += transaction.Change;
            }

            return clusteredTransactions.Values.ToList();
        }


        // 从文件加载全部的交易记录
        public List<TransactionsConvetor> LoadAllTransactions(string CurrencyName)
        {
            List<TransactionsConvetor> allTransactions = new List<TransactionsConvetor>();

            var playerName = Service.ClientState.LocalPlayer?.Name?.TextValue;
            var serverName = Service.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name;
            string playerDataFolder = Path.Join(Plugin.GetPlugin.PluginInterface.ConfigDirectory.FullName, $"{playerName}_{serverName}");

            PlayerDataFolder = playerDataFolder;

            string filePath = Path.Join(PlayerDataFolder ?? "", $"{CurrencyName}.txt");

            try
            {
                // 提前检查文件是否存在，如果不存在直接返回空列表
                if (!File.Exists(filePath))
                {
                    return allTransactions;
                }

                string[] lines = File.ReadAllLines(filePath);
                foreach (string line in lines)
                {
                    TransactionsConvetor transaction = TransactionsConvetor.FromFileLine(line);
                    allTransactions.Add(transaction);
                }
            }
            catch (Exception ex)
            {
                PluginLog.Debug("从数据文件中获取全部交易记录时出现错误: " + ex.Message);
            }
            if (Plugin.GetPlugin.Configuration.ReverseSort)
            {
                allTransactions.Reverse();
                return allTransactions;
            }
            return allTransactions;
            
        }

        // 从文件加载最新一条交易记录
        public TransactionsConvetor LoadLatestSingleTransaction(string CurrencyName)
        {
            transactionsConvetor ??= new TransactionsConvetor();

            var playerName = Service.ClientState.LocalPlayer?.Name?.TextValue;
            var serverName = Service.ClientState.LocalPlayer?.HomeWorld?.GameData?.Name;
            string playerDataFolder = Path.Join(Plugin.GetPlugin.PluginInterface.ConfigDirectory.FullName, $"{playerName}_{serverName}");

            PlayerDataFolder = playerDataFolder;

            string filePath = Path.Join(PlayerDataFolder ?? "", $"{CurrencyName}.txt");

            List<TransactionsConvetor> allTransactions = TransactionsConvetor.FromFile(filePath, TransactionsConvetor.FromFileLine);

            TransactionsConvetor latestTransaction = allTransactions.LastOrDefault();

            if (latestTransaction != null)
            {
                return latestTransaction;
            }
            else
            {
                return new TransactionsConvetor
                {
                    TimeStamp = DateTime.Now,
                    Amount = 0,
                    Change = 0
                };
            }
        }



        // 追加交易记录
        public void AppendTransaction(DateTime Timestamp, string CurrencyName, long Amount, long Change)
        {
            transactionsConvetor ??= new TransactionsConvetor();
            var singleTransaction = new TransactionsConvetor
            {
                TimeStamp = Timestamp,
                CurrencyName = CurrencyName,
                Amount = Amount,
                Change = Change
            };
            temporarySingleTransactionList.Add(singleTransaction);

            string filePath = Path.Join(PlayerDataFolder ?? "", $"{CurrencyName}.txt");

            singleTransaction.AppendTransactionToFile(filePath, temporarySingleTransactionList);

            temporarySingleTransactionList.Clear();
        }

        // 添加交易记录
        public void AddTransaction(DateTime Timestamp, string CurrencyName, long Amount, long Change)
        {

            var Transaction = new TransactionsConvetor
            {
                TimeStamp = Timestamp,
                CurrencyName = CurrencyName,
                Amount = Amount,
                Change = Change
            };
            temporarySingleTransactionList.Add(Transaction);

            string filePath = Path.Join(PlayerDataFolder ?? "", $"{CurrencyName}.txt");

            Transaction.WriteTransactionsToFile(filePath, temporarySingleTransactionList);

            temporarySingleTransactionList.Clear();
        }
    }
}
