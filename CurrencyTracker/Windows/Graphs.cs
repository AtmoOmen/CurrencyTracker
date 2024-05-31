using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CurrencyTracker.Manager;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ImPlotNET;
using static CurrencyTracker.Windows.Main;

namespace CurrencyTracker.Windows;

public class Graph : Window, IDisposable
{
    public Graph(Plugin plugin) : base($"Graphs##{Name}") => Flags |= ImGuiWindowFlags.NoScrollbar;

    private enum GroupByInterval
    {
        Day,
        Week,
        Month,
        Year
    }

    private static DisplayTransactionGroup[]? changeGraphData;
    private static DisplayTransactionGroup[]? amountGraphData;
    private static DisplayTransactionGroup[]? locationGraphData;
    private static DisplayTransactionGroup[]? locationAmountGraphData;

    public class DisplayTransactionGroup
    {
        public string XAxis { get; set; } = string.Empty;
        public float YAxis { get; set; }
    }

    private static readonly Dictionary<uint, string> ViewLoc = new()
    {
        { 0, Service.Lang.GetText("AmountGraph") },
        { 1, Service.Lang.GetText("ChangeGraph") },
        { 2, Service.Lang.GetText("LocationGraph") },
        { 3, Service.Lang.GetText("LocationAmountGraph") }
    };

    private static uint _currentPlot;
    private static GroupByInterval _groupInterval;

    public override void Draw()
    {
        if (SelectedCurrencyID == 0)
        {
            P.Graph.IsOpen = false;
            return;
        }

        var currencyName = Service.Config.AllCurrencies[SelectedCurrencyID];
        var currencyIcon = Service.Config.AllCurrencyIcons[SelectedCurrencyID].ImGuiHandle;

        ImGui.SetWindowFontScale(1.3f);
        ImGui.BeginGroup();
        var currentCursorPos = ImGui.GetCursorPos();
        ImGui.SetCursorPosY(currentCursorPos.Y + 2f);
        ImGui.Image(currencyIcon, ImGuiHelpers.ScaledVector2(24f));

        ImGui.SameLine();
        ImGui.SetCursorPosY(currentCursorPos.Y + 2f);
        ImGui.TextColored(ImGuiColors.DalamudOrange, currencyName);
        ImGui.EndGroup();
        ImGui.SetWindowFontScale(1f);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
        ImGui.SetCursorPosY(currentCursorPos.Y + 2f);
        if (ImGui.BeginCombo("###GraphsViewSelectCombo", ViewLoc[_currentPlot], ImGuiComboFlags.HeightLarge))
        {
            foreach (var view in ViewLoc)
                if (ImGui.Selectable(view.Value, view.Key == _currentPlot))
                    _currentPlot = view.Key;
            ImGui.EndCombo();
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(100f * ImGuiHelpers.GlobalScale);
        ImGui.SetCursorPosY(currentCursorPos.Y + 2f);
        if (ImGui.BeginCombo("###GraphsIntervalSelectCombo", _groupInterval.ToString(), ImGuiComboFlags.HeightLarge))
        {
            foreach (GroupByInterval view in Enum.GetValues(typeof(GroupByInterval)))
                if (ImGui.Selectable(view.ToString(), view == _groupInterval))
                    _groupInterval = view;
            ImGui.EndCombo();
        }

        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.SetCursorPosY(currentCursorPos.Y + 2f);
        if (ImGui.Button(FontAwesomeIcon.SyncAlt.ToIconString()))
        {
            amountGraphData = null;
            changeGraphData = null;
            locationGraphData = null;
            locationAmountGraphData = null;
        }

        ImGui.PopFont();


        if (currentTransactions.Count <= 0) return;
        switch (_currentPlot)
        {
            case 0:
                AmountGraph(currentTransactions);
                break;
            case 1:
                ChangeGraph(currentTransactions);
                break;
            case 2:
                LocationGraph(currentTransactions);
                break;
            case 3:
                LocationAmountGraph(currentTransactions);
                break;
        }
    }

    private static void AmountGraph(IReadOnlyCollection<DisplayTransaction> currentTypeTransactions)
    {
        if (currentTypeTransactions.Count == 0) return;

        var (dividedFactor, dividedName) =
            CalculateDividedFactor((int)Math.Abs(currentTypeTransactions.Average(t => t.Transaction.Amount)));

        amountGraphData ??= GroupTransactions(currentTypeTransactions, _groupInterval);

        if (ImPlot.BeginPlot(Service.Lang.GetText("AmountGraph"), ImGui.GetContentRegionAvail()))
        {
            ImPlot.SetupAxesLimits(-2, amountGraphData.Length + 2, -2, amountGraphData.Max(x => x.YAxis) + 5);

            ImPlot.SetupAxis(ImAxis.X1, Service.Lang.GetText("Time"));
            ImPlot.SetupAxis(ImAxis.Y1, $"{Service.Lang.GetText("Amount")} {dividedName}", ImPlotAxisFlags.AutoFit);

            var amountValues = amountGraphData.Select(x => (float)Math.Round(x.YAxis / dividedFactor, 6)).ToArray();
            var dateTimeValues = amountGraphData.Select(x => x.XAxis).ToArray();

            ImPlot.SetupAxisTicks(ImAxis.X1, 0, dateTimeValues.Length - 1, dateTimeValues.Length, dateTimeValues);
            ImPlot.PlotBars("", ref amountValues[0], dateTimeValues.Length);
            ImPlot.EndPlot();
        }

        return;

        DisplayTransactionGroup[] GroupTransactions(
            IEnumerable<DisplayTransaction> currentTypeTransactions, GroupByInterval interval)
        {
            var result = new List<DisplayTransactionGroup>();

            switch (interval)
            {
                case GroupByInterval.Day:
                    result = currentTypeTransactions
                             .GroupBy(t => t.Transaction.TimeStamp.Date)
                             .Select(g => new DisplayTransactionGroup
                             {
                                 XAxis = g.Key.ToString("yy/MM/dd"),
                                 YAxis = (float)g.Average(t => t.Transaction.Amount)
                             })
                             .Reverse()
                             .ToList();
                    break;

                case GroupByInterval.Week:
                    var firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
                    var calendar = CultureInfo.CurrentCulture.Calendar;

                    result = currentTypeTransactions
                             .GroupBy(t =>
                             {
                                 var weekStart = t.Transaction.TimeStamp.Date.AddDays(
                                     -((7 + (int)t.Transaction.TimeStamp.DayOfWeek - (int)firstDayOfWeek) % 7));
                                 return new
                                 {
                                     WeekNumber = calendar.GetWeekOfYear(t.Transaction.TimeStamp,
                                                                         CalendarWeekRule.FirstFourDayWeek,
                                                                         firstDayOfWeek),
                                     t.Transaction.TimeStamp.Year
                                 };
                             })
                             .Select(g => new DisplayTransactionGroup
                             {
                                 XAxis = g.Key.WeekNumber == 1
                                             ? new DateTime(g.Key.Year, 1, 1).ToString("yy/MM/dd")
                                             : calendar.AddDays(new DateTime(g.Key.Year, 1, 1),
                                                                (g.Key.WeekNumber - 1) * 7).ToString("yy/MM/dd"),
                                 YAxis = (float)g.Average(t => t.Transaction.Amount)
                             })
                             .Reverse()
                             .ToList();
                    break;

                case GroupByInterval.Month:
                    result = currentTypeTransactions
                             .GroupBy(t => new { t.Transaction.TimeStamp.Year, t.Transaction.TimeStamp.Month })
                             .Select(g => new DisplayTransactionGroup
                             {
                                 XAxis = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("yy/MM/dd"),
                                 YAxis = (float)g.Average(t => t.Transaction.Amount)
                             })
                             .Reverse()
                             .ToList();
                    break;

                case GroupByInterval.Year:
                    result = currentTypeTransactions
                             .GroupBy(t => t.Transaction.TimeStamp.Year)
                             .Select(g => new DisplayTransactionGroup
                             {
                                 XAxis = new DateTime(g.Key, 1, 1).ToString("yy/MM/dd"),
                                 YAxis = (float)g.Average(t => t.Transaction.Amount)
                             })
                             .Reverse()
                             .ToList();
                    break;
            }

            return [.. result];
        }
    }

    private static void ChangeGraph(IReadOnlyCollection<DisplayTransaction> currentTypeTransactions)
    {
        if (currentTypeTransactions.Count == 0) return;

        var (dividedFactor, dividedName) =
            CalculateDividedFactor((int)currentTypeTransactions.Average(x => Math.Abs(x.Transaction.Change)));
        changeGraphData ??= GroupTransactions(currentTypeTransactions, _groupInterval);

        if (ImPlot.BeginPlot(Service.Lang.GetText("ChangeGraph"), ImGui.GetContentRegionAvail()))
        {
            ImPlot.SetupAxis(ImAxis.X1, Service.Lang.GetText("Time"));
            ImPlot.SetupAxis(ImAxis.Y1, $"{Service.Lang.GetText("Change")} {dividedName}", ImPlotAxisFlags.AutoFit);
            ImPlot.SetupAxesLimits(0, changeGraphData.Length, changeGraphData.Min(x => x.YAxis),
                                   changeGraphData.Max(x => x.YAxis));

            var changeValues = changeGraphData.Select(x => (float)Math.Round(x.YAxis / dividedFactor, 6)).ToArray();
            var dateTimeValues = changeGraphData.Select(x => x.XAxis).ToArray();
            ImPlot.SetupAxisTicks(ImAxis.X1, 0, dateTimeValues.Length - 1, dateTimeValues.Length, dateTimeValues);
            ImPlot.PlotBars("", ref changeValues[0], dateTimeValues.Length);
            ImPlot.EndPlot();
        }

        return;

        DisplayTransactionGroup[] GroupTransactions(
            IEnumerable<DisplayTransaction> currentTypeTransactions, GroupByInterval interval)
        {
            var result = new List<DisplayTransactionGroup>();

            switch (interval)
            {
                case GroupByInterval.Day:
                    result = currentTypeTransactions
                             .GroupBy(t => t.Transaction.TimeStamp.Date)
                             .Select(g => new DisplayTransactionGroup
                             {
                                 XAxis = g.Key.ToString("yy/MM/dd"),
                                 YAxis = (float)g.Average(t => t.Transaction.Change)
                             })
                             .Reverse()
                             .ToList();
                    break;

                case GroupByInterval.Week:
                    var firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
                    var calendar = CultureInfo.CurrentCulture.Calendar;

                    result = currentTypeTransactions
                             .GroupBy(t =>
                             {
                                 var weekStart = t.Transaction.TimeStamp.Date.AddDays(
                                     -((7 + (int)t.Transaction.TimeStamp.DayOfWeek - (int)firstDayOfWeek) % 7));
                                 return new
                                 {
                                     WeekNumber = calendar.GetWeekOfYear(t.Transaction.TimeStamp,
                                                                         CalendarWeekRule.FirstFourDayWeek,
                                                                         firstDayOfWeek),
                                     t.Transaction.TimeStamp.Year
                                 };
                             })
                             .Select(g => new DisplayTransactionGroup
                             {
                                 XAxis = g.Key.WeekNumber == 1
                                             ? new DateTime(g.Key.Year, 1, 1).ToString("yy/MM/dd")
                                             : calendar.AddDays(new DateTime(g.Key.Year, 1, 1),
                                                                (g.Key.WeekNumber - 1) * 7).ToString("yy/MM/dd"),
                                 YAxis = (float)g.Average(t => t.Transaction.Change)
                             })
                             .Reverse()
                             .ToList();
                    break;

                case GroupByInterval.Month:
                    result = currentTypeTransactions
                             .GroupBy(t => new { t.Transaction.TimeStamp.Year, t.Transaction.TimeStamp.Month })
                             .Select(g => new DisplayTransactionGroup
                             {
                                 XAxis = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("yy/MM/dd"),
                                 YAxis = (float)g.Average(t => t.Transaction.Change)
                             })
                             .Reverse()
                             .ToList();
                    break;

                case GroupByInterval.Year:
                    result = currentTypeTransactions
                             .GroupBy(t => t.Transaction.TimeStamp.Year)
                             .Select(g => new DisplayTransactionGroup
                             {
                                 XAxis = new DateTime(g.Key, 1, 1).ToString("yy/MM/dd"),
                                 YAxis = (float)g.Average(t => t.Transaction.Change)
                             })
                             .Reverse()
                             .ToList();
                    break;
            }

            return [.. result];
        }
    }

    private static void LocationGraph(IReadOnlyCollection<DisplayTransaction> currentTypeTransactions)
    {
        if (currentTypeTransactions.Count == 0) return;

        locationGraphData ??= [.. currentTypeTransactions
                              .GroupBy(transaction => transaction.Transaction.LocationName)
                              .Select(group => new DisplayTransactionGroup { XAxis = group.Key, YAxis = group.Count() })
                              .OrderByDescending(item => item.YAxis)];

        if (ImPlot.BeginPlot(Service.Lang.GetText("LocationGraph"), ImGui.GetContentRegionAvail()))
        {
            ImPlot.SetupAxis(ImAxis.X1, Service.Lang.GetText("Location"));
            ImPlot.SetupAxis(ImAxis.Y1, $"###{Service.Lang.GetText("Change")}", ImPlotAxisFlags.AutoFit);
            ImPlot.SetupAxesLimits(0, locationGraphData.Length, -2, locationGraphData.Max(x => x.YAxis));

            var locationValues = locationGraphData.Select(x => x.YAxis).ToArray();
            var countValues = locationGraphData.Select(x => x.XAxis).ToArray();
            ImPlot.SetupAxisTicks(ImAxis.X1, 0, countValues.Length - 1, countValues.Length, countValues);
            ImPlot.PlotBars("", ref locationValues[0], countValues.Length);
            ImPlot.EndPlot();
        }
    }

    private static void LocationAmountGraph(IReadOnlyCollection<DisplayTransaction> currentTypeTransactions)
    {
        if (currentTypeTransactions.Count == 0) return;

        var (dividedFactor, dividedName) =
            CalculateDividedFactor((int)currentTypeTransactions.Average(x => Math.Abs(x.Transaction.Change)));
        locationAmountGraphData ??= [.. currentTypeTransactions
                                    .GroupBy(transaction => transaction.Transaction.LocationName)
                                    .Select(group => new DisplayTransactionGroup
                                    {
                                        XAxis = group.Key,
                                        YAxis = group.Sum(item => item.Transaction.Change / dividedFactor)
                                    })
                                    .OrderByDescending(item => item.YAxis)];

        if (ImPlot.BeginPlot(Service.Lang.GetText("LocationAmountGraph"), ImGui.GetContentRegionAvail()))
        {
            ImPlot.SetupAxis(ImAxis.X1, Service.Lang.GetText("Location"));
            ImPlot.SetupAxis(ImAxis.Y1, $"{Service.Lang.GetText("Amount")} ({dividedName})", ImPlotAxisFlags.AutoFit);
            ImPlot.SetupAxesLimits(0, locationAmountGraphData.Length, locationAmountGraphData.Min(x => x.YAxis),
                                   locationAmountGraphData.Max(x => x.YAxis));

            var locationValues = locationAmountGraphData.Select(x => x.YAxis).ToArray();
            var amountValues = locationAmountGraphData.Select(x => x.XAxis).ToArray();
            ImPlot.SetupAxisTicks(ImAxis.X1, 0, amountValues.Length - 1, amountValues.Length, amountValues);
            ImPlot.PlotBars("", ref locationValues[0], amountValues.Length);
            ImPlot.EndPlot();
        }
    }

    private static (float, string) CalculateDividedFactor(int averageAmount)
    {
        var dividedFactor = 1;
        var dividedName = "";

        switch (averageAmount)
        {
            case >= 1000000000:
                dividedFactor = 1000000000;
                dividedName += Service.Lang.GetText("DividedUnitBil");
                break;
            case >= 1000000:
                dividedFactor = 1000000;
                dividedName += Service.Lang.GetText("DividedUnitMil");
                break;
            case >= 1000:
                dividedFactor = 1000;
                dividedName += Service.Lang.GetText("DividedUnitThou");
                break;
        }

        return (dividedFactor, dividedName);
    }

    public void Dispose() { }
}
