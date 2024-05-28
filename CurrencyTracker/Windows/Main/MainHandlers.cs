using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Infos;
using CurrencyTracker.Manager.Transactions;
using ImGuiNET;
using TinyPinyin;

namespace CurrencyTracker.Windows;

public partial class Main
{
    internal static void ReloadOrderedOptions()
    {
        var orderedOptionsSet = new HashSet<uint>(Service.Config.OrderedOptions);
        var allCurrenciesSet = new HashSet<uint>(Service.Config.AllCurrencyID);

        if (!orderedOptionsSet.SetEquals(allCurrenciesSet))
        {
            Service.Config.OrderedOptions = [.. allCurrenciesSet];
            Service.Config.Save();
        }
    }

    internal static IEnumerable<Transaction> ApplyFilters(IEnumerable<Transaction> transactions)
    {
        var filteredTransactions = transactions;

        if (TimeColumn.IsClusteredByTime && TimeColumn.ClusterHour > 0)
            filteredTransactions = ClusterTransactionsByTime(filteredTransactions, TimeSpan.FromHours(TimeColumn.ClusterHour));

        if (ChangeColumn.IsChangeFilterEnabled)
            filteredTransactions = filteredTransactions.Where(TransactionMatchesChangeFilter);

        if (TimeColumn.IsTimeFilterEnabled)
            filteredTransactions = ApplyDateTimeFilter(filteredTransactions, getTimeStampFunc, 
                                                       TimeColumn.FilterStartDate, TimeColumn.FilterEndDate.AddDays(1));

        if (LocationColumn.IsLocationFilterEnabled)
            filteredTransactions = ApplyLocationOrNoteFilter(filteredTransactions, t => t.LocationName, LocationColumn.SearchLocationName);

        if (NoteColumn.IsNoteFilterEnabled)
            filteredTransactions = ApplyLocationOrNoteFilter(filteredTransactions, t => t.Note, NoteColumn.SearchNoteContent);

        return Service.Config.ReverseSort ? filteredTransactions.OrderByDescending(getTimeStampFunc) : filteredTransactions;

        static DateTime getTimeStampFunc(Transaction t) => t.TimeStamp;
    }

    private static bool TransactionMatchesChangeFilter(Transaction transaction) =>
        (ChangeColumn.FilterMode == 0 && transaction.Change > ChangeColumn.FilterValue) || 
        (ChangeColumn.FilterMode != 0 && transaction.Change < ChangeColumn.FilterValue);

    private static IEnumerable<Transaction> ApplyDateTimeFilter(IEnumerable<Transaction> transactions, Func<Transaction, DateTime> dateTimeSelector, DateTime startDate, DateTime endDate)
    {
        return transactions.Where(transaction => dateTimeSelector(transaction) >= startDate && dateTimeSelector(transaction) <= endDate);
    }
    
    private static IEnumerable<Transaction> ApplyLocationOrNoteFilter(IEnumerable<Transaction> transactions, Func<Transaction, string> textSelector, string query)
    {
        if (string.IsNullOrEmpty(query)) 
            return transactions;

        var isChineseSimplified = Service.Config.SelectedLanguage == "ChineseSimplified";
        var queries = query.Split(',')
                           .Select(q => new
                           {
                               Normalized = q.Trim().Normalize(NormalizationForm.FormKC),
                               Pinyin = isChineseSimplified ? PinyinHelper.GetPinyin(q.Trim(), "") : null
                           })
                           .Where(q => !string.IsNullOrEmpty(q.Normalized))
                           .ToList();

        return transactions.Where(transaction =>
        {
            var normalizedText = textSelector(transaction).Normalize(NormalizationForm.FormKC);
            if (queries.Any(q => normalizedText.Contains(q.Normalized, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (isChineseSimplified)
            {
                var pinyin = PinyinHelper.GetPinyin(normalizedText, "");
                return queries.Any(q => pinyin.Contains(q.Pinyin, StringComparison.OrdinalIgnoreCase));
            }
            return false;
        });
    }

    private static IEnumerable<Transaction> ClusterTransactionsByTime(IEnumerable<Transaction> transactions, TimeSpan interval)
    {
        var groupedTransactions = transactions.GroupBy(t => t.TimeStamp.AddTicks(-(t.TimeStamp.Ticks % interval.Ticks)));

        var clusteredTransactions = groupedTransactions.Select(CreateCluster);

        return clusteredTransactions;

        Transaction CreateCluster(IGrouping<DateTime, Transaction> transactionGroup)
        {
            var firstTransaction = transactionGroup.First();
            var clusterTimestamp = firstTransaction.TimeStamp.AddTicks(-(firstTransaction.TimeStamp.Ticks % interval.Ticks));

            var totalChange = 0L;
            var maxAmount = 0L;
            var knownLocationGroups = new Dictionary<string, int>();

            foreach (var transaction in transactionGroup)
            {
                totalChange += transaction.Change;
                maxAmount = Math.Max(maxAmount, transaction.Amount);

                if (!string.IsNullOrEmpty(transaction.LocationName) && !transaction.LocationName.Equals(Service.Lang.GetText("UnknownLocation")))
                {
                    knownLocationGroups[transaction.LocationName] = knownLocationGroups.GetValueOrDefault(transaction.LocationName) + 1;
                }
            }

            var orderedUniqueKnownLocations = knownLocationGroups.OrderByDescending(pair => pair.Value).Take(3).Select(pair => pair.Key).ToList();

            var cluster = new Transaction
            {
                TimeStamp = clusterTimestamp,
                Amount = maxAmount,
                Change = totalChange,
                LocationName = string.Join(", ", orderedUniqueKnownLocations) + (orderedUniqueKnownLocations.Count == 3 ? "..." : "")
            };

            return cluster;
        }
    }

    public static void OnCurrencyChanged(uint currencyID, TransactionFileCategory category, ulong ID)
    {
        UpdateTransactions(currencyID, category, ID);
    }

    public static void UpdateTransactions(uint currencyID, TransactionFileCategory category, ulong ID)
    {
        if (SelectedCurrencyID == 0 || currencyID != SelectedCurrencyID || currentView != category || (currentView == category && currentViewID != ID)) return;

        if (!P.Main.IsOpen)
        {
            _shouldRefreshTransactions = true;
            return;
        }

        currentTypeTransactions = ApplyFilters(TransactionsHandler.LoadAllTransactions(SelectedCurrencyID, currentView, currentViewID)).ToDisplayTransaction();
        if (CharacterCurrencyInfos.Count == 0) LoadDataMCS();
        else
        {
            if (CharacterCurrencyInfos.All(x => x.Character.ContentID != Service.ClientState.LocalContentId))
            {
                var info = new CharacterCurrencyInfo(P.CurrentCharacter);
                CharacterCurrencyInfos.Add(info);
            }
        }

        ImGui.CloseCurrentPopup();
    }

    public static void RefreshTransactionsView()
    {
        TaskManager.Abort();

        TaskManager.DelayNext(250);
        TaskManager.Enqueue(() => UpdateTransactions(SelectedCurrencyID, currentView, currentViewID));
    }
}
