using System;
using System.Collections.Generic;
using System.Linq;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tools;
using CurrencyTracker.Manager.Trackers.Handlers;
using CurrencyTracker.Manager.Transactions;
using Dalamud.Game.Text.SeStringHandling;
using IntervalUtility;

namespace CurrencyTracker.Manager.Trackers;

public class Tracker
{
    public delegate void CurrencyChangedDelegate(uint currencyID, TransactionFileCategory category, ulong ID);
    public static event CurrencyChangedDelegate? CurrencyChanged;

    internal static void Init()
    { 
        InitTracking();

        foreach (var currency in CurrencyInfo.PresetCurrencies)
            if (!Service.Config.PresetCurrencies.ContainsKey(currency))
            {
                var currencyName = CurrencyInfo.GetCurrencyLocalName(currency);
                if (!string.IsNullOrEmpty(currencyName)) Service.Config.PresetCurrencies.Add(currency, currencyName);
            }

        Service.Config.PresetCurrencies = Service.Config.PresetCurrencies.Where(kv => CurrencyInfo.PresetCurrencies.Contains(kv.Key))
                              .ToUpdateDictionary(kv => kv.Key, kv => kv.Value);
        Service.Config.Save();

        if (Service.Config.FirstOpen)
        {
            foreach (var currencyID in CurrencyInfo.DefaultCustomCurrencies)
            {
                var currencyName = CurrencyInfo.GetCurrencyLocalName(currencyID);

                if (string.IsNullOrEmpty(currencyName)) continue;

                Service.Config.CustomCurrencies.TryAdd(currencyID, currencyName);
            }

            Service.Config.FirstOpen = false;
            Service.Config.Save();
        }
    }

    internal static void InitTracking()
    {
        HandlerManager.Init();
        ComponentManager.Init();

        CheckAllCurrencies();
        DService.Log.Debug("Currency Tracker Activated");
    }

    internal static bool CheckCurrency(
        uint currencyID, string locationName = "", string noteContent = "", RecordChangeType recordChangeType = 0,
        uint source = 0, TransactionFileCategory category = 0, ulong ID = 0)
    {
        if (!CheckRuleAreaRestrictions(currencyID)) return false;

        var currencyAmount = CurrencyInfo.GetCurrencyAmount(currencyID, category, ID);
        var previousAmount = CurrencyInfo.GetCurrencyAmountFromFile(currencyID, P.CurrentCharacter, category, ID);

        if (previousAmount == null && currencyAmount <= 0) return false;

        var currencyChange = currencyAmount - (previousAmount ?? 0);
        if (currencyChange == 0) return false;

        locationName = string.IsNullOrEmpty(locationName) ? CurrentLocationName : locationName;

        if (recordChangeType == RecordChangeType.All ||
            (recordChangeType == RecordChangeType.Positive && currencyChange > 0) ||
            (recordChangeType == RecordChangeType.Negative && currencyChange < 0))
        {
            if (previousAmount != null)
            {
                TransactionsHandler.AppendTransaction(currencyID, DateTime.Now, currencyAmount, currencyChange,
                                                      locationName,
                                                      noteContent, category, ID);
            }
            else
            {
                TransactionsHandler.AddTransaction(currencyID, DateTime.Now, currencyAmount, currencyAmount,
                                                   locationName,
                                                   noteContent, category, ID);
            }

            var currencyName = CurrencyInfo.GetCurrencyName(currencyID);
            CurrencyChanged?.Invoke(currencyID, category, ID);

            PostCurrencyChangeCheck(currencyName, currencyID, currencyAmount, currencyChange, category, ID, source);
            return true;
        }

        return false;
    }

    private static void PostCurrencyChangeCheck(string currencyName, uint currencyID, long currencyAmount, long currencyChange,
                                                TransactionFileCategory category, ulong ID, uint source)
    {
        DService.Log.Debug($"{currencyName}({currencyID}) Changed ({currencyChange:+#,##0;-#,##0;0}) in {category}");
        if (P.PI.IsDev) DService.Log.Debug($"Source: {source}");
        CheckRuleAmountCap(currencyID, (int)currencyAmount, (int)currencyChange, category, ID);
    }

    private static bool CheckRuleAreaRestrictions(uint currencyID)
    {
        if (!ItemHandler.ItemIDs.Contains(currencyID)) return false;

        if (!Service.Config.CurrencyRules.TryGetValue(currencyID, out var rule))
            return true;

        if (rule.RestrictedAreas == null) return true;

        // 地点限制 Location Restrictions
        if (!rule.RegionRulesMode) // 黑名单 Blacklist
        {
            if (rule.RestrictedAreas.Contains(CurrentLocationID)) return false;
        }
        else // 白名单 Whitelist
        {
            if (!rule.RestrictedAreas.Contains(CurrentLocationID)) return false;
        }

        return true;
    }

    internal static bool CheckRuleAmountCap(
        uint currencyID, int currencyAmount, int currencyChange, TransactionFileCategory category, ulong ID)
    {
        if (!Service.Config.CurrencyRules.TryGetValue(currencyID, out var rule))
            return true;

        if (rule.AlertedAmountIntervals == null) return true;
        if (rule.AlertedChangeIntervals == null) return true;

        var util = new IntervalUtil();

        // 数量 Amount
        CheckIntervals(currencyID, CurrencyInterval.LoadIntervals(currencyID, 0, new TransactionFileCategoryInfo(category, ID)), currencyAmount,
                       "Amount");

        // 收支 Change
        CheckIntervals(currencyID, CurrencyInterval.LoadIntervals(currencyID, 1, new TransactionFileCategoryInfo(category, ID)), currencyChange,
                       "Change");

        return true;

        void CheckIntervals(uint id, List<Interval<int>> intervals, int value, string type)
        {
            foreach (var interval in intervals)
                if (util.InRange(interval, value, true) && Service.Config.AlertNotificationChat)
                {
                    var message = Service.Lang.GetSeString("AlertIntervalMessage", Service.Lang.GetText(type), value.ToString("N0"),
                                                           SeString.CreateItemLink(id, false),
                                                           GetSelectedViewName(category, ID),
                                                           interval.ToIntervalString());
                    DService.Chat.PrintError(message);
                }
        }
    }

    internal static bool CheckCurrencies(
        IEnumerable<uint> currencies, string locationName = "", string noteContent = "",
        RecordChangeType recordChangeType = RecordChangeType.All, uint source = 0, TransactionFileCategory category = 0,
        ulong ID = 0)
    {
        var isChanged = false;
        foreach (var currency in Service.Config.AllCurrencyID)
            if (CheckCurrency(currency, locationName, noteContent, recordChangeType, source, category, ID))
                isChanged = true;

        var enumerable = currencies as uint[] ?? currencies.ToArray();
        if (enumerable.Length == 0) return false;
        foreach (var currency in enumerable)
            if (CheckCurrency(currency, locationName, noteContent, recordChangeType, source, category, ID))
                isChanged = true;


        return isChanged;
    }

    internal static bool CheckAllCurrencies(
        string locationName = "", string noteContent = "", RecordChangeType recordChangeType = RecordChangeType.All,
        uint source = 0, TransactionFileCategory category = 0, ulong ID = 0)
    {
        var isChanged = false;
        foreach (var currency in Service.Config.AllCurrencyID)
            if (CheckCurrency(currency, locationName, noteContent, recordChangeType, source, category, ID))
                isChanged = true;

        return isChanged;
    }

    internal static void TriggerCurrencyChangedEvent(uint currencyID, TransactionFileCategory category, ulong ID)
        => CurrencyChanged?.Invoke(currencyID, category, ID);

    internal static void Uninit()
    {
        HandlerManager.Uninit();
        ComponentManager.Uninit();

        DService.Log.Debug("Currency Tracker Deactivated");
    }

    internal static void Dispose() => Uninit();
}
