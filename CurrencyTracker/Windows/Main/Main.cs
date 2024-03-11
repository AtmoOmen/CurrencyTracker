using System;
using CurrencyTracker.Manager.Trackers.Components;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ECommons.Automation;
using ImGuiNET;

namespace CurrencyTracker.Windows;

public partial class Main : Window, IDisposable
{
    private bool showRecordOptions = true;
    private bool showOthers = true;

    private static TaskManager? TaskManager;


    public Main(Plugin plugin) : base("Currency Tracker")
    {
        Flags |= ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBringToFrontOnFocus;

        TaskManager ??= new TaskManager { AbortOnTimeout = true, TimeLimitMS = 5000, ShowDebug = false };
        Initialize(plugin);
    }

    private void Initialize(Plugin plugin)
    {
        Service.Tracker.CurrencyChanged += OnCurrencyChanged;
        Service.Tracker.CurrencyChanged += ServerBar.OnCurrencyChanged;

        searchTimerMCS.Elapsed += SearchTimerMCSElapsed;
        searchTimerMCS.AutoReset = false;

        startDatePicker.DateSelected += RefreshTransactionsView;
        endDatePicker.DateSelected += RefreshTransactionsView;

        lastLangTF = Service.Lang.Language;

        ReloadOrderedOptions();
    }

    public override void Draw()
    {
        if (!Service.ClientState.IsLoggedIn) return;

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
        Service.Tracker.CurrencyChanged -= OnCurrencyChanged;
        Service.Tracker.CurrencyChanged -= ServerBar.OnCurrencyChanged;

        ServerBar.DtrEntry.Dispose();

        TaskManager.Abort();

        searchTimerMCS.Elapsed -= SearchTimerMCSElapsed;
        searchTimerMCS.Stop();
        searchTimerMCS.Dispose();

        startDatePicker.DateSelected -= RefreshTransactionsView;
        endDatePicker.DateSelected -= RefreshTransactionsView;
    }
}
