using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tracker;
using CurrencyTracker.Manager.Transactions;
using CurrencyTracker.Trackers;
using CurrencyTracker.Windows;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;

namespace CurrencyTracker.Manager.Trackers.Components;

public class ServerBar : TrackerComponentBase
{

    internal static IDtrBarEntry             DtrEntry { get; } = DService.DtrBar.Get("CurrencyTracker");
    internal static long                     LastPeriodChanges;
    private static  CancellationTokenSource? _cancelTokenSource;

    protected override void OnInit()
    {
        DisposeCancelSource();

        DtrEntry.Text    =  DtrEntry.Tooltip = "Waiting...";
        DtrEntry.Shown   =  true;
        DtrEntry.OnClick += ToggleMainWindow;

        TrackerManager.CurrencyChanged     += OnCurrencyChanged;
        Service.Lang.LanguageChange += OnLangChanged;
        OnCurrencyChanged(Service.Config.ServerBarDisplayCurrency, TransactionFileCategory.Inventory, 0);
    }

    private static void ToggleMainWindow(DtrInteractionEvent data)
    {
        P.Main.IsOpen ^= true;

        if (P.Main.IsOpen)
            Main.LoadCurrencyTransactions(Service.Config.ServerBarDisplayCurrency);
    }

    private static void OnLangChanged(string language) => UpdateDtrEntryDelayed();

    internal static void OnCurrencyChanged(uint currencyID, TransactionFileCategory category, ulong ID)
    {
        if (currencyID != Service.Config.ServerBarDisplayCurrency) return;
        UpdateDtrEntryDelayed();
    }

    private static void UpdateDtrEntryDelayed()
    {
        DisposeCancelSource();
        _cancelTokenSource = new CancellationTokenSource();

        DService.Framework.RunOnTick(UpdateDtrEntry, TimeSpan.FromSeconds(0.5f), 0, _cancelTokenSource.Token);
    }

    private static void UpdateDtrEntry()
    {
        var thisPeriodChanges = GetChanges(ApplyDateTimeFilter);
        LastPeriodChanges = GetChanges(ApplyPreviousPeriodDateTimeFilter);

        DtrEntry.Shown   = true;
        DtrEntry.Text    = BuildDtrText(thisPeriodChanges);
        DtrEntry.Tooltip = BuildDtrTooltip();
    }

    private static SeString BuildDtrText(long thisPeriodChanges)
    {
        return new SeStringBuilder()
               .AddText(
                   $"$ {CurrencyInfo.GetName(Service.Config.ServerBarDisplayCurrency)}: {thisPeriodChanges:+ #,##0;- #,##0;0}")
               .Build();
    }

    private static string BuildDtrTooltip() =>
        $"{Service.Lang.GetText("CycleMode")}: {GetCycleModeLoc(Service.Config.ServerBarCycleMode)}\n\n" +
        $"{Service.Lang.GetText("PreviousCycle")}: {LastPeriodChanges:+ #,##0;- #,##0;0}";

    private static long GetChanges(Func<IEnumerable<Transaction>, IEnumerable<Transaction>> applyDateTimeFilter)
    {
        var categories = new[]
        {
            TransactionFileCategory.Inventory, TransactionFileCategory.SaddleBag,
            TransactionFileCategory.PremiumSaddleBag
        };
        
        var periodChanges = categories.Sum(cate =>
                                               CalculateChangesForCategory(cate, applyDateTimeFilter));

        if (Service.Config.CharacterRetainers.TryGetValue(LocalPlayerState.ContentID, out var retainers))
            periodChanges += retainers.Sum(r => CalculateChangesForRetainer(r.Key, applyDateTimeFilter));

        return periodChanges;
    }

    private static long CalculateChangesForCategory(
        TransactionFileCategory category, Func<IEnumerable<Transaction>, IEnumerable<Transaction>> applyDateTimeFilter) =>
        applyDateTimeFilter(TransactionsHandler.LoadAllTransactions(
                                Service.Config.ServerBarDisplayCurrency, category))
            .Sum(x => x.Change);

    private static long CalculateChangesForRetainer(
        ulong retainerKey, Func<IEnumerable<Transaction>, IEnumerable<Transaction>> applyDateTimeFilter) =>
        applyDateTimeFilter(TransactionsHandler.LoadAllTransactions(
                                Service.Config.ServerBarDisplayCurrency,
                                TransactionFileCategory.Retainer, retainerKey))
            .Sum(x => x.Change);

    private static IEnumerable<Transaction> ApplyDateTimeFilter(IEnumerable<Transaction> transactions)
    {
        var (startTime, endTime) = GetPeriod();
        return transactions.Where(t => t.TimeStamp >= startTime && t.TimeStamp <= endTime);
    }

    private static IEnumerable<Transaction> ApplyPreviousPeriodDateTimeFilter(IEnumerable<Transaction> transactions)
    {
        var (startTime, endTime) = GetPreviousPeriod(GetPeriod());
        return transactions.Where(t => t.TimeStamp >= startTime && t.TimeStamp <= endTime);
    }

    private static (DateTime startTime, DateTime endTime) GetPeriod()
    {
        var endTime = DateTime.Now;
        var startTime = Service.Config.ServerBarCycleMode switch
        {
            ServerBarCycleMode.Today       => DateTime.Today,
            ServerBarCycleMode.Past24Hours => endTime.AddDays(-1),
            ServerBarCycleMode.Past3Days   => DateTime.Today.AddDays(-3),
            ServerBarCycleMode.Past7Days   => DateTime.Today.AddDays(-7),
            ServerBarCycleMode.Past14Days  => DateTime.Today.AddDays(-14),
            ServerBarCycleMode.Past30Days  => DateTime.Today.AddDays(-30),
            _                              => DateTime.Today
        };

        return (startTime, endTime);
    }

    private static (DateTime startTime, DateTime endTime) GetPreviousPeriod(
        (DateTime startTime, DateTime endTime) period)
    {
        var duration = period.endTime - period.startTime;
        return (period.startTime      - duration, period.startTime);
    }

    internal static string GetCycleModeLoc(ServerBarCycleMode mode) =>
        mode switch
        {
            ServerBarCycleMode.Today       => Service.Lang.GetText("Today"),
            ServerBarCycleMode.Past24Hours => Service.Lang.GetText("Past24Hours"),
            ServerBarCycleMode.Past3Days   => Service.Lang.GetText("Past3Days"),
            ServerBarCycleMode.Past7Days   => Service.Lang.GetText("Past7Days"),
            ServerBarCycleMode.Past14Days  => Service.Lang.GetText("Past14Days"),
            ServerBarCycleMode.Past30Days  => Service.Lang.GetText("Past30Days"),
            _                              => string.Empty
        };

    private static void DisposeCancelSource()
    {
        if (_cancelTokenSource == null) return;
        
        _cancelTokenSource.Cancel();
        _cancelTokenSource.Dispose();
        _cancelTokenSource = null;
    }

    protected override void OnUninit()
    {
        TrackerManager.CurrencyChanged     -= OnCurrencyChanged;
        Service.Lang.LanguageChange -= OnLangChanged;

        DisposeCancelSource();
    }
}

public enum ServerBarCycleMode
{
    Today       = 0,
    Past24Hours = 1,
    Past3Days   = 2,
    Past7Days   = 3,
    Past14Days  = 4,
    Past30Days  = 5
}
