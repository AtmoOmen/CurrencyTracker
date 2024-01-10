namespace CurrencyTracker.Windows;

public partial class Main
{
    internal void ReloadOrderedOptions()
    {
        var orderedOptionsSet = new HashSet<uint>(C.OrderedOptions);
        var allCurrenciesSet = new HashSet<uint>(C.AllCurrencyID);

        if (!orderedOptionsSet.SetEquals(allCurrenciesSet))
        {
            C.OrderedOptions = allCurrenciesSet.ToList();
            C.Save();
        }
    }

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

    private IEnumerable<TransactionsConvertor> ApplyChangeFilter(IEnumerable<TransactionsConvertor> transactions)
    {
        return transactions.Where(transaction => filterMode == 0 ? transaction.Change > filterValue : transaction.Change < filterValue);
    }

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

    private IEnumerable<TransactionsConvertor> ApplyDateTimeFilter(IEnumerable<TransactionsConvertor> transactions)
    {
        var nextDay = filterEndDate.AddDays(1);
        return transactions.Where(transaction => transaction.TimeStamp >= filterStartDate && transaction.TimeStamp <= nextDay);
    }

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

    public void OnCurrencyChanged(uint currencyID, TransactionFileCategory category, ulong ID)
    {
        UpdateTransactions(currencyID, category, ID);
    }

    public void UpdateTransactions(uint currencyID, TransactionFileCategory category, ulong ID)
    {
        if (!IsOpen || selectedCurrencyID == 0 || currencyID != selectedCurrencyID || currentView != category || (currentView == category && currentViewID != ID)) return;

        selectedStates.Clear();
        selectedTransactions.Clear();

        currentTypeTransactions = ApplyFilters(TransactionsHandler.LoadAllTransactions(selectedCurrencyID, currentView, currentViewID));
        if (!characterCurrencyInfos.Any()) LoadDataMCS();
        else
        {
            var existingInfo = characterCurrencyInfos.GetOrAdd(P.CurrentCharacter, new CharacterCurrencyInfo { Character = P.CurrentCharacter });
            existingInfo.GetCharacterCurrencyAmount();
        }

        ImGui.CloseCurrentPopup();
    }

    private void SearchTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        UpdateTransactions(selectedCurrencyID, currentView, currentViewID);
    }
}
