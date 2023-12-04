namespace CurrencyTracker.Windows;

public class Graph : Window, IDisposable
{
    private readonly Main Main = Plugin.Instance.Main;
    private List<TransactionsConvertor>? currentTypeTransactions = new();
    private float[]? currencyChangeData;
    private float[]? currencyAmountData;

    public Graph(Plugin plugin) : base($"Graphs##{Plugin.Name}")
    {
        Flags |= ImGuiWindowFlags.NoScrollbar;

        Initialize();
    }

    public void Dispose()
    {
    }

    private void Initialize()
    {
        if (Main.selectedCurrencyID == 0)
            return;

        if (Main.currentTypeTransactions == null)
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

        ImGui.Text($"{Service.Lang.GetText("Now")}:");
        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.DalamudOrange, Plugin.Configuration.AllCurrencies[Main.selectedCurrencyID]);
        ImGui.SameLine();
        ImGui.Text(Service.Lang.GetText("GraphLabel"));
        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.DalamudOrange, Main.currentTypeTransactions.Count.ToString());
        ImGui.SameLine();
        HelpMessages();

        currentTypeTransactions = Main.currentTypeTransactions;

        if (currentTypeTransactions != null)
        {
            AmountGraph(currentTypeTransactions);
            ChangeGraph(currentTypeTransactions);
            LocationGraph(currentTypeTransactions);
            LocationAmountGraph(currentTypeTransactions);
        }
    }

    private void HelpMessages()
    { ImGuiComponents.HelpMarker(Service.Lang.GetText("GraphHelpMessages1")); }

    private void AmountGraph(List<TransactionsConvertor> currentTypeTransactions)
    {
        if (currentTypeTransactions == null || !currentTypeTransactions.Any())
            return;

        var averageAmount = (int)currentTypeTransactions.Average(x => Math.Abs(x.Amount));
        var (dividedFactor, dividedName) = CaculateDividedFactor(averageAmount);

        currencyAmountData = currentTypeTransactions.Select(x => (float)Math.Round(x.Amount / dividedFactor, 6))
            .Reverse()
            .ToArray();

        var graphTitle = $"{Service.Lang.GetText("AmountGraph")}{dividedName}";

        if (ImGui.CollapsingHeader($"{graphTitle} {Service.Lang.GetText("AmountGraph1")}"))
        {
            var plotFlags = ImPlotFlags.None;
            var plotSize = new Vector2(ImGui.GetWindowWidth() - 10, 600);
            if (ImPlot.BeginPlot(graphTitle, plotSize, plotFlags))
            {
                ImPlot.SetupAxesLimits(0, currencyAmountData.Length, currencyAmountData.Min(), currencyAmountData.Max());
                ImPlot.SetupMouseText(ImPlotLocation.North, ImPlotMouseTextFlags.None);
                ImPlot.PlotLine("", ref currencyAmountData[0], currencyAmountData.Length);
                ImPlot.EndPlot();
            }

            ImGui.TextDisabled(Service.Lang.GetText("AmountGraph4"));
        }
    }

    private void ChangeGraph(List<TransactionsConvertor> currentTypeTransactions)
    {
        if (currentTypeTransactions == null || !currentTypeTransactions.Any())
            return;

        var averageAmount = (int)currentTypeTransactions.Average(x => Math.Abs(x.Change));
        var (dividedFactor, dividedName) = CaculateDividedFactor(averageAmount);

        currencyChangeData = currentTypeTransactions.Select(x => (float)Math.Round(x.Change / dividedFactor, 6))
            .Reverse()
            .ToArray();

        var graphTitle = $"{Service.Lang.GetText("ChangeGraph")}{dividedName}";

        if (ImGui.CollapsingHeader($"{graphTitle} {Service.Lang.GetText("ChangeGraph1")}"))
        {
            var plotFlags = ImPlotFlags.None;
            var plotSize = new Vector2(ImGui.GetWindowWidth() - 10, 600);
            if (ImPlot.BeginPlot(graphTitle, plotSize, plotFlags))
            {
                ImPlot.SetupAxesLimits(0, currencyChangeData.Length, currencyChangeData.Min(), currencyChangeData.Max());
                ImPlot.SetupMouseText(ImPlotLocation.North, ImPlotMouseTextFlags.None);
                ImPlot.PlotLine("", ref currencyChangeData[0], currencyChangeData.Length, 1, 0, ImPlotLineFlags.SkipNaN);
                ImPlot.EndPlot();
            }

            ImGui.TextDisabled(Service.Lang.GetText("ChangeGraph2"));
        }
    }

    private static void LocationGraph(List<TransactionsConvertor> currentTypeTransactions)
    {
        if (currentTypeTransactions == null || !currentTypeTransactions.Any())
            return;

        var locationCounts = currentTypeTransactions
            .GroupBy(transaction => transaction.LocationName)
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
                ImPlot.PlotBarGroups(new string[] { "" }, ref counts[0], 1, locations.Length);
                ImPlot.EndPlot();
            }

            ImGui.TextDisabled(Service.Lang.GetText("LocationGraph2"));
        }
    }

    private static void LocationAmountGraph(List<TransactionsConvertor> currentTypeTransactions)
    {
        if (currentTypeTransactions == null || !currentTypeTransactions.Any())
            return;

        var averageAmount = (int)currentTypeTransactions.Average(x => Math.Abs(x.Change));
        var (dividedFactor, dividedName) = CaculateDividedFactor(averageAmount);

        var locationAmounts = currentTypeTransactions
            .GroupBy(transaction => transaction.LocationName)
            .Select(group => new { Location = group.Key, AmountSum = group.Sum(item => item.Change / dividedFactor) })
            .OrderByDescending(item => item.AmountSum)
            .ToList();

        var locations = locationAmounts.Select(item => item.Location).ToArray();
        var amounts = locationAmounts.Select(item => (float)item.AmountSum).ToArray();

        var graphTitle = $"{Service.Lang.GetText("LocationAmountGraph")}{dividedName}";

        if (ImGui.CollapsingHeader($"{graphTitle} {Service.Lang.GetText("LocationAmountGraph1")}"))
        {
            var plotFlags = ImPlotFlags.None;
            var plotSize = new Vector2(ImGui.GetWindowWidth() - 10, 600);
            if (ImPlot.BeginPlot(graphTitle, plotSize, plotFlags))
            {
                var positions = Enumerable.Range(0, locations.Length).Select(i => (double)i).ToArray();
                ImPlot.SetupAxisTicks(ImAxis.X1, ref positions[0], locations.Length, locations);
                ImPlot.PlotBarGroups(new string[] { "" }, ref amounts[0], 1, locations.Length);
                ImPlot.EndPlot();
            }

            ImGui.TextDisabled(Service.Lang.GetText("LocationAmountGraph2"));
        }
    }

    private static (float, string) CaculateDividedFactor(int averageAmount)
    {
        var dividedFactor = 1;
        var dividedName = "";

        if (averageAmount >= 1000000000)
        {
            dividedFactor = 1000000000;
            dividedName += Service.Lang.GetText("DividedUnitBil");
        }
        else if (averageAmount >= 1000000)
        {
            dividedFactor = 1000000;
            dividedName += Service.Lang.GetText("DividedUnitMil");
        }
        else if (averageAmount >= 1000)
        {
            dividedFactor = 1000;
            dividedName += Service.Lang.GetText("DividedUnitThou");
        }

        return (dividedFactor, dividedName);
    }
}
