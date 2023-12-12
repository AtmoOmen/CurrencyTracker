namespace CurrencyTracker.Windows;

public partial class Main : Window, IDisposable
{
    public Main(Plugin plugin) : base("Currency Tracker")
    {
        Flags |= ImGuiWindowFlags.NoScrollbar;
        Flags |= ImGuiWindowFlags.NoScrollWithMouse;
        Flags |= ImGuiWindowFlags.NoBringToFrontOnFocus;

        Initialize(plugin);
    }

    // 初始化 Initialize
    private void Initialize(Plugin plugin)
    {
        transactionsPerPage = C.RecordsPerPage;
        isChangeColoring = C.ChangeTextColoring;
        positiveChangeColor = C.PositiveChangeColor;
        negativeChangeColor = C.NegativeChangeColor;
        childWidthOffset = C.ChildWidthOffset;
        autoSaveInterval = C.AutoSaveInterval;
        maxBackupFilesCount = C.MaxBackupFilesCount;

        if (filterEndDate.Month == 1 && filterEndDate.Day == 1) filterStartDate = new DateTime(DateTime.Now.Year - 1, 12, 31);
        else filterStartDate = filterStartDate = filterEndDate.AddDays(-1);

        searchTimer.Elapsed += SearchTimerElapsed;
        searchTimer.AutoReset = false;

        searchTimerCCT.Elapsed += SearchTimerCCTElapsed;
        searchTimerCCT.AutoReset = false;

        searchTimerMCS.Elapsed += SearchTimerMCSElapsed;
        searchTimerMCS.AutoReset = false;

        searchTimerCS.Elapsed += SearchTimerCSElapsed;
        searchTimerCS.AutoReset = false;

        LoadOptions();
    }

    // 将预置货币类型、玩家自定义的货币类型加入选项列表 Add preset currencies and player-customed currencies to the list of options
    private void LoadOptions()
    {
        foreach (var currencyKey in C.AllCurrencies.Keys)
        {
            if (!selectedStates.ContainsKey(currencyKey))
            {
                selectedStates.Add(currencyKey, new());
                selectedTransactions.Add(currencyKey, new());
            }
        }

        if (C.OrderedOptions == null || C.OrderedOptions.Count == 0)
        {
            C.OrderedOptions = C.AllCurrencies.Keys.ToList();
            C.Save();
        }
        else
        {
            ReloadOrderedOptions();
        }
    }

    public override void Draw()
    {
        if (!Service.ClientState.IsLoggedIn) return;

        windowWidth = ImGui.GetWindowWidth();

        DrawCategory(ref showRecordOptions, Service.Lang.GetText("Category-RecordOptions"), RecordOptionsUI);
        if (!showRecordOptions && !showOthers) ImGui.SameLine();
        DrawCategory(ref showOthers, Service.Lang.GetText("Category-Others"), OthersUI);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        CurrencyListboxUI();
        TransactionTableUI();
    }

    private static void DrawCategory(ref bool showUI, string labelText, System.Action uiAction)
    {
        ImGui.TextColored(showUI ? ImGuiColors.DalamudYellow : ImGuiColors.DalamudGrey, labelText);
        if (ImGui.IsItemClicked())
        {
            showUI = !showUI;
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(Service.Lang.GetText("Category-Help"));
        }
        if (showUI)
        {
            uiAction();
        }
    }

    public void Dispose()
    {
        searchTimer.Elapsed -= SearchTimerElapsed;
        searchTimer.Stop();
        searchTimer.Dispose();

        searchTimerCCT.Elapsed -= SearchTimerCCTElapsed;
        searchTimerCCT.Stop();
        searchTimerCCT.Dispose();

        searchTimerMCS.Elapsed -= SearchTimerMCSElapsed;
        searchTimerMCS.Stop();
        searchTimerMCS.Dispose();
    }
}
