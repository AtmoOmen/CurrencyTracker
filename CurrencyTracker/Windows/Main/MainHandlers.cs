namespace CurrencyTracker.Windows;

public partial class Main
{
    // 用于处理选项增减 Used to handle options changes.
    private void ReloadOrderedOptions()
    {
        var orderedOptionsSet = new HashSet<uint>(C.OrderedOptions);
        var allCurrenciesSet = new HashSet<uint>(C.AllCurrencies.Keys);

        if (!orderedOptionsSet.SetEquals(allCurrenciesSet))
        {
            orderedOptionsSet.UnionWith(allCurrenciesSet);
            orderedOptionsSet.IntersectWith(allCurrenciesSet);

            C.OrderedOptions = orderedOptionsSet.ToList();
            C.Save();
        }
    }

    // 用于处理选项位置变化 Used to handle option's position change.
    private void SwapOptions(int index1, int index2)
    {
        (C.OrderedOptions[index2], C.OrderedOptions[index1]) = (C.OrderedOptions[index1], C.OrderedOptions[index2]);
        C.Save();
    }

    // 按收支隐藏不符合要求的交易记录 Hide Unmatched Transactions By Change
    private List<TransactionsConvertor> ApplyChangeFilter(List<TransactionsConvertor> transactions)
    {
        var filteredTransactions = new List<TransactionsConvertor>();

        foreach (var transaction in transactions)
        {
            var isTransactionValid = filterMode == 0 ?
                transaction.Change > filterValue :
                transaction.Change < filterValue;

            if (isTransactionValid)
            {
                filteredTransactions.Add(transaction);
            }
        }
        return filteredTransactions;
    }

    // 按时间间隔聚类交易记录 Cluster Transactions By Interval
    private static List<TransactionsConvertor> ClusterTransactionsByTime(List<TransactionsConvertor> transactions, TimeSpan interval)
    {
        var clusteredTransactions = new Dictionary<DateTime, TransactionsConvertor>();

        foreach (var transaction in transactions)
        {
            var clusterTime = transaction.TimeStamp.AddTicks(-(transaction.TimeStamp.Ticks % interval.Ticks));
            if (!clusteredTransactions.TryGetValue(clusterTime, out var cluster))
            {
                cluster = new TransactionsConvertor
                {
                    TimeStamp = clusterTime,
                    Amount = 0,
                    Change = 0,
                    LocationName = string.Empty
                };
                clusteredTransactions.Add(clusterTime, cluster);
            }

            if (!transaction.LocationName.IsNullOrEmpty() && !transaction.LocationName.Equals(Service.Lang.GetText("UnknownLocation")))
            {
                if (string.IsNullOrWhiteSpace(cluster.LocationName))
                {
                    cluster.LocationName = transaction.LocationName;
                }
                else
                {
                    var locationNames = cluster.LocationName.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    if (locationNames.Length < 3)
                    {
                        cluster.LocationName += $", {transaction.LocationName}";
                    }
                }
            }

            cluster.Change += transaction.Change;

            if (cluster.TimeStamp <= transaction.TimeStamp)
            {
                cluster.Amount = transaction.Amount;
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

    // 按时间显示交易记录 Hide Unmatched Transactions By Time
    private List<TransactionsConvertor> ApplyDateTimeFilter(List<TransactionsConvertor> transactions)
    {
        var filteredTransactions = new List<TransactionsConvertor>();

        foreach (var transaction in transactions)
        {
            if (transaction.TimeStamp >= filterStartDate && transaction.TimeStamp <= filterEndDate.AddDays(1))
            {
                filteredTransactions.Add(transaction);
            }
        }
        return filteredTransactions;
    }

    // 按地点名显示交易记录 Hide Unmatched Transactions By Location
    private List<TransactionsConvertor> ApplyLocationFilter(List<TransactionsConvertor> transactions, string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return transactions;
        }

        var queries = query.Split(',')
                           .Select(q => q.Trim().Normalize(NormalizationForm.FormKC))
                           .Where(q => !string.IsNullOrEmpty(q))
                           .ToList();
        var isChineseSimplified = C.SelectedLanguage == "ChineseSimplified";
        var filteredTransactions = new List<TransactionsConvertor>();

        foreach (var transaction in transactions)
        {
            var normalizedLocation = transaction.LocationName.Normalize(NormalizationForm.FormKC);

            if (queries.Any(q => normalizedLocation.Contains(q, StringComparison.OrdinalIgnoreCase)))
            {
                filteredTransactions.Add(transaction);
            }
            else if (isChineseSimplified)
            {
                var pinyin = PinyinHelper.GetPinyin(normalizedLocation, "");
                if (queries.Any(q => pinyin.Contains(q, StringComparison.OrdinalIgnoreCase)))
                {
                    filteredTransactions.Add(transaction);
                }
            }
        }

        return filteredTransactions;
    }

    // 按备注显示交易记录 Hide Unmatched Transactions By Note
    private List<TransactionsConvertor> ApplyNoteFilter(List<TransactionsConvertor> transactions, string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return transactions;
        }

        var queries = query.Split(',')
                           .Select(q => q.Trim().Normalize(NormalizationForm.FormKC))
                           .Where(q => !string.IsNullOrEmpty(q))
                           .ToList();
        var isChineseSimplified = C.SelectedLanguage == "ChineseSimplified";
        var filteredTransactions = new List<TransactionsConvertor>();

        foreach (var transaction in transactions)
        {
            var normalizedNote = transaction.Note.Normalize(NormalizationForm.FormKC);

            if (queries.Any(q => normalizedNote.Contains(q, StringComparison.OrdinalIgnoreCase)))
            {
                filteredTransactions.Add(transaction);
            }
            else if (isChineseSimplified)
            {
                var pinyin = PinyinHelper.GetPinyin(normalizedNote, "");
                if (queries.Any(q => pinyin.Contains(q, StringComparison.OrdinalIgnoreCase)))
                {
                    filteredTransactions.Add(transaction);
                }
            }
        }

        return filteredTransactions;
    }

    // 延迟加载收支记录 Used to handle too-fast transactions loading
    private void SearchTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        UpdateTransactions();
    }

    // 调整列表框和表格高度用 Used to adjust the height of listbox and chart
    private float ChildframeHeightAdjust()
    {
        var trueCount = Convert.ToInt32(showOthers) + Convert.ToInt32(showRecordOptions);
        var ChildFrameHeight = ImGui.GetWindowHeight() - 245;

        if (trueCount == 2) ChildFrameHeight = ImGui.GetWindowHeight() - 185;
        if (trueCount == 1) ChildFrameHeight = ImGui.GetWindowHeight() - 150;
        if (trueCount == 0) ChildFrameHeight = ImGui.GetWindowHeight() - 85;

        return ChildFrameHeight;
    }

    // 调整文本长度用 Used to adjust the length of the text in header columns.
    private static string CalcNumSpaces()
    {
        var fontSize = ImGui.GetFontSize() / 2;
        var numSpaces = (int)(ImGui.GetColumnWidth() / fontSize);
        var spaces = new string('　', numSpaces);

        return spaces;
    }

    // 应用筛选器 Apply Filters
    internal List<TransactionsConvertor> ApplyFilters(List<TransactionsConvertor> currentTypeTransactions)
    {
        if (isClusteredByTime && clusterHour > 0)
        {
            var interval = TimeSpan.FromHours(clusterHour);
            currentTypeTransactions = ClusterTransactionsByTime(currentTypeTransactions, interval);
        }

        if (isChangeFilterEnabled)
            currentTypeTransactions = ApplyChangeFilter(currentTypeTransactions);

        if (isTimeFilterEnabled)
            currentTypeTransactions = ApplyDateTimeFilter(currentTypeTransactions);

        if (isLocationFilterEnabled)
            currentTypeTransactions = ApplyLocationFilter(currentTypeTransactions, searchLocationName);

        if (isNoteFilterEnabled)
            currentTypeTransactions = ApplyNoteFilter(currentTypeTransactions, searchNoteContent);

        if (C.ReverseSort)
        {
            currentTypeTransactions = currentTypeTransactions.OrderByDescending(item => item.TimeStamp).ToList();
        }

        return currentTypeTransactions;
    }

    // 用于在记录新增时更新记录 Used to update transactions when transactions added
    public void UpdateTransactionsEvent(object sender, EventArgs e)
    {
        UpdateTransactions();
    }

    // 用于在筛选时更新记录 Used to update transactions
    public void UpdateTransactions(int ifClearSelectedStates = 1)
    {
        if (currentTypeTransactions == null || selectedCurrencyID == 0)
        {
            return;
        }

        if (ifClearSelectedStates == 1)
        {
            selectedStates[selectedCurrencyID].Clear();
            selectedTransactions[selectedCurrencyID].Clear();
        }

        if (C.AllCurrencies.TryGetValue(selectedCurrencyID, out var currencyName))
        {
            Transactions.ReorderTransactions(selectedCurrencyID);
            currentTypeTransactions = ApplyFilters(Transactions.LoadAllTransactions(selectedCurrencyID));
        }
    }
}
