using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CurrencyTracker.Manager;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ImPlotNET;

namespace CurrencyTracker.Windows;

public class Graph : Window, IDisposable
{
    private readonly Main Main = P.Main;
    private float[]? currencyChangeData;
    private float[]? currencyAmountData;

    public Graph(Plugin plugin) : base($"Graphs##{Name}")
    {
        Flags |= ImGuiWindowFlags.NoScrollbar;
    }

    public override void Draw()
    {
        if (!P.Main.IsOpen || Main.SelectedCurrencyID == 0)
        {
            P.Graph.IsOpen = false;
            return;
        }

        ImGui.Text($"{Service.Lang.GetText("Now")}:");
        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.DalamudOrange, Service.Config.AllCurrencies[Main.SelectedCurrencyID]);
        ImGui.SameLine();
        ImGui.Text(Service.Lang.GetText("GraphLabel"));
        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.DalamudOrange, Main.currentTypeTransactions.Count.ToString());

        if (Main.currentTypeTransactions.Count > 0)
        {
            AmountGraph(Main.currentTypeTransactions);
            ChangeGraph(Main.currentTypeTransactions);
            LocationGraph(Main.currentTypeTransactions);
            LocationAmountGraph(Main.currentTypeTransactions);
        }
    }

    private void AmountGraph(IReadOnlyCollection<Main.DisplayTransaction> currentTypeTransactions)
    {
        if (currentTypeTransactions.Count == 0) return;

        var averageAmount = (int)currentTypeTransactions.Average(x => Math.Abs(x.Transaction.Amount));
        var (dividedFactor, dividedName) = CalculateDividedFactor(averageAmount);

        currencyAmountData = currentTypeTransactions
                             .Select(x => (float)Math.Round(x.Transaction.Amount / dividedFactor, 6))
                             .Reverse()
                             .ToArray();

        var graphTitle = $"{Service.Lang.GetText("AmountGraph")}{dividedName}";

        if (ImGui.CollapsingHeader($"{graphTitle} {Service.Lang.GetText("AmountGraph1")}"))
        {
            const ImPlotFlags plotFlags = ImPlotFlags.None;
            var plotSize = new Vector2(ImGui.GetWindowWidth() - 10, 600);
            if (ImPlot.BeginPlot(graphTitle, plotSize, plotFlags))
            {
                ImPlot.SetupAxesLimits(0, currencyAmountData.Length, currencyAmountData.Min(),
                                       currencyAmountData.Max());
                ImPlot.SetupMouseText(ImPlotLocation.North, ImPlotMouseTextFlags.None);
                ImPlot.PlotLine("", ref currencyAmountData[0], currencyAmountData.Length);
                ImPlot.EndPlot();
            }
        }
    }

    private void ChangeGraph(IReadOnlyCollection<Main.DisplayTransaction> currentTypeTransactions)
    {
        if (currentTypeTransactions.Count == 0) return;

        var averageAmount = (int)currentTypeTransactions.Average(x => Math.Abs(x.Transaction.Change));
        var (dividedFactor, dividedName) = CalculateDividedFactor(averageAmount);

        currencyChangeData = currentTypeTransactions
                             .Select(x => (float)Math.Round(x.Transaction.Change / dividedFactor, 6))
                             .Reverse()
                             .ToArray();

        var graphTitle = $"{Service.Lang.GetText("ChangeGraph")}{dividedName}";

        if (ImGui.CollapsingHeader($"{graphTitle} {Service.Lang.GetText("ChangeGraph1")}"))
        {
            var plotFlags = ImPlotFlags.None;
            var plotSize = new Vector2(ImGui.GetWindowWidth() - 10, 600);
            if (ImPlot.BeginPlot(graphTitle, plotSize, plotFlags))
            {
                ImPlot.SetupAxesLimits(0, currencyChangeData.Length, currencyChangeData.Min(),
                                       currencyChangeData.Max());
                ImPlot.SetupMouseText(ImPlotLocation.North, ImPlotMouseTextFlags.None);
                ImPlot.PlotLine("", ref currencyChangeData[0], currencyChangeData.Length, 1, 0,
                                ImPlotLineFlags.SkipNaN);
                ImPlot.EndPlot();
            }
        }
    }

    private static void LocationGraph(IReadOnlyCollection<Main.DisplayTransaction> currentTypeTransactions)
    {
        if (currentTypeTransactions.Count == 0) return;

        var locationCounts = currentTypeTransactions
                             .GroupBy(transaction => transaction.Transaction.LocationName)
                             .Select(group => new { Location = group.Key, Count = group.Count() })
                             .OrderByDescending(item => item.Count)
                             .ToList();

        var locations = locationCounts.Select(item => item.Location).ToArray();
        var counts = locationCounts.Select(item => (float)item.Count).ToArray();

        var graphTitle = Service.Lang.GetText("LocationGraph");

        if (ImGui.CollapsingHeader($"{graphTitle} {Service.Lang.GetText("LocationGraph1")}"))
        {
            var plotFlags = ImPlotFlags.None;
            var plotSize = new Vector2(ImGui.GetWindowWidth() - 10, 600);
            if (ImPlot.BeginPlot(graphTitle, plotSize, plotFlags))
            {
                var positions = Enumerable.Range(0, locations.Length).Select(i => (double)i).ToArray();
                ImPlot.SetupAxisTicks(ImAxis.X1, ref positions[0], locations.Length, locations);
                ImPlot.PlotBarGroups([""], ref counts[0], 1, locations.Length);
                ImPlot.EndPlot();
            }
        }
    }

    private static void LocationAmountGraph(IReadOnlyCollection<Main.DisplayTransaction> currentTypeTransactions)
    {
        if (currentTypeTransactions.Count == 0) return;

        var averageAmount = (int)currentTypeTransactions.Average(x => Math.Abs(x.Transaction.Change));
        var (dividedFactor, dividedName) = CalculateDividedFactor(averageAmount);

        var locationAmounts = currentTypeTransactions
                              .GroupBy(transaction => transaction.Transaction.LocationName)
                              .Select(group => new
                              {
                                  Location = group.Key,
                                  AmountSum = group.Sum(item => item.Transaction.Change / dividedFactor)
                              })
                              .OrderByDescending(item => item.AmountSum)
                              .ToList();

        var locations = locationAmounts.Select(item => item.Location).ToArray();
        var amounts = locationAmounts.Select(item => item.AmountSum).ToArray();

        var graphTitle = $"{Service.Lang.GetText("LocationAmountGraph")}{dividedName}";

        if (ImGui.CollapsingHeader($"{graphTitle} {Service.Lang.GetText("LocationAmountGraph1")}"))
        {
            const ImPlotFlags plotFlags = ImPlotFlags.None;
            var plotSize = new Vector2(ImGui.GetWindowWidth() - 10, 600);
            if (ImPlot.BeginPlot(graphTitle, plotSize, plotFlags))
            {
                var positions = Enumerable.Range(0, locations.Length).Select(i => (double)i).ToArray();
                ImPlot.SetupAxisTicks(ImAxis.X1, ref positions[0], locations.Length, locations);
                ImPlot.PlotBarGroups([""], ref amounts[0], 1, locations.Length);
                ImPlot.EndPlot();
            }
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
