using System;
using System.Collections.Generic;
using System.Linq;
using CurrencyTracker.Manager.Infos;
using CurrencyTracker.Manager.Tools;
using CurrencyTracker.Manager.Trackers.Handlers;
using CurrencyTracker.Manager.Transactions;
using CurrencyTracker.Windows;
using Dalamud.Game.Text.SeStringHandling;
using IntervalUtility;

namespace CurrencyTracker.Manager.Trackers;

public class Tracker
{
    public delegate void CurrencyChangedDelegate(uint currencyID, TransactionFileCategory category, ulong ID);
    public static event CurrencyChangedDelegate? CurrencyChanged;

    internal static void Init()
    {
        if (Service.ClientState.IsLoggedIn) InitializeTracking();

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

    internal static void InitializeTracking()
    {
        HandlerManager.Init();
        ComponentManager.Init();

        CheckAllCurrencies();
        Service.Log.Debug("Currency Tracker Activated");
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
        if (currencyChange == 0 ||
            !CheckRuleAmountCap(currencyID, (int)currencyAmount, (int)currencyChange, category, ID)) return false;

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

            PostTransactionUpdate(currencyID, currencyChange, source, category, ID);
            return true;
        }

        return false;
    }

    private static bool CheckRuleAreaRestrictions(uint currencyID)
    {
        if (!ItemHandler.ItemIDs.Contains(currencyID)) return false;

        if (!Service.Config.CurrencyRules.TryGetValue(currencyID, out var rule))
        {
            Service.Config.CurrencyRules.Add(currencyID, new CurrencyRule());
            Service.Config.Save();
        }
        else
        {
            // 地点限制 Location Restrictions
            if (!rule.RegionRulesMode) // 黑名单 Blacklist
            {
                if (rule.RestrictedAreas.Contains(CurrentLocationID)) return false;
            }
            else // 白名单 Whitelist
            {
                if (!rule.RestrictedAreas.Contains(CurrentLocationID)) return false;
            }
        }

        return true;
    }

    internal static bool CheckRuleAmountCap(
        uint currencyID, int currencyAmount, int currencyChange, TransactionFileCategory category, ulong ID)
    {
        if (!Service.Config.CurrencyRules.TryGetValue(currencyID, out _))
        {
            Service.Config.CurrencyRules.Add(currencyID, new CurrencyRule());
            Service.Config.Save();
            return true;
        }

        var util = new IntervalUtil();

        // 数量 Amount
        CheckIntervals(currencyID, CurrencySettings.GetOrCreateIntervals(currencyID, 0, category, ID), currencyAmount,
                       "Amount");

        // 收支 Change
        CheckIntervals(currencyID, CurrencySettings.GetOrCreateIntervals(currencyID, 1, category, ID), currencyChange,
                       "Change");

        return true;

        void CheckIntervals(uint id, List<Interval<int>> intervals, int value, string type)
        {
            foreach (var interval in intervals)
                if (util.InRange(interval, value, true) && Service.Config.AlertNotificationChat)
                {
                    var message = Service.Lang.GetSeString("AlertIntervalMessage", type, value.ToString("N0"),
                                                           SeString.CreateItemLink(id, false),
                                                           GetSelectedViewName(category, ID),
                                                           interval.ToIntervalString());
                    Service.Chat.PrintError(message);
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
        if (!enumerable.Any()) return false;
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

    private static void PostTransactionUpdate(
        uint currencyID, long currencyChange, uint source, TransactionFileCategory category, ulong ID)
    {
        var currencyName = CurrencyInfo.GetCurrencyName(currencyID);

        CurrencyChanged?.Invoke(currencyID, category, ID);
        Service.Log.Debug($"{currencyName}({currencyID}) Changed ({currencyChange:+#,##0;-#,##0;0}) in {category}");
        if (P.PluginInterface.IsDev) Service.Log.Debug($"Source: {source}");
    }

    internal static void Uninit()
    {
        HandlerManager.Uninit();
        ComponentManager.Uninit();

        Service.Log.Debug("Currency Tracker Deactivated");
    }

    internal static void Dispose() => Uninit();
}
