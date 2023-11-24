namespace CurrencyTracker.Manager
{
    public partial class Transactions
    {
        // 加载全部记录 Load All Transactions
        public static List<TransactionsConvertor> LoadAllTransactions(uint currencyID)
        {
            var allTransactions = new List<TransactionsConvertor>();

            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Load All Transactions: Player Data Folder Path Missed.");
                return allTransactions;
            }

            if (!Plugin.Instance.Configuration.AllCurrencies.TryGetValue(currencyID, out var currencyName))
            {
                Service.PluginLog.Error("Currency Missed");
                return allTransactions;
            }

            var filePath = Path.Combine(Plugin.Instance.PlayerDataFolder ?? "", $"{currencyName}.txt");

            if (!File.Exists(filePath))
            {
                return allTransactions;
            }

            try
            {
                allTransactions = TransactionsConvertor.FromFile(filePath);
            }
            catch (Exception ex)
            {
                Service.PluginLog.Debug($"Error Loding All Transactionsa from the data file: {ex.Message}");
            }

            return allTransactions;
        }

        // 以列表形式加载最新一条记录 Load Latest Transaction in the Form of List
        public static List<TransactionsConvertor> LoadLatestTransaction(uint currencyID)
        {
            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Load Lastest Transaction: Player Data Folder Path Missed.");
                return new();
            }

            if (!Plugin.Instance.Configuration.AllCurrencies.TryGetValue(currencyID, out var currencyName))
            {
                Service.PluginLog.Error("Currency Missed");
                return new();
            }

            var filePath = Path.Combine(Plugin.Instance.PlayerDataFolder, $"{currencyName}.txt");

            var allTransactions = TransactionsConvertor.FromFile(filePath);

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
        public static TransactionsConvertor LoadLatestSingleTransaction(uint currencyID)
        {
            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Load Lastest Single Transaction: Player Data Folder Path Missed.");
                return new();
            }

            if (!Plugin.Instance.Configuration.AllCurrencies.TryGetValue(currencyID, out var currencyName))
            {
                Service.PluginLog.Error("Currency Missed");
                return new();
            }

            var filePath = Path.Combine(Plugin.Instance.PlayerDataFolder, $"{currencyName}.txt");

            if (!File.Exists(filePath))
            {
                return new();
            }

            string? lastLine = null;
            using (var stream = File.OpenRead(filePath))
            {
                stream.Position = Math.Max(0, stream.Length - 512);
                using var reader = new StreamReader(stream);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    lastLine = line;
                }
            }

            if (lastLine == null)
            {
                return new();
            }

            var latestTransaction = TransactionsConvertor.FromFileLine(lastLine);

            return latestTransaction;
        }

        // 加载指定范围内的记录 Load Transactions in Specific Range
        public static List<TransactionsConvertor> LoadTransactionsInRange(uint currencyID, int startIndex, int endIndex)
        {
            var allTransactions = LoadAllTransactions(currencyID);

            if (startIndex < 0 || startIndex >= allTransactions.Count || endIndex < 0 || endIndex >= allTransactions.Count)
            {
                throw new ArgumentException("Invalid index range.");
            }

            var transactionsInRange = new List<TransactionsConvertor>();
            for (var i = startIndex; i <= endIndex; i++)
            {
                transactionsInRange.Add(allTransactions[i]);
            }

            return transactionsInRange;
        }

        // 删除最新一条记录 Delete Latest Transaction
        public static uint DeleteLatestTransaction(uint currencyID)
        {
            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Delete Lastest Single Transaction: Player Data Folder Path Missed.");
                return 0;
            }

            if (!Plugin.Instance.Configuration.AllCurrencies.TryGetValue(currencyID, out var currencyName))
            {
                Service.PluginLog.Error("Currency Missed");
                return 0;
            }

            var filePath = Path.Combine(Plugin.Instance.PlayerDataFolder ?? "", $"{currencyName}.txt");

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
        public static void EditLatestTransaction(uint currencyID, string LocationName = "None", string Note = "None", bool forceEdit = false, uint timeout = 10)
        {
            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Edit Transaction: Player Data Folder Path Missed.");
                return;
            }

            if (!Plugin.Instance.Configuration.AllCurrencies.TryGetValue(currencyID, out var currencyName))
            {
                Service.PluginLog.Error("Currency Missed");
                return;
            }

            if (!Plugin.Instance.Configuration.AllCurrencies.ContainsValue(currencyName))
            {
                return;
            }

            var editedTransaction = LoadLatestSingleTransaction(currencyID);

            if (editedTransaction == null)
            {
                return;
            }

            if (!editedTransaction.Note.IsNullOrEmpty())
            {
                return;
            }

            if (!forceEdit)
            {
                if ((DateTime.Now - editedTransaction.TimeStamp).TotalSeconds > timeout)
                {
                    return;
                }
            }

            if (DeleteLatestTransaction(currencyID) == 0)
            {
                return;
            }

            AppendTransaction(currencyID, editedTransaction.TimeStamp, editedTransaction.Amount, editedTransaction.Change, (LocationName == "None") ? editedTransaction.LocationName : LocationName, (Note == "None") ? editedTransaction.Note : Note);
        }

        // 在数据末尾追加最新一条记录 Append One Transaction
        public static void AppendTransaction(uint currencyID, DateTime TimeStamp, long Amount, long Change, string LocationName, string Note)
        {
            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Append Transaction: Player Data Folder Path Missed.");
                return;
            }

            if (!Plugin.Instance.Configuration.AllCurrencies.TryGetValue(currencyID, out var currencyName))
            {
                Service.PluginLog.Error("Currency Missed");
                return;
            }

            var singleTransaction = new TransactionsConvertor
            {
                TimeStamp = TimeStamp,
                Amount = Amount,
                Change = Change,
                LocationName = LocationName,
                Note = Note
            };

            var tempList = new List<TransactionsConvertor>
            {
                singleTransaction
            };

            var filePath = Path.Combine(Plugin.Instance.PlayerDataFolder, $"{currencyName}.txt");
            TransactionsConvertor.AppendTransactionToFile(filePath, tempList);
        }

        // 新建一条数据记录 Create One New Transaction
        public static void AddTransaction(uint currencyID, DateTime TimeStamp, long Amount, long Change, string LocationName, string Note)
        {
            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Append Transaction: Player Data Folder Path Missed.");
                return;
            }

            if (!Plugin.Instance.Configuration.AllCurrencies.TryGetValue(currencyID, out var currencyName))
            {
                Service.PluginLog.Error("Currency Missed");
                return;
            }

            var Transaction = new TransactionsConvertor
            {
                TimeStamp = TimeStamp,
                Amount = Amount,
                Change = Change,
                LocationName = LocationName,
                Note = Note
            };

            var tempList = new List<TransactionsConvertor>
            {
                Transaction
            };

            var filePath = Path.Combine(Plugin.Instance.PlayerDataFolder ?? "", $"{currencyName}.txt");
            TransactionsConvertor.WriteTransactionsToFile(filePath, tempList);
            tempList.Clear();
        }

        // 根据时间重新排序文件内记录 Sort Transactions in File by Time
        public static void ReorderTransactions(uint currencyID)
        {
            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Reorder Transactions: Player Data Folder Path Missed.");
                return;
            }

            if (!Plugin.Instance.Configuration.AllCurrencies.TryGetValue(currencyID, out var currencyName))
            {
                Service.PluginLog.Error("Currency Missed");
                return;
            }

            var allTransactions = LoadAllTransactions(currencyID);

            allTransactions = allTransactions.OrderBy(x => x.TimeStamp).ToList();

            var filePath = Path.Combine(Plugin.Instance.PlayerDataFolder ?? "", $"{currencyName}.txt");

            TransactionsConvertor.WriteTransactionsToFile(filePath, allTransactions);
        }

        // 按照临界值合并记录 Merge Transactions By Threshold
        public static int MergeTransactionsByLocationAndThreshold(uint currencyID, long threshold, bool isOneWayMerge)
        {
            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Merge Transactions: Player Data Folder Path Missed.");
                return 0;
            }

            if (!Plugin.Instance.Configuration.AllCurrencies.TryGetValue(currencyID, out var currencyName))
            {
                Service.PluginLog.Error("Currency Missed");
                return 0;
            }

            var allTransactions = LoadAllTransactions(currencyID);

            if (allTransactions.Count <= 1)
            {
                return 0;
            }

            var mergedTransactions = new List<TransactionsConvertor>();
            var mergedCount = 0;

            for (var i = 0; i < allTransactions.Count;)
            {
                var currentTransaction = allTransactions[i];
                var seperateMergedCount = 0;

                while (++i < allTransactions.Count &&
                    currentTransaction.LocationName == allTransactions[i].LocationName &&
                    Math.Abs(allTransactions[i].Change) < threshold)
                {
                    var nextTransaction = allTransactions[i];

                    if (!isOneWayMerge || (isOneWayMerge &&
                        (currentTransaction.Change >= 0 && nextTransaction.Change >= 0) ||
                        (currentTransaction.Change < 0 && nextTransaction.Change < 0)))
                    {
                        if (nextTransaction.TimeStamp > currentTransaction.TimeStamp)
                        {
                            currentTransaction.Amount = nextTransaction.Amount;
                            currentTransaction.TimeStamp = nextTransaction.TimeStamp;
                        }
                        currentTransaction.Change += nextTransaction.Change;

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
                }

                mergedTransactions.Add(currentTransaction);
            }

            var filePath = Path.Combine(Plugin.Instance.PlayerDataFolder ?? "", $"{currencyName}.txt");
            TransactionsConvertor.WriteTransactionsToFile(filePath, mergedTransactions);

            return mergedCount;
        }

        // 合并特定的记录 Merge Specific Transactions
        public static int MergeSpecificTransactions(uint currencyID, string LocationName, List<TransactionsConvertor> selectedTransactions, string NoteContent = "-1")
        {
            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Merge Transactions: Player Data Folder Path Missed.");
                return 0;
            }

            if (!Plugin.Instance.Configuration.AllCurrencies.TryGetValue(currencyID, out var currencyName))
            {
                Service.PluginLog.Error("Currency Missed");
                return 0;
            }

            var allTransactions = LoadAllTransactions(currencyID);

            if (allTransactions.Count <= 1)
            {
                return 0;
            }

            var latestTime = DateTime.MinValue;
            long overallChange = 0;
            long finalAmount = 0;
            var mergedCount = 0;

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
                allTransactions.Remove(foundTransaction);
                mergedCount++;
            }

            var finalTransaction = new TransactionsConvertor
            {
                TimeStamp = latestTime,
                Change = overallChange,
                LocationName = LocationName,
                Amount = finalAmount,
                Note = NoteContent != "-1" ? NoteContent : $"({Service.Lang.GetText("MergedSpecificHelp", mergedCount)})"
            };

            allTransactions.Add(finalTransaction);

            var filePath = Path.Combine(Plugin.Instance.PlayerDataFolder, $"{currencyName}.txt");
            TransactionsConvertor.WriteTransactionsToFile(filePath, allTransactions);

            return mergedCount;
        }

        // 清除异常记录 Clear Exceptional Records
        public static int ClearExceptionRecords(uint currencyID)
        {
            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.PluginLog.Warning("Fail to Clear Transactions: Player Data Folder Path Missed.");
                return 0;
            }

            if (!Plugin.Instance.Configuration.AllCurrencies.TryGetValue(currencyID, out var currencyName))
            {
                Service.PluginLog.Error("Currency Missed");
                return 0;
            }

            var filePath = Path.Join(Plugin.Instance.PlayerDataFolder, $"{currencyName}.txt");

            var allTransactions = TransactionsConvertor.FromFile(filePath);

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
        public static string ExportData(List<TransactionsConvertor> data, string fileName, uint currencyID, int exportType)
        {
            if (!Plugin.Instance.Configuration.AllCurrencies.TryGetValue(currencyID, out var currencyName))
            {
                Service.PluginLog.Error("Currency Missed");
                return string.Empty;
            }

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
                headers = $"{Service.Lang.GetText("ExportFileMDHeader")} {currencyName}\n\n" +
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
                ? $"{currencyName}_{nowTime}.{fileExtension}"
                : $"{fileName}_{currencyName}_{nowTime}.{fileExtension}";

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
