namespace CurrencyTracker.Windows;

public partial class Main : Window, IDisposable
{
    private readonly Configuration? C = Plugin.Configuration;
    private readonly Plugin? P = Plugin.Instance;

    private bool showRecordOptions = true;
    private bool showOthers = true;

    public Main(Plugin plugin) : base("Currency Tracker")
    {
        Flags |= ImGuiWindowFlags.NoScrollbar;
        Flags |= ImGuiWindowFlags.NoScrollWithMouse;
        Flags |= ImGuiWindowFlags.NoBringToFrontOnFocus;

        Initialize(plugin);
    }

    private void Initialize(Plugin plugin)
    {
        Service.Tracker.CurrencyChanged += OnCurrencyChanged;

        searchTimer.Elapsed += SearchTimerElapsed;
        searchTimer.AutoReset = false;

        searchTimerACC.Elapsed += SearchTimerACCElapsed;
        searchTimerACC.AutoReset = false;

        searchTimerMCS.Elapsed += SearchTimerMCSElapsed;
        searchTimerMCS.AutoReset = false;

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

        searchTimer.Elapsed -= SearchTimerElapsed;
        searchTimer.Stop();
        searchTimer.Dispose();

        searchTimerACC.Elapsed -= SearchTimerACCElapsed;
        searchTimerACC.Stop();
        searchTimerACC.Dispose();

        searchTimerMCS.Elapsed -= SearchTimerMCSElapsed;
        searchTimerMCS.Stop();
        searchTimerMCS.Dispose();
    }
}
