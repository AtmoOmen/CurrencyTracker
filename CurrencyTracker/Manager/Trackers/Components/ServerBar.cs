using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tasks;
using CurrencyTracker.Manager.Transactions;
using CurrencyTracker.Windows;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;

namespace CurrencyTracker.Manager.Trackers.Components;

public class ServerBar : ITrackerComponent
{
    public bool Initialized { get; set; }

    internal static DtrBarEntry? DtrEntry;

    internal static long LastPeriodChanges;
    private static TaskManager? TaskManager;

    public void Init()
    {
        if (Service.DtrBar.Get("CurrencyTracker") is not { } entry) return;

        TaskManager ??= new TaskManager { AbortOnTimeout = true, TimeLimitMS = 5000, ShowDebug = false };

        DtrEntry = entry;
        DtrEntry.Text = "Waiting...";
        DtrEntry.Tooltip = "Waiting...";
        DtrEntry.Shown = true;
        DtrEntry.OnClick += OnClick;

        Tracker.CurrencyChanged += OnCurrencyChanged;
        Service.Lang.LanguageChange += OnLangChanged;
        OnCurrencyChanged(Service.Config.ServerBarDisplayCurrency, TransactionFileCategory.Inventory, 0);
    }

    private static void OnLangChanged(string language)
    {
        TaskManager.Abort();

        TaskManager.DelayNext(500);
        TaskManager.Enqueue(UpdateDtrEntry);
    }

    private static void OnClick()
    {
        P.Main.IsOpen ^= true;
        Main.LoadCurrencyTransactions(Service.Config.ServerBarDisplayCurrency);
    }

    internal static void OnCurrencyChanged(uint currencyID, TransactionFileCategory category, ulong ID)
    {
        if (currencyID != Service.Config.ServerBarDisplayCurrency) return;

        TaskManager.Abort();

        TaskManager.DelayNext(500);
        TaskManager.Enqueue(UpdateDtrEntry);
    }

    private static void UpdateDtrEntry()
    {
        var thisPeriodChanges = GetChanges(ApplyDateTimeFilter);
        LastPeriodChanges = GetChanges(ApplyPreviousPeriodDateTimeFilter);

        DtrEntry.Text = new SeStringBuilder()
                        .AddText(
                            $"$ {CurrencyInfo.GetCurrencyName(Service.Config.ServerBarDisplayCurrency)}: {thisPeriodChanges:+ #,##0;- #,##0;0}")
                        .Build();
        DtrEntry.Tooltip = $"{Service.Lang.GetText("CycleMode")}: {GetCycleModeLoc(Service.Config.ServerBarCycleMode)}\n\n" +
                           $"{Service.Lang.GetText("PreviousCycle")}: {LastPeriodChanges:+ #,##0;- #,##0;0}";
    }

    private static long GetChanges(
        Func<IEnumerable<Transaction>, IEnumerable<Transaction>> applyDateTimeFilter)
    {
        var periodChanges = 0L;
        var categories = new[]
        {
            TransactionFileCategory.Inventory, TransactionFileCategory.SaddleBag,
            TransactionFileCategory.PremiumSaddleBag
        };

        foreach (var cate in categories)
            periodChanges +=
                applyDateTimeFilter(
                        TransactionsHandler.LoadAllTransactions(Service.Config.ServerBarDisplayCurrency, cate))
                    .Sum(x => x.Change);

        if (Service.Config.CharacterRetainers.TryGetValue(Service.ClientState.LocalContentId, out var value))
        {
            foreach (var retainer in value)
                periodChanges +=
                    applyDateTimeFilter(TransactionsHandler.LoadAllTransactions(
                                            Service.Config.ServerBarDisplayCurrency, TransactionFileCategory.Retainer,
                                            retainer.Key)).Sum(x => x.Change);
        }

        return periodChanges;
    }

    private static IEnumerable<Transaction> ApplyDateTimeFilter(
        IEnumerable<Transaction> transactions)
    {
        var period = GetPeriod();
        return transactions.Where(transaction =>
                                      transaction.TimeStamp >= period.startTime &&
                                      transaction.TimeStamp <= period.endTime);
    }

    private static IEnumerable<Transaction> ApplyPreviousPeriodDateTimeFilter(
        IEnumerable<Transaction> transactions)
    {
        var period = GetPreviousPeriod(GetPeriod());
        return transactions.Where(transaction =>
                                      transaction.TimeStamp >= period.startTime &&
                                      transaction.TimeStamp <= period.endTime);
    }

    private static (DateTime startTime, DateTime endTime) GetPeriod()
    {
        var startTime = DateTime.Today;
        var endTime = DateTime.Now;

        // 0 - Today; 1 - Past 24 Hours; 2 - Past 3 Days; 3 - Past 7 Days;
        startTime = Service.Config.ServerBarCycleMode switch
        {
            0 => DateTime.Today,
            1 => DateTime.Now.AddDays(-1),
            2 => DateTime.Today.AddDays(-3),
            3 => DateTime.Today.AddDays(-7),
            _ => startTime
        };

        return (startTime, endTime);
    }

    private static (DateTime startTime, DateTime endTime) GetPreviousPeriod(
        (DateTime startTime, DateTime endTime) period)
    {
        var duration = period.endTime - period.startTime;
        var previousStartTime = period.startTime - duration;
        var previousEndTime = period.endTime - duration;

        return (previousStartTime, previousEndTime);
    }

    internal static string GetCycleModeLoc(int mode)
    {
        var loc = mode switch
        {
            0 => Service.Lang.GetText("Today"),
            1 => Service.Lang.GetText("Past24Hours"),
            2 => Service.Lang.GetText("Past3Days"),
            3 => Service.Lang.GetText("Past7Days"),
            _ => string.Empty
        };
        return loc;
    }

    public void Uninit()
    {
        Tracker.CurrencyChanged -= OnCurrencyChanged;
        Service.Lang.LanguageChange -= OnLangChanged;

        DtrEntry?.Dispose();
        DtrEntry = null;
        Service.DtrBar.Remove("CurrencyTracker");

        TaskManager?.Abort();
    }
}
