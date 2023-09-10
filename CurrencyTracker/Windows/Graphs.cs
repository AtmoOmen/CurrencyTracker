using CurrencyTracker.Manager;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ImPlotNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace CurrencyTracker.Windows;

#pragma warning disable CS8602

public class Graph : Window, IDisposable
{
    internal static LanguageManager? Lang;
    private readonly Main main = Plugin.Instance.Main;
    private List<TransactionsConvertor>? currentTypeTransactions = new List<TransactionsConvertor>();
    private float[]? currencyChangeData;
    private float[]? currencyAmountData;

    // 选择的语言 Selected Language
    internal string playerLang = string.Empty;

    public Graph(Plugin plugin) : base("Currency Tracker - Graphs")
    {
        Flags |= ImGuiWindowFlags.NoScrollbar;

        Initialize();
        Lang = new LanguageManager(main.playerLang);
    }

    public void Dispose()
    {
    }

    private void Initialize()
    {
        if (main.selectedCurrencyName == null)
            return;

        if (main.currentTypeTransactions == null)
        {
            if (Plugin.Instance.Graph.IsOpen)
                Plugin.Instance.Graph.IsOpen = false;
            return;
        }
    }

    public override unsafe void Draw()
    {
        if (!Plugin.Instance.Main.IsOpen) 
        {
            Plugin.Instance.Graph.IsOpen = false;
        }

        ImGui.Text(Lang.GetText("CustomCurrencyLabel2"));
        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.DalamudOrange, main.selectedCurrencyName);
        ImGui.SameLine();
        ImGui.Text(Lang.GetText("GraphLabel"));
        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.DalamudOrange, main.currentTypeTransactions.Count.ToString());
        ImGui.SameLine();
        HelpMessages();

        currentTypeTransactions = main.currentTypeTransactions;

        if (currentTypeTransactions != null)
        {
            AmountGraph(currentTypeTransactions);
            ChangeGraph(currentTypeTransactions);
            LocationGraph(currentTypeTransactions);
            LocationAmountGraph(currentTypeTransactions);
        }
    }

    private void HelpMessages()
    { ImGuiComponents.HelpMarker(Lang.GetText("GraphHelpMessages1")); }

    private void AmountGraph(List<TransactionsConvertor> currentTypeTransactions)
    {
        var averageAmount = (int)currentTypeTransactions.Average(x => Math.Abs(x.Amount));
        var dividedFactor = CaculateDividedFactor(averageAmount).Item1;
        var dividedName = CaculateDividedFactor(averageAmount).Item2;

        currencyAmountData = currentTypeTransactions.Select(x => (float)Math.Round(x.Amount / dividedFactor, 6))
            .Reverse()
            .ToArray();

        if (currencyAmountData != null)
        {
            var latestValue = currencyAmountData[currencyAmountData.Length - 1];
            var secondToLastValue = currencyAmountData[currencyAmountData.Length - 2];
            var oldestValue = currencyAmountData[0];
            var overallChangeRate = Math.Round((latestValue - oldestValue) / oldestValue * 100f, 2);
            var newestChangeRate = Math.Round((latestValue - secondToLastValue) / secondToLastValue * 100f, 2);

            var graphTitle = $"{Lang.GetText("AmountGraph")}{dividedName}";

            if (ImGui.CollapsingHeader($"{graphTitle}{Lang.GetText("AmountGraph1")}"))
            {
                if (ImPlot.BeginPlot(
                    graphTitle,
                    new Vector2(ImGui.GetWindowWidth() - 10, 600),
                    ImPlotFlags.NoMenus | ImPlotFlags.NoBoxSelect | ImPlotFlags.NoMouseText))
                {
                    ImPlot.SetupAxesLimits(
                        0,
                        currencyAmountData.Length,
                        currencyAmountData.Min(),
                        currencyAmountData.Max());
                    ImPlot.SetupMouseText(ImPlotLocation.North, ImPlotMouseTextFlags.None);
                    ImPlot.PlotBars("", ref currencyAmountData[0], currencyAmountData.Length);

#if DEV
                    if (ImPlot.IsAxisHovered(ImAxis.X1))
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text($"{Lang.GetText("AmountGraph2")}{overallChangeRate}%%");
                        AppendChangeRateToolTip(overallChangeRate);
                        ImGui.Text($"{Lang.GetText("AmountGraph3")}{newestChangeRate}%%");
                        AppendChangeRateToolTip(newestChangeRate);
                        ImGui.EndTooltip();
                    }
#endif
                    ImPlot.EndPlot();
                }

                ImGui.TextDisabled(Lang.GetText("AmountGraph4"));
            }
        }
    }

    private void ChangeGraph(List<TransactionsConvertor> currentTypeTransactions)
    {
        var averageAmount = (int)currentTypeTransactions.Average(x => Math.Abs(x.Change));
        var dividedFactor = CaculateDividedFactor(averageAmount).Item1;
        var dividedName = CaculateDividedFactor(averageAmount).Item2;

        currencyChangeData = currentTypeTransactions.Select(x => (float)Math.Round(x.Change / dividedFactor, 6))
            .Reverse()
            .ToArray();

        if (currencyChangeData != null)
        {
            var graphTitle = $"{Lang.GetText("ChangeGraph")}{dividedName}";

            if (ImGui.CollapsingHeader($"{graphTitle}{Lang.GetText("ChangeGraph1")}"))
            {
                if (ImPlot.BeginPlot(
                    graphTitle,
                    new Vector2(ImGui.GetWindowWidth() - 10, 600),
                    ImPlotFlags.NoMenus | ImPlotFlags.NoBoxSelect | ImPlotFlags.NoMouseText))
                {
                    ImPlot.SetupAxesLimits(
                        0,
                        currencyChangeData.Length,
                        currencyChangeData.Min(),
                        currencyChangeData.Max());
                    ImPlot.SetupMouseText(ImPlotLocation.North, ImPlotMouseTextFlags.None);
                    ImPlot.PlotLine(
                        "",
                        ref currencyChangeData[0],
                        currencyChangeData.Length,
                        1,
                        0,
                        ImPlotLineFlags.SkipNaN);
                    ImPlot.EndPlot();
                }
                ImGui.TextDisabled(Lang.GetText("ChangeGraph2"));
            }
        }
    }

    private void LocationGraph(List<TransactionsConvertor> currentTypeTransactions)
    {
        var locationCounts = currentTypeTransactions
            .GroupBy(transaction => transaction.LocationName)
            .Select(group => new { Location = group.Key, Count = group.Count() })
            .OrderByDescending(item => item.Count)
            .ToList();

        string[] locations = locationCounts.Select(item => item.Location).ToArray();
        string[] locationsLegend = { "" };
        float[] counts = locationCounts.Select(item => (float)item.Count).ToArray();

        if (locationCounts != null)
        {
            var graphTitle = Lang.GetText("LocationGraph");

            if (ImGui.CollapsingHeader($"{graphTitle}{Lang.GetText("LocationGraph1")}"))
            {
                if (ImPlot.BeginPlot(
                    graphTitle,
                    new Vector2(ImGui.GetWindowWidth() - 10, 600),
                    ImPlotFlags.NoMenus | ImPlotFlags.NoBoxSelect | ImPlotFlags.NoMouseText))
                {
                    double[] positions = new double[locations.Length];
                    for (int i = 0; i < locations.Length; i++)
                    {
                        positions[i] = i;
                    }
                    ImPlot.SetupAxisTicks(ImAxis.X1, ref positions[0], locations.Length, locations);
                    ImPlot.PlotBarGroups(locationsLegend, ref counts[0], 1, locations.Length);
                    ImPlot.EndPlot();
                }

                ImGui.TextDisabled(Lang.GetText("LocationGraph2"));
            }
        }
    }

    private void LocationAmountGraph(List<TransactionsConvertor> currentTypeTransactions)
    {
        var averageAmount = (int)currentTypeTransactions.Average(x => Math.Abs(x.Change));
        var dividedFactor = CaculateDividedFactor(averageAmount).Item1;
        var dividedName = CaculateDividedFactor(averageAmount).Item2;

        var locationAmounts = currentTypeTransactions
        .GroupBy(transaction => transaction.LocationName)
            .Select(group => new { Location = group.Key, AmountSum = group.Sum(item => item.Change / dividedFactor) })
            .OrderByDescending(item => item.AmountSum)
            .ToList();

        string[] locations = locationAmounts.Select(item => item.Location).ToArray();
        string[] locationsLegend = { "" };
        float[] amounts = locationAmounts.Select(item => (float)item.AmountSum).ToArray();

        if (locationAmounts != null)
        {
            var graphTitle = $"{Lang.GetText("LocationAmountGraph")}{dividedName}";

            if (ImGui.CollapsingHeader($"{graphTitle}{Lang.GetText("LocationAmountGraph1")}"))
            {
                if (ImPlot.BeginPlot(
                    graphTitle,
                    new Vector2(ImGui.GetWindowWidth() - 10, 600),
                    ImPlotFlags.NoMenus | ImPlotFlags.NoBoxSelect | ImPlotFlags.NoMouseText))
                {
                    double[] positions = new double[locations.Length];
                    for (int i = 0; i < locations.Length; i++)
                    {
                        positions[i] = i;
                    }
                    ImPlot.SetupAxisTicks(ImAxis.X1, ref positions[0], locations.Length, locations);
                    ImPlot.PlotBarGroups(locationsLegend, ref amounts[0], 1, locations.Length);
                    ImPlot.EndPlot();
                }
                ImGui.TextDisabled(Lang.GetText("LocationAmountGraph2"));
            }
        }
    }

    private (float, string) CaculateDividedFactor(int averageAmount)
    {
        float dividedFactor = 1;
        string dividedName = "";

        if (averageAmount < 1000)
        {
            dividedFactor = 1;
            dividedName += "";
        }
        else if (averageAmount >= 1000 && averageAmount < 1000000)
        {
            dividedFactor = 1000;
            dividedName += Lang.GetText("DividedName");
        }
        else if (averageAmount >= 1000000 && averageAmount < 1000000000)
        {
            dividedFactor = 1000000;
            dividedName += Lang.GetText("DividedName1");
        }
        else if (averageAmount >= 1000000000)
        {
            dividedFactor = 1000000000;
            dividedName += Lang.GetText("DividedName2");
        }
        return (dividedFactor, dividedName);
    }

    private void AppendChangeRateToolTip(double changRate)
    {
        if (changRate > 0)
        {
            ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.DPSRed, Lang.GetText("ChangeRateToolTip"));
        }
        if (changRate < 0)
        {
            ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.HealerGreen, Lang.GetText("ChangeRateToolTip1"));
        }
    }
}
