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
            Service.Config.OrderedOptions = allCurrenciesSet.ToList();
            Service.Config.Save();
        }
    }

    internal static List<Transaction> ApplyFilters(List<Transaction> transactions)
    {
        IEnumerable<Transaction> filteredTransactions = transactions;

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

        return (Service.Config.ReverseSort ? filteredTransactions.OrderByDescending(item => item.TimeStamp) : filteredTransactions).ToList();
    }

    private static IEnumerable<Transaction> ApplyChangeFilter(IEnumerable<Transaction> transactions)
    {
        return transactions.Where(transaction => filterMode == 0 ? transaction.Change > filterValue : transaction.Change < filterValue);
    }

    private static IEnumerable<Transaction> ClusterTransactionsByTime(IEnumerable<Transaction> transactions, TimeSpan interval)
    {
        var clusteredTransactions = new Dictionary<DateTime, Transaction>();

        foreach (var transaction in transactions)
        {
            var clusterTime = transaction.TimeStamp.AddTicks(-(transaction.TimeStamp.Ticks % interval.Ticks));
            if (!clusteredTransactions.TryGetValue(clusterTime, out var cluster))
            {
                cluster = new Transaction
                {
                    TimeStamp = clusterTime,
                    Amount = 0,
                    Change = 0,
                    LocationName = string.Empty
                };
                clusteredTransactions.Add(clusterTime, cluster);
            }

            if (!string.IsNullOrEmpty(transaction.LocationName) && !transaction.LocationName.Equals(Service.Lang.GetText("UnknownLocation")))
            {
                if (string.IsNullOrEmpty(cluster.LocationName))
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

    private static IEnumerable<Transaction> ApplyDateTimeFilter(IEnumerable<Transaction> transactions)
    {
        var nextDay = filterEndDate.AddDays(1);
        return transactions.Where(transaction => transaction.TimeStamp >= filterStartDate && transaction.TimeStamp <= nextDay);
    }

    private static IEnumerable<Transaction> ApplyLocationFilter(IEnumerable<Transaction> transactions, string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return transactions;
        }

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
            var normalizedLocation = transaction.LocationName.Normalize(NormalizationForm.FormKC);
            if (queries.Any(q => normalizedLocation.Contains(q.Normalized, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (isChineseSimplified)
            {
                var pinyin = PinyinHelper.GetPinyin(normalizedLocation, "");
                return queries.Any(q => pinyin.Contains(q.Pinyin, StringComparison.OrdinalIgnoreCase));
            }
            return false;
        });
    }

    private static IEnumerable<Transaction> ApplyNoteFilter(IEnumerable<Transaction> transactions, string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return transactions;
        }

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

        selectedStates.Clear();
        selectedTransactions.Clear();

        currentTypeTransactions = ApplyFilters(TransactionsHandler.LoadAllTransactions(SelectedCurrencyID, currentView, currentViewID));
        if (CharacterCurrencyInfos.Count == 0) LoadDataMCS();
        else
        {
            if (CharacterCurrencyInfos.All(x => x.Character.ContentID != Service.ClientState.LocalContentId))
            {
                var info = new CharacterCurrencyInfo { Character = P.CurrentCharacter };
                CharacterCurrencyInfos.Add(info);
            }
        }

        ImGui.CloseCurrentPopup();
    }

    private static void RefreshTransactionsView()
    {
        TaskManager.Abort();

        TaskManager.DelayNext(250);
        TaskManager.Enqueue(() => UpdateTransactions(SelectedCurrencyID, currentView, currentViewID));
    }
}
