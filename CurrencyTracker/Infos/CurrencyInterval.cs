using System.Collections.Generic;
using CurrencyTracker.Internal;
using CurrencyTracker.Manager;
using IntervalUtility;

namespace CurrencyTracker.Infos;

public static class CurrencyInterval
{
    public static List<Interval<int>> LoadIntervals(uint currencyID, int alertMode, TransactionFileCategoryInfo categoryInfo)
    {
        if (PluginConfig.Instance().CurrencyRules.TryAdd(currencyID, new CurrencyRule()))
            PluginConfig.Instance().Save();

        var rules = PluginConfig.Instance().CurrencyRules[currencyID];

        rules.AlertedAmountIntervals ??= new();
        rules.AlertedChangeIntervals ??= new();

        var intervalDic = alertMode == 0
                              ? rules.AlertedAmountIntervals
                              : rules.AlertedChangeIntervals;

        var viewString = categoryInfo.Category.GetTransactionViewKeyString(categoryInfo.ID);

        if (!intervalDic.TryGetValue(viewString, out var intervalList))
        {
            intervalList            = [];
            intervalDic[viewString] = intervalList;
            PluginConfig.Instance().Save();
        }

        return intervalList;
    }

    public static bool AddInterval(uint currencyID, int alertMode, TransactionFileCategoryInfo categoryInfo, Interval<int> interval)
    {
        // 防止出现空引用 To Prevent Null Reference Exception
        LoadIntervals(currencyID, alertMode, categoryInfo);

        var rules = PluginConfig.Instance().CurrencyRules[currencyID];
        var intervalDic = alertMode == 0
                              ? rules.AlertedAmountIntervals
                              : rules.AlertedChangeIntervals;
        var intervalList = intervalDic[categoryInfo.Category.GetTransactionViewKeyString(categoryInfo.ID)];

        if (!intervalList.Contains(interval))
        {
            intervalList.Add(interval);

            PluginConfig.Instance().Save();
            return true;
        }

        return false;
    }

    public static bool RemoveInterval(uint currencyID, int alertMode, TransactionFileCategoryInfo categoryInfo, Interval<int> interval)
    {
        // 防止出现空引用 To Prevent Null Reference Exception
        LoadIntervals(currencyID, alertMode, categoryInfo);

        var rules = PluginConfig.Instance().CurrencyRules[currencyID];
        var intervalDic = alertMode == 0
                              ? rules.AlertedAmountIntervals
                              : rules.AlertedChangeIntervals;
        var intervalList = intervalDic[categoryInfo.Category.GetTransactionViewKeyString(categoryInfo.ID)];
        var state        = intervalList.Remove(interval);
        PluginConfig.Instance().Save();

        return state;
    }

    public static Interval<int>? CreateInterval(int start, int end)
    {
        if (start > end  && start != -1 && end != -1 ||
            start == end && start != -1 && end != -1 ||
            start < -1                               ||
            end   < -1)
            return null;

        int? end1 = start == -1 ? null : start;
        int? end2 = end   == -1 ? null : end;
        return new Interval<int>(end1, end2);
    }
}
