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
using static CurrencyTracker.Manager.Tools.Helpers;
using static CurrencyTracker.Manager.Trackers.TerrioryHandler;

namespace CurrencyTracker.Manager.Trackers;

public class Tracker
{
    public delegate void CurrencyChangedDelegate(uint currencyID, TransactionFileCategory category, ulong ID);

    public event CurrencyChangedDelegate? CurrencyChanged;

    public HandlerManager? HandlerManager;
    public ComponentManager? ComponentManager;

    private readonly Configuration? C = Service.Config;
    private readonly Plugin? P = Plugin.P;

    public Tracker()
    {
        InitCurrencies();

        HandlerManager ??= new HandlerManager();
        ComponentManager ??= new ComponentManager();

        if (Service.ClientState.IsLoggedIn) InitializeTracking();
    }

    private void InitCurrencies()
    {
        foreach (var currency in CurrencyInfo.PresetCurrencies)
            if (!C.PresetCurrencies.ContainsKey(currency))
            {
                var currencyName = CurrencyInfo.GetCurrencyLocalName(currency);
                if (!string.IsNullOrEmpty(currencyName)) C.PresetCurrencies.Add(currency, currencyName);
            }

        C.PresetCurrencies = C.PresetCurrencies.Where(kv => CurrencyInfo.PresetCurrencies.Contains(kv.Key))
                              .ToUpdateDictionary(kv => kv.Key, kv => kv.Value);
        C.Save();

        if (C.FisrtOpen)
        {
            foreach (var currencyID in CurrencyInfo.DefaultCustomCurrencies)
            {
                var currencyName = CurrencyInfo.GetCurrencyLocalName(currencyID);

                if (string.IsNullOrEmpty(currencyName)) continue;

                C.CustomCurrencies.TryAdd(currencyID, currencyName);
            }

            C.FisrtOpen = false;
            C.Save();
        }
    }

    public void InitializeTracking()
    {
        HandlerManager.Init();
        ComponentManager.Init();

        CheckAllCurrencies();
        Service.Log.Debug("Currency Tracker Activated");
    }

    public bool CheckCurrency(
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

    public bool CheckRuleAreaRestrictions(uint currencyID)
    {
        if (!ItemHandler.ItemIDs.Contains(currencyID)) return false;

        if (!C.CurrencyRules.TryGetValue(currencyID, out var rule))
        {
            C.CurrencyRules.Add(currencyID, rule = new CurrencyRule());
            C.Save();
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

    public bool CheckRuleAmountCap(
        uint currencyID, int currencyAmount, int currencyChange, TransactionFileCategory category, ulong ID)
    {
        if (!C.CurrencyRules.TryGetValue(currencyID, out _))
        {
            C.CurrencyRules.Add(currencyID, new CurrencyRule());
            C.Save();
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

        void CheckIntervals(uint currencyID, List<Interval<int>> intervals, int value, string type)
        {
            foreach (var interval in intervals)
                if (util.InRange(interval, value, true) && C.AlertNotificationChat)
                {
                    var message = Service.Lang.GetSeString("AlertIntervalMessage", type, value.ToString("N0"),
                                                           SeString.CreateItemLink(currencyID, false),
                                                           GetSelectedViewName(category, ID),
                                                           interval.ToIntervalString());
                    Service.Chat.PrintError(message);
                }
        }
    }

    public bool CheckCurrencies(
        IEnumerable<uint> currencies, string locationName = "", string noteContent = "",
        RecordChangeType recordChangeType = RecordChangeType.All, uint source = 0, TransactionFileCategory category = 0,
        ulong ID = 0)
    {
        var isChanged = false;
        foreach (var currency in C.AllCurrencyID)
            if (CheckCurrency(currency, locationName, noteContent, recordChangeType, source, category, ID))
                isChanged = true;

        var enumerable = currencies as uint[] ?? currencies.ToArray();
        if (!enumerable.Any()) return false;
        foreach (var currency in enumerable)
            if (CheckCurrency(currency, locationName, noteContent, recordChangeType, source, category, ID))
                isChanged = true;


        return isChanged;
    }

    public bool CheckAllCurrencies(
        string locationName = "", string noteContent = "", RecordChangeType recordChangeType = RecordChangeType.All,
        uint source = 0, TransactionFileCategory category = 0, ulong ID = 0)
    {
        var isChanged = false;
        foreach (var currency in C.AllCurrencyID)
            if (CheckCurrency(currency, locationName, noteContent, recordChangeType, source, category, ID))
                isChanged = true;

        return isChanged;
    }

    private void PostTransactionUpdate(
        uint currencyID, long currencyChange, uint source, TransactionFileCategory category, ulong ID)
    {
        var currencyName = CurrencyInfo.GetCurrencyName(currencyID);

        CurrencyChanged?.Invoke(currencyID, category, ID);
        Service.Log.Debug($"{currencyName}({currencyID}) Changed ({currencyChange:+#,##0;-#,##0;0}) in {category}");
        if (P.PluginInterface.IsDev) Service.Log.Debug($"Source: {source}");
    }

    internal void Uninit()
    {
        HandlerManager.Uninit();
        ComponentManager.Uninit();

        Service.Log.Debug("Currency Tracker Deactivated");
    }

    internal void Dispose()
    {
        Uninit();
    }
}
