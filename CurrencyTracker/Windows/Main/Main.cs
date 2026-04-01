using System;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Tracker;
using CurrencyTracker.Manager.Transactions;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using OmenTools.Threading.TaskHelper;

namespace CurrencyTracker.Windows;

public class DisplayTransaction
{
    public Transaction Transaction { get; set; } = null!;
    public bool        Selected    { get; set; }
}

public partial class Main : Window, IDisposable
{
    public static uint SelectedCurrencyID { get; set; } = 0;

    private static bool ShowRecordOptions = true;
    private static bool ShowOthers        = true;
    private static bool ShouldRefreshTransactions;

    private static TaskHelper? TaskHelper;

    public Main() : base("Currency Tracker")
    {
        Flags |= ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        TaskHelper ??= new TaskHelper { TimeoutMS = 5_000 };

        TrackerManager.CurrencyChanged += OnCurrencyChanged;

        ReloadOrderedOptions();
    }

    public override void OnOpen()
    {
        if (SelectedCurrencyID != 0 && ShouldRefreshTransactions)
        {
            UpdateTransactions(SelectedCurrencyID, currentView, currentViewID);
            ShouldRefreshTransactions = false;
        }
    }

    public override void Draw()
    {
        if (!DService.Instance().ClientState.IsLoggedIn) return;

        DrawCategory(ref ShowRecordOptions, Service.Lang.GetText("Category-RecordOptions"), RecordOptionsUI);
        if (!ShowRecordOptions && !ShowOthers) ImGui.SameLine();
        DrawCategory(ref ShowOthers, Service.Lang.GetText("Category-Others"), OthersUI);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        CurrencyListboxUI();
        TransactionTableUI();
    }

    private static void DrawCategory(ref bool showUI, string labelText, Action uiAction)
    {
        ImGui.TextColored(showUI ? ImGuiColors.DalamudYellow : ImGuiColors.DalamudGrey, labelText);

        if (ImGui.IsItemClicked())
            showUI = !showUI;

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(Service.Lang.GetText("Category-Help"));

        if (showUI)
            uiAction();
    }

    public void Dispose()
    {
        TrackerManager.CurrencyChanged -= OnCurrencyChanged;

        TaskHelper?.Abort();
        TaskHelper = null;
    }
}
