namespace CurrencyTracker.Manager
{
    public static class Transactions
    {
        // Transactions Type Suffix:
        // Inventory - {CurrencyName}.txt
        // Retainer - {CurrencyName}_{RetainerID}.txt
        // Saddle Bag - {CurrencyName}_SB.txt
        // Premium Saddle Bag - {CurrencyName}_PSB.txt
        public enum TransactionFileCategory
        {
            Inventory = 0,
            Retainer = 1,
            SaddleBag = 2,
            PremiumSaddleBag = 3,
        }

        /// <summary>
        /// Returns a file path that includes PlayerDataFolder
        /// </summary>
        public static string GetTransactionFilePath(uint CurrencyID, TransactionFileCategory category, ulong ID = 0)
        {
            var suffix = GetTransactionFileSuffix(category, ID);
            var path = Path.Combine(Plugin.Instance.PlayerDataFolder, $"{Plugin.Configuration.AllCurrencies[CurrencyID]}{suffix}.txt");
            return path;
        }

        public static string GetTransactionFileSuffix(TransactionFileCategory category, ulong ID = 0)
        {
            var suffix = string.Empty;
            switch (category)
            {
                case TransactionFileCategory.Inventory:
                    suffix = string.Empty;
                    break;
                case TransactionFileCategory.Retainer:
                    suffix = $"_{0}";
                    break;
                case TransactionFileCategory.SaddleBag:
                    suffix = "_SB";
                    break;
                case TransactionFileCategory.PremiumSaddleBag:
                    suffix = "_PSB";
                    break;
            }
            return suffix;
        }

        private static bool ValidityCheck(uint currencyID)
        {
            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty())
            {
                Service.Log.Warning("Player data folder Missed.");
                return false;
            }

            if (!Plugin.Configuration.AllCurrencies.ContainsKey(currencyID))
            {
                Service.Log.Error("Currency Missed");
                return false;
            }

            return true;
        }



        // 加载全部记录 Load All Transactions
        public static List<TransactionsConvertor> LoadAllTransactions(uint currencyID, TransactionFileCategory category = 0, ulong ID = 0)
        {
            var allTransactions = new List<TransactionsConvertor>();

            if (!ValidityCheck(currencyID)) return allTransactions;

            var filePath = GetTransactionFilePath(currencyID, category, ID);

            if (!File.Exists(filePath)) return allTransactions;

            try
            {
                allTransactions = TransactionsConvertor.FromFile(filePath);
            }
            catch (Exception ex)
            {
                Service.Log.Debug($"Fail to lode All Transactionsa from the data file: {ex.Message}");
            }

            return allTransactions;
        }

        // 加载最新一条记录 Load Latest Transaction
        public static TransactionsConvertor? LoadLatestSingleTransaction(uint currencyID, CharacterInfo? characterInfo = null, TransactionFileCategory category = 0, ulong ID = 0)
        {
            var playerDataFolder = characterInfo != null
                ? Path.Join(Plugin.Instance.PluginInterface.ConfigDirectory.FullName, $"{characterInfo.Name}_{characterInfo.Server}")
                : Plugin.Instance.PlayerDataFolder;

            var filePath = characterInfo != null
                ? Path.Join(playerDataFolder, $"{Plugin.Configuration.AllCurrencies[currencyID]}{GetTransactionFileSuffix(category, ID)}.txt")
                : GetTransactionFilePath(currencyID, category);

            if (characterInfo == null && !ValidityCheck(currencyID)) return null;
            if (!File.Exists(filePath)) return null;

            var lastLine = File.ReadLines(filePath).LastOrDefault();

            return lastLine == null ? new() : TransactionsConvertor.FromFileLine(lastLine);
        }

        // 加载指定范围内的记录 Load Transactions in Specific Range
        public static List<TransactionsConvertor> LoadTransactionsInRange(uint currencyID, int startIndex, int endIndex, TransactionFileCategory category = 0, ulong ID = 0)
        {
            var allTransactions = LoadAllTransactions(currencyID, category, ID);

            if (startIndex < 0 || startIndex >= allTransactions.Count || endIndex < 0 || endIndex >= allTransactions.Count)
            {
                Service.Log.Error("Invalid index range");
                return new();
            }

            return allTransactions.GetRange(startIndex, endIndex - startIndex + 1);
        }

        // 删除最新一条记录 Delete Latest Transaction
        public static bool DeleteLatestTransaction(uint currencyID, TransactionFileCategory category = 0, ulong ID = 0)
        {
            if (!ValidityCheck(currencyID)) return false;

            var filePath = GetTransactionFilePath(currencyID, category, ID);

            if (!File.Exists(filePath)) return false;

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var buffer = new byte[512];
            var endPosition = stream.Length - 1;
            for (var position = endPosition; position >= 0; position--)
            {
                stream.Position = position;
                stream.Read(buffer, 0, 1);

                if (buffer[0] == '\n')
                {
                    break;
                }
            }

            var lastLine = new StreamReader(stream).ReadLine();
            return true;
        }

        // 编辑最新一条记录 Edit Latest Transaction
        public static void EditLatestTransaction(uint currencyID, string LocationName = "", string Note = "", bool forceEdit = false, uint timeout = 10, bool onlyEditEmpty = false, TransactionFileCategory category = 0, ulong ID = 0)
        {
            if (!ValidityCheck(currencyID)) return;

            var editedTransaction = LoadLatestSingleTransaction(currencyID, null, category, ID);

            if (editedTransaction == null || (!forceEdit && (DateTime.Now - editedTransaction.TimeStamp).TotalSeconds > timeout) || (onlyEditEmpty && !string.IsNullOrEmpty(editedTransaction.Note))) return;

            if (!DeleteLatestTransaction(currencyID, category, ID)) return;

            AppendTransaction(currencyID, editedTransaction.TimeStamp, editedTransaction.Amount, editedTransaction.Change, LocationName.IsNullOrEmpty() ? editedTransaction.LocationName : LocationName, Note.IsNullOrEmpty() ? editedTransaction.Note : Note, category, ID);

            Plugin.Instance.Main.UpdateTransactions();
        }

        // 编辑指定记录 Edit Specific Transactions
        public static int EditSpecificTransactions(uint currencyID, List<TransactionsConvertor> selectedTransactions, string locationName = "", string noteContent = "", TransactionFileCategory category = 0, ulong ID = 0)
        {
            if (!selectedTransactions.Any()) return selectedTransactions.Count;

            var editedTransactions = LoadAllTransactions(currencyID, category, ID);
            var filePath = GetTransactionFilePath(currencyID, category, ID);

            var failCount = 0;

            foreach (var transaction in selectedTransactions)
            {
                var index = editedTransactions.FindIndex(t => IsTransactionEqual(t, transaction));

                if (index == -1)
                {
                    failCount++;
                    continue;
                }

                editedTransactions[index].LocationName = locationName.IsNullOrEmpty() ? editedTransactions[index].LocationName : locationName;
                editedTransactions[index].Note = noteContent.IsNullOrEmpty() ? editedTransactions[index].Note : noteContent;
            }

            TransactionsConvertor.WriteTransactionsToFile(filePath, editedTransactions);

            return failCount;
        }

        // 编辑全部货币最新一条记录 Edit All Currencies Latest Transaction
        public static void EditAllLatestTransaction(string LocationName = "", string Note = "", bool forceEdit = false, uint timeout = 10, bool onlyEditEmpty = false, TransactionFileCategory category = 0, ulong ID = 0)
        {
            if (Plugin.Instance.PlayerDataFolder.IsNullOrEmpty()) return;

            foreach (var currency in Plugin.Configuration.AllCurrencies)
            {
                EditLatestTransaction(currency.Key, LocationName, Note, forceEdit, timeout, onlyEditEmpty, category, ID);
            }
        }

        // 在数据末尾追加最新一条记录 Append One Transaction
        public static void AppendTransaction(uint currencyID, DateTime TimeStamp, long Amount, long Change, string LocationName, string Note, TransactionFileCategory category = 0, ulong ID = 0)
        {
            if (!ValidityCheck(currencyID)) return;

            var filePath = GetTransactionFilePath(currencyID, category, ID);

            TransactionsConvertor.AppendTransactionToFile(filePath, new List<TransactionsConvertor>
            {
                new()
                {
                    TimeStamp = TimeStamp,
                    Amount = Amount,
                    Change = Change,
                    LocationName = LocationName,
                    Note = Note
                }
            });
        }

        // 新建一条数据记录 Create One New Transaction
        public static void AddTransaction(uint currencyID, DateTime TimeStamp, long Amount, long Change, string LocationName, string Note, TransactionFileCategory category = 0, ulong ID = 0)
        {
            if (!ValidityCheck(currencyID)) return;

            var filePath = GetTransactionFilePath(currencyID, category, ID);

            TransactionsConvertor.WriteTransactionsToFile(filePath, new List<TransactionsConvertor>
            {
                new()
                {
                    TimeStamp = TimeStamp,
                    Amount = Amount,
                    Change = Change,
                    LocationName = LocationName,
                    Note = Note
                }
            });
        }

        // 根据时间重新排序文件内记录 Sort Transactions in File by Time
        public static void ReorderTransactions(uint currencyID, TransactionFileCategory category = 0, ulong ID = 0)
        {
            if (!ValidityCheck(currencyID)) return;

            TransactionsConvertor.WriteTransactionsToFile(
                GetTransactionFilePath(currencyID, category, ID),
                LoadAllTransactions(currencyID, category, ID).OrderBy(x => x.TimeStamp).ToList()
            );
        }

        // 按照临界值合并记录 Merge Transactions By Threshold
        public static int MergeTransactionsByLocationAndThreshold(uint currencyID, long threshold, bool isOneWayMerge, TransactionFileCategory category = 0, ulong ID = 0)
        {
            if (!ValidityCheck(currencyID)) return 0;

            var allTransactions = LoadAllTransactions(currencyID, category, ID);
            if (allTransactions.Count <= 1) return 0;

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

            TransactionsConvertor.WriteTransactionsToFile(GetTransactionFilePath(currencyID, category, ID), mergedTransactions);

            return mergedCount;
        }

        // 合并特定的记录 Merge Specific Transactions (等待测试)
        public static int MergeSpecificTransactions(uint currencyID, string LocationName, List<TransactionsConvertor> selectedTransactions, string NoteContent = "-1", TransactionFileCategory category = 0, ulong ID = 0)
        {
            if (!ValidityCheck(currencyID) || selectedTransactions.Count <= 1) return 0;

            var allTransactions = LoadAllTransactions(currencyID, category, ID);
            var filePath = GetTransactionFilePath(currencyID, category, ID);

            var latestTransaction = selectedTransactions
                .Select(t => allTransactions.FirstOrDefault(a => IsTransactionEqual(a, t)))
                .Where(t => t != null)
                .OrderByDescending(t => t.TimeStamp)
                .FirstOrDefault();

            if (latestTransaction == null) return 0;

            selectedTransactions.ForEach(t => allTransactions.Remove(t));

            var finalTransaction = new TransactionsConvertor
            {
                TimeStamp = latestTransaction.TimeStamp,
                Change = selectedTransactions.Sum(t => t.Change),
                LocationName = LocationName,
                Amount = latestTransaction.Amount,
                Note = NoteContent != "-1" ? NoteContent : $"({Service.Lang.GetText("MergedSpecificHelp", selectedTransactions.Count)})"
            };

            allTransactions.Add(finalTransaction);

            TransactionsConvertor.WriteTransactionsToFile(filePath, allTransactions);

            return selectedTransactions.Count;
        }

        // 清除异常记录 Clear Exceptional Records
        public static int ClearExceptionRecords(uint currencyID, TransactionFileCategory category = 0, ulong ID = 0)
        {
            if (!ValidityCheck(currencyID)) return 0;

            var allTransactions = LoadAllTransactions(currencyID, category, ID);
            var initialCount = allTransactions.Count;
            var index = 0;

            allTransactions.RemoveAll(transaction =>
                (index++ != 0 && transaction.Change == transaction.Amount) || transaction.Change == 0);

            if (allTransactions.Count == initialCount) return 0;

            TransactionsConvertor.WriteTransactionsToFile(GetTransactionFilePath(currencyID, category, ID), allTransactions);
            return initialCount - allTransactions.Count;
        }

        // 导出数据 Export Transactions Data
        public static string ExportData(List<TransactionsConvertor> data, string fileName, uint currencyID, int exportType, TransactionFileCategory category = 0, ulong ID = 0)
        {
            if (!ValidityCheck(currencyID)) return "Fail";

            var currencyName = Plugin.Configuration.AllCurrencies[currencyID];
            var fileExtension = exportType == 0 ? "csv" : "md";
            var headers = exportType == 0 ? Service.Lang.GetText("ExportFileCSVHeader") : $"{Service.Lang.GetText("ExportFileMDHeader")} {currencyName}\n\n{Service.Lang.GetText("ExportFileMDHeader1")}";
            var lineTemplate = exportType == 0 ? "{0},{1},{2},{3},{4}" : "| {0} | {1} | {2} | {3} | {4} |";

            if (exportType != 0 && exportType != 1)
            {
                Service.Chat.PrintError(Service.Lang.GetText("ExportFileHelp2"));
                return "Fail";
            }

            var playerDataFolder = Path.Combine(Plugin.Instance.PlayerDataFolder, "Exported");
            Directory.CreateDirectory(playerDataFolder);

            var nowTime = DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss");
            var finalFileName = string.IsNullOrWhiteSpace(fileName) ? $"{currencyName}_{nowTime}.{fileExtension}" : $"{fileName}_{currencyName}_{nowTime}.{fileExtension}";
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
