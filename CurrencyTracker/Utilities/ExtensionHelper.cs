using System;
using System.Collections.Generic;
using System.Linq;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Transactions;
using CurrencyTracker.Windows;
using IntervalUtility;

namespace CurrencyTracker.Utilities;

public static class ExtensionHelper
{
    public static List<DisplayTransaction> ToDisplayTransaction(this IEnumerable<Transaction> transactions)
    {
        return transactions.Select(transaction => new DisplayTransaction
        {
            Transaction = transaction,
            Selected    = false
        }).ToList();
    }

    public static UpdateDictionary<TKey, TValue> ToUpdateDictionary<TKey, TValue>(
        this IEnumerable<KeyValuePair<TKey, TValue>> pairs,
        Func<KeyValuePair<TKey, TValue>, TKey>       keySelector,
        Func<KeyValuePair<TKey, TValue>, TValue>     valueSelector)
        where TKey : notnull
    {
        var updateDictionary = new UpdateDictionary<TKey, TValue>();
        foreach (var pair in pairs) updateDictionary.Add(keySelector(pair), valueSelector(pair));
        return updateDictionary;
    }

    public static string ToIntervalString<T>(this Interval<T> interval) where T : struct, IComparable => 
        $"{(interval.Start == null ? "(-∞" : $"[{interval.Start}")},{(interval.End == null ? "+∞)" : $"{interval.End}]")}";
}
