using System;
using CurrencyTracker.Helpers.TaskHelper;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Trackers;
using CurrencyTracker.Manager.Transactions;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace CurrencyTracker.Windows;

public class DisplayTransaction
{
    public Transaction Transaction { get; set; } = null!;
    public bool Selected { get; set; }
}

public partial class Main : Window, IDisposable
{
    public static uint SelectedCurrencyID { get; set; } = 0;

    private static bool _showRecordOptions = true;
    private static bool _showOthers = true;
    private static bool _shouldRefreshTransactions;

    private static TaskHelper? TaskHelper;

    public Main(Plugin _) : base("Currency Tracker")
    {
        Flags |= ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        TaskHelper ??= new TaskHelper() { TimeLimitMS = 5000 };

        Tracker.CurrencyChanged += OnCurrencyChanged;

        ReloadOrderedOptions();
    }

    public override void OnOpen()
    {
        if (SelectedCurrencyID != 0 && _shouldRefreshTransactions)
        {
            UpdateTransactions(SelectedCurrencyID, currentView, currentViewID);
            _shouldRefreshTransactions = false;
        }
    }

    public override void Draw()
    {
        if (!Service.ClientState.IsLoggedIn) return;

        DrawCategory(ref _showRecordOptions, Service.Lang.GetText("Category-RecordOptions"), RecordOptionsUI);
        if (!_showRecordOptions && !_showOthers) ImGui.SameLine();
        DrawCategory(ref _showOthers, Service.Lang.GetText("Category-Others"), OthersUI);

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
        Tracker.CurrencyChanged -= OnCurrencyChanged;

        TaskHelper?.Abort();
        TaskHelper = null;
    }
}
