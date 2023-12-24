namespace CurrencyTracker.Windows;

public partial class Main
{
    // 用于处理选项增减 Used to handle options changes.
    private void ReloadOrderedOptions()
    {
        var orderedOptionsSet = new HashSet<uint>(C.OrderedOptions);
        var allCurrenciesSet = new HashSet<uint>(C.AllCurrencyID);

        if (!orderedOptionsSet.SetEquals(allCurrenciesSet))
        {
            C.OrderedOptions = allCurrenciesSet.ToList();
            C.Save();
        }
    }

    // 用于处理选项位置变化 Used to handle option's position change.
    private void SwapOptions(int index1, int index2)
    {
        if (index1 < 0 || index1 >= C.OrderedOptions.Count || index2 < 0 || index2 >= C.OrderedOptions.Count) return;

        (C.OrderedOptions[index2], C.OrderedOptions[index1]) = (C.OrderedOptions[index1], C.OrderedOptions[index2]);
        C.Save();
    }

    // 按收支隐藏不符合要求的交易记录 Hide Unmatched Transactions By Change
    private IEnumerable<TransactionsConvertor> ApplyChangeFilter(IEnumerable<TransactionsConvertor> transactions)
    {
        return transactions.Where(transaction => filterMode == 0 ? transaction.Change > filterValue : transaction.Change < filterValue);
    }

    // 按时间间隔聚类交易记录 Cluster Transactions By Interval
    private static IEnumerable<TransactionsConvertor> ClusterTransactionsByTime(IEnumerable<TransactionsConvertor> transactions, TimeSpan interval)
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
                if (cluster.LocationName.IsNullOrEmpty())
                {
                    cluster.LocationName = transaction.LocationName;
                }
                else
                {
                    var locationNames = cluster.LocationName.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    if (locationNames.Length < 3 && !cluster.LocationName.Contains(transaction.LocationName))
                    {
                        cluster.LocationName = new StringBuilder(cluster.LocationName).Append(", ").Append(transaction.LocationName).ToString();
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

        return clusteredTransactions.Values;
    }

    // 按时间显示交易记录 Hide Unmatched Transactions By Time
    private IEnumerable<TransactionsConvertor> ApplyDateTimeFilter(IEnumerable<TransactionsConvertor> transactions)
    {
        var nextDay = filterEndDate.AddDays(1);
        return transactions.Where(transaction => transaction.TimeStamp >= filterStartDate && transaction.TimeStamp <= nextDay);
    }

    // 按地点名显示交易记录 Hide Unmatched Transactions By Location
    private IEnumerable<TransactionsConvertor> ApplyLocationFilter(IEnumerable<TransactionsConvertor> transactions, string query)
    {
        if (query.IsNullOrEmpty())
        {
            return transactions;
        }

        var isChineseSimplified = C.SelectedLanguage == "ChineseSimplified";
        var queries = query.Split(',')
                           .Select(q => new
                           {
                               Normalized = q.Trim().Normalize(NormalizationForm.FormKC),
                               Pinyin = isChineseSimplified ? PinyinHelper.GetPinyin(q.Trim(), "") : null
                           })
                           .Where(q => !q.Normalized.IsNullOrEmpty())
                           .ToList();

        return transactions.Where(transaction =>
        {
            var normalizedLocation = transaction.LocationName.Normalize(NormalizationForm.FormKC);
            if (queries.Any(q => normalizedLocation.Contains(q.Normalized, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
            else if (isChineseSimplified)
            {
                var pinyin = PinyinHelper.GetPinyin(normalizedLocation, "");
                return queries.Any(q => pinyin.Contains(q.Pinyin, StringComparison.OrdinalIgnoreCase));
            }
            return false;
        });
    }

    // 按备注显示交易记录 Hide Unmatched Transactions By Note
    private IEnumerable<TransactionsConvertor> ApplyNoteFilter(IEnumerable<TransactionsConvertor> transactions, string query)
    {
        if (query.IsNullOrEmpty())
        {
            return transactions;
        }

        var isChineseSimplified = C.SelectedLanguage == "ChineseSimplified";
        var queries = query.Split(',')
                           .Select(q => new
                           {
                               Normalized = q.Trim().Normalize(NormalizationForm.FormKC),
                               Pinyin = isChineseSimplified ? PinyinHelper.GetPinyin(q.Trim(), "") : null
                           })
                           .Where(q => !q.Normalized.IsNullOrEmpty())
                           .ToList();

        return transactions.Where(transaction =>
        {
            var normalizedNote = transaction.Note.Normalize(NormalizationForm.FormKC);
            if (queries.Any(q => normalizedNote.Contains(q.Normalized, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
            else if (isChineseSimplified)
            {
                var pinyin = PinyinHelper.GetPinyin(normalizedNote, "");
                return queries.Any(q => pinyin.Contains(q.Pinyin, StringComparison.OrdinalIgnoreCase));
            }
            return false;
        });
    }

    // 应用筛选器 Apply Filters
    internal List<TransactionsConvertor> ApplyFilters(List<TransactionsConvertor> currentTypeTransactions)
    {
        IEnumerable<TransactionsConvertor> filteredTransactions = currentTypeTransactions;

        if (isClusteredByTime && clusterHour > 0)
            filteredTransactions = ClusterTransactionsByTime(filteredTransactions, TimeSpan.FromHours(clusterHour));

        if (isChangeFilterEnabled)
            filteredTransactions = ApplyChangeFilter(filteredTransactions);

        if (isTimeFilterEnabled)
            filteredTransactions = ApplyDateTimeFilter(filteredTransactions);

        if (isLocationFilterEnabled)
            filteredTransactions = ApplyLocationFilter(filteredTransactions, searchLocationName);

        if (isNoteFilterEnabled)
            filteredTransactions = ApplyNoteFilter(filteredTransactions, searchNoteContent);

        return (C.ReverseSort ? filteredTransactions.OrderByDescending(item => item.TimeStamp) : filteredTransactions).ToList();
    }

    // 用于在记录新增时更新记录 Used to update transactions when transactions added
    public void OnCurrencyChanged(uint currencyID, TransactionFileCategory category, ulong ID)
    {
        UpdateTransactions(currencyID, category, ID);
    }

    // 用于在更新主界面记录 Used to update transactions
    public void UpdateTransactions(uint currencyID, TransactionFileCategory category, ulong ID)
    {
        if (!IsOpen || selectedCurrencyID == 0 || currencyID != selectedCurrencyID || currentView != category || (currentView == category && currentViewID != ID)) return;

        selectedStates.Clear();
        selectedTransactions.Clear();

        currentTypeTransactions = ApplyFilters(Transactions.LoadAllTransactions(selectedCurrencyID, currentView, currentViewID));

        ImGui.CloseCurrentPopup();
    }

    // 延迟加载收支记录 Used to handle too-fast transactions loading
    private void SearchTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        UpdateTransactions(selectedCurrencyID, currentView, currentViewID);
    }

    // 调整列表框和表格高度用 Used to adjust the height of listbox and chart
    private float ChildframeHeightAdjust()
    {
        var trueCount = Convert.ToInt32(showOthers) + Convert.ToInt32(showRecordOptions);
        var windowHeight = ImGui.GetWindowHeight();

        return windowHeight - (trueCount == 2 ? 185 : trueCount == 1 ? 150 : 85);
    }
}
