using System.Collections.Generic;
using CurrencyTracker.Manager;
using IntervalUtility;

namespace CurrencyTracker.Infos;

public static class CurrencyInterval
{
    public static List<Interval<int>> LoadIntervals(uint currencyID, int alertMode, TransactionFileCategoryInfo categoryInfo)
    {
        if (Service.Config.CurrencyRules.TryAdd(currencyID, new CurrencyRule()))
            Service.Config.Save();

        var rules = Service.Config.CurrencyRules[currencyID];

        rules.AlertedAmountIntervals ??= new();
        rules.AlertedChangeIntervals ??= new();

        var intervalDic = alertMode == 0
                              ? rules.AlertedAmountIntervals
                              : rules.AlertedChangeIntervals;

        var viewString = GetTransactionViewKeyString(categoryInfo.Category, categoryInfo.ID);
        if (!intervalDic.TryGetValue(viewString, out var intervalList))
        {
            intervalList = [];
            intervalDic[viewString] = intervalList;
            Service.Config.Save();
        }

        return intervalList;
    }

    public static bool AddInterval(uint currencyID, int alertMode, TransactionFileCategoryInfo categoryInfo, Interval<int> interval)
    {
        // 防止出现空引用 To Prevent Null Reference Exception
        LoadIntervals(currencyID, alertMode, categoryInfo);

        var rules = Service.Config.CurrencyRules[currencyID];
        var intervalDic = alertMode == 0
                              ? rules.AlertedAmountIntervals
                              : rules.AlertedChangeIntervals;
        var intervalList = intervalDic[GetTransactionViewKeyString(categoryInfo.Category, categoryInfo.ID)];
        if (!intervalList.Contains(interval))
        {
            intervalList.Add(interval);

            Service.Config.Save();
            return true;
        }

        return false;
    }

    public static bool RemoveInterval(uint currencyID, int alertMode, TransactionFileCategoryInfo categoryInfo, Interval<int> interval)
    {
        // 防止出现空引用 To Prevent Null Reference Exception
        LoadIntervals(currencyID, alertMode, categoryInfo);

        var rules = Service.Config.CurrencyRules[currencyID];
        var intervalDic = alertMode == 0
                              ? rules.AlertedAmountIntervals
                              : rules.AlertedChangeIntervals;
        var intervalList = intervalDic[GetTransactionViewKeyString(categoryInfo.Category, categoryInfo.ID)];
        var state = intervalList.Remove(interval);
        Service.Config.Save();

        return state;
    }

    public static Interval<int>? CreateInterval(int start, int end)
    {
        if (start > end && start != -1 && end != -1 || start == end && start != -1 && end != -1 || start < -1 || end < -1) return null;

        int? end1 = start == -1 ? null : start;
        int? end2 = end == -1 ? null : end;
        return new Interval<int>(end1, end2);
    }
}
