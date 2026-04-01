using System;
using System.Linq;
using System.Collections.Generic;
using CurrencyTracker.Manager.Transactions;
using CurrencyTracker.Windows;
using IntervalUtility;

namespace CurrencyTracker.Utilities;

public static class ExtensionHelper
{
    public static List<DisplayTransaction> ToDisplayTransaction(this IEnumerable<Transaction> transactions)
    {
        return transactions.Select
        (transaction => new DisplayTransaction
            {
                Transaction = transaction,
                Selected    = false
            }
        ).ToList();
    }

    public static string ToIntervalString<T>(this Interval<T> interval) where T : struct, IComparable =>
        $"{(interval.Start == null ? "(-∞" : $"[{interval.Start}")},{(interval.End == null ? "+∞)" : $"{interval.End}]")}";
}
