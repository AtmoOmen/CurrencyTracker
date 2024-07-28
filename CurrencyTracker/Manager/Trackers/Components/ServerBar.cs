using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Transactions;
using CurrencyTracker.Windows;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;

namespace CurrencyTracker.Manager.Trackers.Components;

public class ServerBar : ITrackerComponent
{
    public bool Initialized { get; set; }
    internal static IDtrBarEntry? DtrEntry;
    internal static long LastPeriodChanges;
    private static CancellationTokenSource? CancelTokenSource;

    public void Init()
    {
        if (Service.DtrBar.Get("CurrencyTracker") is not { } entry) return;

        DtrEntry = entry;
        DtrEntry.Text = DtrEntry.Tooltip = "Waiting...";
        DtrEntry.Shown = true;
        DtrEntry.OnClick += () =>
        {
            P.Main.IsOpen ^= true;
            Main.LoadCurrencyTransactions(Service.Config.ServerBarDisplayCurrency);
        };

        Tracker.CurrencyChanged += OnCurrencyChanged;
        Service.Lang.LanguageChange += OnLangChanged;
        OnCurrencyChanged(Service.Config.ServerBarDisplayCurrency, TransactionFileCategory.Inventory, 0);
    }

    private static void OnLangChanged(string language) => UpdateDtrEntryDelayed();

    internal static void OnCurrencyChanged(uint currencyID, TransactionFileCategory category, ulong ID)
    {
        if (currencyID == Service.Config.ServerBarDisplayCurrency)
            UpdateDtrEntryDelayed();
    }

    private static async void UpdateDtrEntryDelayed()
    {
        CancelTokenSource?.Cancel();
        CancelTokenSource = new CancellationTokenSource();

        try
        {
            await Task.Delay(500, CancelTokenSource.Token);
            UpdateDtrEntry();
        }
        catch (TaskCanceledException)
        {
            // ignore
        }
    }

    private static void UpdateDtrEntry()
    {
        var thisPeriodChanges = GetChanges(ApplyDateTimeFilter);
        LastPeriodChanges = GetChanges(ApplyPreviousPeriodDateTimeFilter);

        DtrEntry.Text = new SeStringBuilder()
                        .AddText($"$ {CurrencyInfo.GetCurrencyName(Service.Config.ServerBarDisplayCurrency)}: " +
                                 $"{thisPeriodChanges:+ #,##0;- #,##0;0}")
                        .Build();
        DtrEntry.Tooltip = $"{Service.Lang.GetText("CycleMode")}: {GetCycleModeLoc(Service.Config.ServerBarCycleMode)}\n\n" +
                           $"{Service.Lang.GetText("PreviousCycle")}: {LastPeriodChanges:+ #,##0;- #,##0;0}";
    }

    private static long GetChanges(Func<IEnumerable<Transaction>, IEnumerable<Transaction>> applyDateTimeFilter)
    {
        var categories = new[] { TransactionFileCategory.Inventory, TransactionFileCategory.SaddleBag, TransactionFileCategory.PremiumSaddleBag };
        var periodChanges = categories.Sum(cate =>
            applyDateTimeFilter(TransactionsHandler.LoadAllTransactions(Service.Config.ServerBarDisplayCurrency, cate)).Sum(x => x.Change));

        if (Service.Config.CharacterRetainers.TryGetValue(Service.ClientState.LocalContentId, out var retainers))
        {
            periodChanges += retainers.Sum(retainer =>
                applyDateTimeFilter(TransactionsHandler.LoadAllTransactions(Service.Config.ServerBarDisplayCurrency, TransactionFileCategory.Retainer, retainer.Key)).Sum(x => x.Change));
        }

        return periodChanges;
    }

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
            0 => DateTime.Today,
            1 => endTime.AddDays(-1),
            2 => DateTime.Today.AddDays(-3),
            3 => DateTime.Today.AddDays(-7),
            _ => DateTime.Today
        };
        return (startTime, endTime);
    }

    private static (DateTime startTime, DateTime endTime) GetPreviousPeriod((DateTime startTime, DateTime endTime) period)
    {
        var duration = period.endTime - period.startTime;
        return (period.startTime - duration, period.endTime - duration);
    }

    internal static string GetCycleModeLoc(int mode) => mode switch
    {
        0 => Service.Lang.GetText("Today"),
        1 => Service.Lang.GetText("Past24Hours"),
        2 => Service.Lang.GetText("Past3Days"),
        3 => Service.Lang.GetText("Past7Days"),
        _ => string.Empty
    };

    public void Uninit()
    {
        Tracker.CurrencyChanged -= OnCurrencyChanged;
        Service.Lang.LanguageChange -= OnLangChanged;

        CancelTokenSource?.Cancel();
        CancelTokenSource?.Dispose();

        DtrEntry?.Remove();
        DtrEntry = null;
        Service.DtrBar.Remove("CurrencyTracker");
    }
}
