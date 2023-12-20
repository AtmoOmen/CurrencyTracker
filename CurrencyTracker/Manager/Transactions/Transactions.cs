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
            var currencyName = CurrencyInfo.GetCurrencyName(CurrencyID);
            var path = Path.Join(Plugin.Instance.PlayerDataFolder, $"{currencyName}{suffix}.txt");
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
                    suffix = $"_{ID}";
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

            return true;
        }


        // 加载全部记录 Load All Transactions
        public static List<TransactionsConvertor> LoadAllTransactions(uint currencyID, TransactionFileCategory category = 0, ulong ID = 0)
        {
            var filePath = GetTransactionFilePath(currencyID, category, ID);

            return ValidityCheck(currencyID) && File.Exists(filePath)
                ? TransactionsConvertor.FromFile(filePath)
                : new();
        }

        // 加载最新一条记录 Load Latest Transaction
        public static TransactionsConvertor? LoadLatestSingleTransaction(uint currencyID, CharacterInfo? characterInfo = null, TransactionFileCategory category = 0, ulong ID = 0)
        {
            var playerDataFolder = characterInfo != null
                ? Path.Join(Plugin.Instance.PluginInterface.ConfigDirectory.FullName, $"{characterInfo.Name}_{characterInfo.Server}")
                : Plugin.Instance.PlayerDataFolder;

            var filePath = characterInfo != null
                ? Path.Join(playerDataFolder, $"{CurrencyInfo.GetCurrencyName(currencyID)}{GetTransactionFileSuffix(category, ID)}.txt")
                : GetTransactionFilePath(currencyID, category, ID);

            if (characterInfo == null && !ValidityCheck(currencyID)) return null;
            if (!File.Exists(filePath)) return null;

            var lastLine = File.ReadLines(filePath).Reverse().FirstOrDefault();

            return lastLine == null ? new() : TransactionsConvertor.FromFileLine(lastLine.AsSpan());
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

        // 新建一条数据记录 Create a New Transaction File with a transaction
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

        // 合并特定的记录 Merge Specific Transactions
        public static int MergeSpecificTransactions(uint currencyID, string LocationName, List<TransactionsConvertor> selectedTransactions, string NoteContent = "-1", TransactionFileCategory category = 0, ulong ID = 0)
        {
            if (!ValidityCheck(currencyID) || selectedTransactions.Count <= 1) return 0;

            var allTransactions = LoadAllTransactions(currencyID, category, ID);
            var filePath = GetTransactionFilePath(currencyID, category, ID);

            var latestTime = DateTime.MinValue;
            long overallChange = 0;
            long finalAmount = 0;
            var mergedCount = 0;

            foreach (var transaction in selectedTransactions)
            {
                var foundTransaction = allTransactions.FirstOrDefault(t => IsTransactionEqual(t, transaction));

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
            TransactionsConvertor.WriteTransactionsToFile(filePath, allTransactions);

            return mergedCount;
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

        // 备份数据 Backup transactions
        public static string BackupTransactions(string dataFolder, int maxBackupFilesCount)
        {
            if (dataFolder.IsNullOrEmpty()) return "Fail";

            var backupFolder = Path.Combine(dataFolder, "Backups");
            Directory.CreateDirectory(backupFolder);

            if (maxBackupFilesCount > 0)
            {
                var backupFiles = Directory.GetFiles(backupFolder, "*.zip")
                                           .OrderBy(f => new FileInfo(f).CreationTime)
                                           .ToList();

                while (backupFiles.Count >= maxBackupFilesCount)
                {
                    if (!IsFileLocked(new FileInfo(backupFiles[0])))
                    {
                        File.Delete(backupFiles[0]);
                    }
                    backupFiles.RemoveAt(0);
                }
            }

            var tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempFolder);

            var zipFilePath = string.Empty;
            try
            {
                foreach (var file in Directory.GetFiles(dataFolder))
                {
                    File.Copy(file, Path.Combine(tempFolder, Path.GetFileName(file)), true);
                }

                zipFilePath = Path.Combine(backupFolder, $"Backup_{DateTime.Now:yyyyMMddHHmmss}.zip");
                ZipFile.CreateFromDirectory(tempFolder, zipFilePath);
            }
            finally
            {
                Directory.Delete(tempFolder, true);
            }
            return zipFilePath;
        }
    }
}
