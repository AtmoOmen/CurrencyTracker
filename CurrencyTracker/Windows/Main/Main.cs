using System;
using System.Linq;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Tasks;
using CurrencyTracker.Manager.Trackers;
using CurrencyTracker.Manager.Transactions;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace CurrencyTracker.Windows;

public partial class Main : Window, IDisposable
{
    public static uint SelectedCurrencyID { get; set; } = 0;

    private static bool _showRecordOptions = true;
    private static bool _showOthers = true;
    private static bool _shouldRefreshTransactions;

    public class DisplayTransaction
    {
        public Transaction Transaction { get; set; } = null!;
        public bool Selected { get; set; }
    }

    private static TaskManager? TaskManager;


    public Main(Plugin plugin) : base("Currency Tracker")
    {
        Flags |= ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse |
                 ImGuiWindowFlags.NoBringToFrontOnFocus;

        TaskManager ??= new TaskManager { AbortOnTimeout = true, TimeLimitMS = 5000, ShowDebug = false };

        Tracker.CurrencyChanged += OnCurrencyChanged;

        startDatePicker.DateSelected += RefreshTransactionsView;
        endDatePicker.DateSelected += RefreshTransactionsView;
        Service.Lang.LanguageChange += SwitchDatePickerLanguage;

        ReloadOrderedOptions();
    }

    public override void OnOpen()
    {
        if (visibleColumns == Array.Empty<string>())
            visibleColumns = Service.Config.ColumnsVisibility.Where(c => c.Value).Select(c => c.Key).ToArray();

        if (SelectedCurrencyID != 0 && _shouldRefreshTransactions)
        {
            UpdateTransactions(SelectedCurrencyID, currentView, currentViewID);
            _shouldRefreshTransactions = false;
        }

        base.OnOpen();
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

        TaskManager?.Abort();

        startDatePicker.DateSelected -= RefreshTransactionsView;
        endDatePicker.DateSelected -= RefreshTransactionsView;
        Service.Lang.LanguageChange -= SwitchDatePickerLanguage;
    }
}
