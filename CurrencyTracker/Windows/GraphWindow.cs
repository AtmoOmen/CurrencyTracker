using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Transactions;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace CurrencyTracker.Windows;

public class GraphWindow : Window, IDisposable
{
    private static readonly Dictionary<uint, string> ViewLoc = new()
    {
        [0] = Service.Lang.GetText("AmountGraph"),
        [1] = Service.Lang.GetText("ChangeGraph"),
        [2] = Service.Lang.GetText("LocationGraph"),
        [3] = Service.Lang.GetText("LocationAmountGraph"),
    };
    
    private static uint SelectedCurrencyID => 
        Main.SelectedCurrencyID;

    private static List<DisplayTransaction> CurrentTransactions => 
        Main.currentTransactions;
    
    private static uint            SelectedPlotType;
    private static GroupInterval groupInterval;
    
    public GraphWindow() : base($"Graphs##{Name}") => 
        Flags |= ImGuiWindowFlags.NoScrollbar;

    public override void Draw()
    {
        if (SelectedCurrencyID == 0)
        {
            P.Graph.IsOpen = false;
            return;
        }

        var currencyName = CurrencyInfo.GetLocalName(SelectedCurrencyID);
        var currencyIcon = CurrencyInfo.GetIcon(SelectedCurrencyID).ImGuiHandle;

        ImGui.SetWindowFontScale(1.3f);
        var currentCursorPos = ImGui.GetCursorPos();
        using (ImRaii.Group())
        {
            ImGui.SetCursorPosY(currentCursorPos.Y + 2f);
            ImGui.Image(currencyIcon, ImGuiHelpers.ScaledVector2(24f));

            ImGui.SameLine();
            ImGui.SetCursorPosY(currentCursorPos.Y + 2f);
            ImGui.TextColored(ImGuiColors.DalamudOrange, currencyName);
        }
        ImGui.SetWindowFontScale(1f);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
        ImGui.SetCursorPosY(currentCursorPos.Y + 2f);
        using (var combo = ImRaii.Combo("###GraphsViewSelectCombo", ViewLoc[SelectedPlotType], ImGuiComboFlags.HeightLarge))
        {
            if (combo)
            {
                foreach (var view in ViewLoc)
                {
                    if (ImGui.Selectable(view.Value, view.Key == SelectedPlotType))
                        SelectedPlotType = view.Key;
                }
            }
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(100f * ImGuiHelpers.GlobalScale);
        ImGui.SetCursorPosY(currentCursorPos.Y + 2f);

        using (var combo = ImRaii.Combo("###GraphsIntervalSelectCombo", groupInterval.ToString(), ImGuiComboFlags.HeightLarge))
        {
            if (combo)
            {
                foreach (var view in Enum.GetValues<GroupInterval>())
                {
                    if (ImGui.Selectable(view.ToString(), view == groupInterval))
                        groupInterval = view;
                }
            }
        }

        ImGui.SameLine();
        ImGui.SetCursorPosY(currentCursorPos.Y + 2f);
        if (ImGui.Button(FontAwesomeIcon.SyncAlt.ToIconString()))
        {
            AmountGraphPlot.Instance.ClearCachedData();
            ChangeGraphPlot.Instance.ClearCachedData();
            LocationGraphPlot.Instance.ClearCachedData();
            LocationAmountGraphPlot.Instance.ClearCachedData();
        }
        
        if (CurrentTransactions.Count <= 0) return;
        
        switch (SelectedPlotType)
        {
            case 0:
                AmountGraphPlot.Instance.Draw();
                break;
            case 1:
                ChangeGraphPlot.Instance.Draw();
                break;
            case 2:
                LocationGraphPlot.Instance.Draw();
                break;
            case 3:
                LocationAmountGraphPlot.Instance.Draw();
                break;
        }
    }
    
    public void Dispose() { }
    
    // 金额图表类
    private class AmountGraphPlot : BaseGraphPlot
    {
        public static readonly AmountGraphPlot Instance = new();
        
        private static DisplayTransactionGroup[]? cachedData;
        
        protected override string GraphTitle => Service.Lang.GetText("AmountGraph");
        protected override DisplayTransactionGroup[]? GetCachedData() => cachedData;
        protected override void SetCachedData(DisplayTransactionGroup[] data) => cachedData = data;
        public override void ClearCachedData() => cachedData = null;

        protected override DisplayTransactionGroup[] CreateData() => 
            GroupTransactionsByTime(CurrentTransactions, groupInterval, t => t.Amount);

        protected override void DrawPlot(DisplayTransactionGroup[] data)
        {
            var (dividedFactor, dividedName) = CalculateDividedFactor((int)Math.Abs(CurrentTransactions.Average(t => t.Transaction.Amount)));

            using var plot = ImRaii.Plot(GraphTitle, ImGui.GetContentRegionAvail(), ImPlotFlags.None);
            if (!plot) return;

            ImPlot.SetupAxesLimits(-2, data.Length + 2, -2, data.Max(x => x.YAxis) + 5);
            ImPlot.SetupAxis(ImAxis.X1, Service.Lang.GetText("Time"));
            ImPlot.SetupAxis(ImAxis.Y1, $"{Service.Lang.GetText("Amount")} {dividedName}", ImPlotAxisFlags.AutoFit);

            var amountValues = data.Select(x => (float)Math.Round(x.YAxis / dividedFactor, 6)).ToArray();
            var dateTimeValues = data.Select(x => x.XAxis).ToArray();

            ImPlot.SetupAxisTicks(ImAxis.X1, 0, dateTimeValues.Length - 1, dateTimeValues.Length, dateTimeValues);
            ImPlot.PlotBars(string.Empty, ref amountValues[0], dateTimeValues.Length);
        }
    }

    // 变化图表类  
    private class ChangeGraphPlot : BaseGraphPlot
    {
        public static readonly ChangeGraphPlot Instance = new();
        
        private static DisplayTransactionGroup[]? cachedData;
        
        protected override string GraphTitle => Service.Lang.GetText("ChangeGraph");
        protected override DisplayTransactionGroup[]? GetCachedData() => cachedData;
        protected override void SetCachedData(DisplayTransactionGroup[] data) => cachedData = data;
        public override void ClearCachedData() => cachedData = null;

        protected override DisplayTransactionGroup[] CreateData() => 
            GroupTransactionsByTime(CurrentTransactions, groupInterval, t => t.Change);

        protected override void DrawPlot(DisplayTransactionGroup[] data)
        {
            var (dividedFactor, dividedName) = CalculateDividedFactor((int)CurrentTransactions.Average(x => Math.Abs(x.Transaction.Change)));

            using var plot = ImRaii.Plot(GraphTitle, ImGui.GetContentRegionAvail(), ImPlotFlags.None);
            if (!plot) return;

            ImPlot.SetupAxis(ImAxis.X1, Service.Lang.GetText("Time"));
            ImPlot.SetupAxis(ImAxis.Y1, $"{Service.Lang.GetText("Change")} {dividedName}", ImPlotAxisFlags.AutoFit);
            ImPlot.SetupAxesLimits(0, data.Length, data.Min(x => x.YAxis), data.Max(x => x.YAxis));

            var changeValues = data.Select(x => (float)Math.Round(x.YAxis / dividedFactor, 6)).ToArray();
            var dateTimeValues = data.Select(x => x.XAxis).ToArray();
            
            ImPlot.SetupAxisTicks(ImAxis.X1, 0, dateTimeValues.Length - 1, dateTimeValues.Length, dateTimeValues);
            ImPlot.PlotBars(string.Empty, ref changeValues[0], dateTimeValues.Length);
        }
    }

    // 地点图表类
    private class LocationGraphPlot : BaseGraphPlot
    {
        public static readonly LocationGraphPlot Instance = new();
        
        private static DisplayTransactionGroup[]? cachedData;
        
        protected override string GraphTitle => Service.Lang.GetText("LocationGraph");
        protected override DisplayTransactionGroup[]? GetCachedData() => cachedData;
        protected override void SetCachedData(DisplayTransactionGroup[] data) => cachedData = data;
        public override void ClearCachedData() => cachedData = null;

        protected override DisplayTransactionGroup[] CreateData() =>
        [
            .. CurrentTransactions
               .GroupBy(transaction => transaction.Transaction.LocationName)
               .Select(group => new DisplayTransactionGroup { XAxis = group.Key, YAxis = group.Count() })
               .OrderByDescending(item => item.YAxis)
        ];

        protected override void DrawPlot(DisplayTransactionGroup[] data)
        {
            using var plot = ImRaii.Plot(GraphTitle, ImGui.GetContentRegionAvail(), ImPlotFlags.None);
            if (!plot) return;

            ImPlot.SetupAxis(ImAxis.X1, Service.Lang.GetText("Location"));
            ImPlot.SetupAxis(ImAxis.Y1, $"###{Service.Lang.GetText("Change")}", ImPlotAxisFlags.AutoFit);
            ImPlot.SetupAxesLimits(0, data.Length, -2, data.Max(x => x.YAxis));

            var locationValues = data.Select(x => x.YAxis).ToArray();
            var countValues = data.Select(x => x.XAxis).ToArray();
            
            ImPlot.SetupAxisTicks(ImAxis.X1, 0, countValues.Length - 1, countValues.Length, countValues);
            ImPlot.PlotBars(string.Empty, ref locationValues[0], countValues.Length);
        }
    }

    // 地点金额图表类
    private class LocationAmountGraphPlot : BaseGraphPlot
    {
        public static readonly LocationAmountGraphPlot Instance = new();
        
        private static DisplayTransactionGroup[]? cachedData;
        
        protected override string GraphTitle => Service.Lang.GetText("LocationAmountGraph");
        protected override DisplayTransactionGroup[]? GetCachedData() => cachedData;
        protected override void SetCachedData(DisplayTransactionGroup[] data) => cachedData = data;
        public override void ClearCachedData() => cachedData = null;

        protected override DisplayTransactionGroup[] CreateData()
        {
            var (dividedFactor, _) = CalculateDividedFactor((int)CurrentTransactions.Average(x => Math.Abs(x.Transaction.Change)));
            
            return [.. CurrentTransactions
                       .GroupBy(transaction => transaction.Transaction.LocationName)
                       .Select(group => new DisplayTransactionGroup
                       {
                           XAxis = group.Key,
                           YAxis = group.Sum(item => item.Transaction.Change / dividedFactor)
                       })
                       .OrderByDescending(item => item.YAxis)];
        }

        protected override void DrawPlot(DisplayTransactionGroup[] data)
        {
            var (_, dividedName) = CalculateDividedFactor((int)CurrentTransactions.Average(x => Math.Abs(x.Transaction.Change)));

            using var plot = ImRaii.Plot(GraphTitle, ImGui.GetContentRegionAvail(), ImPlotFlags.None);
            if (!plot) return;

            ImPlot.SetupAxis(ImAxis.X1, Service.Lang.GetText("Location"));
            ImPlot.SetupAxis(ImAxis.Y1, $"{Service.Lang.GetText("Amount")} ({dividedName})", ImPlotAxisFlags.AutoFit);
            ImPlot.SetupAxesLimits(0, data.Length, data.Min(x => x.YAxis), data.Max(x => x.YAxis));

            var locationValues = data.Select(x => x.YAxis).ToArray();
            var amountValues = data.Select(x => x.XAxis).ToArray();
            
            ImPlot.SetupAxisTicks(ImAxis.X1, 0, amountValues.Length - 1, amountValues.Length, amountValues);
            ImPlot.PlotBars(string.Empty, ref locationValues[0], amountValues.Length);
        }
    }
    
    private abstract class BaseGraphPlot
    {
        protected abstract string GraphTitle { get; }

        protected abstract DisplayTransactionGroup[]? GetCachedData();

        protected abstract void SetCachedData(DisplayTransactionGroup[] data);

        public abstract void ClearCachedData();

        public void Draw()
        {
            if (CurrentTransactions.Count == 0) return;

            var data = GetOrCreateData();
            if (data == null || data.Length == 0) return;

            DrawPlot(data);
        }

        protected abstract DisplayTransactionGroup[]? CreateData();

        protected abstract void DrawPlot(DisplayTransactionGroup[] data);

        private DisplayTransactionGroup[]? GetOrCreateData()
        {
            var cachedData = GetCachedData();
            if (cachedData != null) return cachedData;

            var newData = CreateData();
            if (newData != null) SetCachedData(newData);
            return newData;
        }

        protected static (float, string) CalculateDividedFactor(int averageAmount)
        {
            var dividedFactor = 1;
            var dividedName   = string.Empty;

            switch (averageAmount)
            {
                case >= 10_0000_0000:
                    dividedFactor =  1000000000;
                    dividedName   += Service.Lang.GetText("DividedUnitBil");
                    break;
                case >= 100_0000:
                    dividedFactor =  1000000;
                    dividedName   += Service.Lang.GetText("DividedUnitMil");
                    break;
                case >= 1000:
                    dividedFactor =  1000;
                    dividedName   += Service.Lang.GetText("DividedUnitThou");
                    break;
            }

            return (dividedFactor, dividedName);
        }

        protected static DisplayTransactionGroup[] GroupTransactionsByTime(
            IEnumerable<DisplayTransaction> transactions, GroupInterval interval, Func<Transaction, float> valueSelector)
        {
            var result = new List<DisplayTransactionGroup>();

            switch (interval)
            {
                case GroupInterval.Day:
                    result = transactions
                             .GroupBy(t => t.Transaction.TimeStamp.Date)
                             .Select(g => new DisplayTransactionGroup
                             {
                                 XAxis = g.Key.ToString("yy/MM/dd"),
                                 YAxis = g.Average(t => valueSelector(t.Transaction))
                             })
                             .Reverse()
                             .ToList();
                    break;

                case GroupInterval.Week:
                    var firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
                    var calendar       = CultureInfo.CurrentCulture.Calendar;

                    result = transactions
                             .GroupBy(t => new
                             {
                                 WeekNumber = calendar.GetWeekOfYear(t.Transaction.TimeStamp, CalendarWeekRule.FirstFourDayWeek, firstDayOfWeek),
                                 t.Transaction.TimeStamp.Year
                             })
                             .Select(g => new DisplayTransactionGroup
                             {
                                 XAxis = g.Key.WeekNumber == 1
                                             ? new DateTime(g.Key.Year, 1, 1).ToString("yy/MM/dd")
                                             : calendar.AddDays(new DateTime(g.Key.Year, 1, 1), (g.Key.WeekNumber - 1) * 7).ToString("yy/MM/dd"),
                                 YAxis = g.Average(t => valueSelector(t.Transaction))
                             })
                             .Reverse()
                             .ToList();
                    break;

                case GroupInterval.Month:
                    result = transactions
                             .GroupBy(t => new { t.Transaction.TimeStamp.Year, t.Transaction.TimeStamp.Month })
                             .Select(g => new DisplayTransactionGroup
                             {
                                 XAxis = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("yy/MM/dd"),
                                 YAxis = g.Average(t => valueSelector(t.Transaction))
                             })
                             .Reverse()
                             .ToList();
                    break;

                case GroupInterval.Year:
                    result = transactions
                             .GroupBy(t => t.Transaction.TimeStamp.Year)
                             .Select(g => new DisplayTransactionGroup
                             {
                                 XAxis = new DateTime(g.Key, 1, 1).ToString("yy/MM/dd"),
                                 YAxis = g.Average(t => valueSelector(t.Transaction))
                             })
                             .Reverse()
                             .ToList();
                    break;
            }

            return [.. result];
        }
    }

    private class DisplayTransactionGroup
    {
        public string XAxis { get; init; } = string.Empty;
        public float  YAxis { get; init; }
    }
    
    private enum GroupInterval
    {
        Day,
        Week,
        Month,
        Year
    }
}
