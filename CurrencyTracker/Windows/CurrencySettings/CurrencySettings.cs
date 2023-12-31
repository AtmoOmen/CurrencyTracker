namespace CurrencyTracker.Windows;

public partial class CurrencySettings : Window, IDisposable
{
    private readonly Configuration? C = Plugin.Configuration;
    private readonly Plugin? P = Plugin.Instance;
    private readonly Main? M = Plugin.Instance.Main;

    private uint selectedCurrencyID = 0;
    private int currencyInfoGroupWidth = 200;

    public CurrencySettings(Plugin plugin) : base($"Currency Settings##{Plugin.Name}")
    {
        Flags |= ImGuiWindowFlags.NoScrollbar;
        Flags |= ImGuiWindowFlags.AlwaysAutoResize;

        Initialize(plugin);
    }

    public void Initialize(Plugin plugin)
    {
        searchTimerTR.Elapsed += SearchTimerTRElapsed;
        searchTimerTR.AutoReset = false;
    }

    public override void Draw()
    {
        if (M == null || M.selectedCurrencyID == 0)
        {
            this.IsOpen = false;
            return;
        }

        selectedCurrencyID = M.selectedCurrencyID;
        ImGui.BeginGroup();
        using (var tab0 = ImRaii.TabBar("CurrencySettingsCT"))
        {
            if (tab0)
            {
                using (var item0 = ImRaii.TabItem(Service.Lang.GetText("Info")))
                {
                    if (item0)
                    {
                        CurrencyInfoGroupUI();

                        ImGui.Separator();
                        RenameCurrencyUI();
                    }
                }

                using (var item0 = ImRaii.TabItem(Service.Lang.GetText("Main-CS-AreaRestriction")))
                {
                    if (item0)
                    {
                        TerrioryRestrictedUI();
                    }
                }

                using (var item0 = ImRaii.TabItem("Alert"))
                {
                    if (item0)
                    {
                        IntervalAlertUI();
                    }
                }
            }

        }
        ImGui.EndGroup();
    }

    private void DrawBackgroundImage()
    {
        var region = ImGui.GetContentRegionAvail();
        var minDimension = Math.Min(region.X, region.Y);

        var areaStart = ImGui.GetCursorPos();
        ImGui.SetCursorPosX(region.X / 2.0f - minDimension / 2.0f);
        ImGui.Image(C.AllCurrencyIcons[1].ImGuiHandle, new Vector2(minDimension), Vector2.Zero, Vector2.One, Vector4.One with { W = 0.10f });
        ImGui.SetCursorPos(areaStart);
    }

    private void CurrencyInfoGroupUI()
    {
        ImGui.BeginGroup();
        ImGui.Image(C.AllCurrencyIcons[selectedCurrencyID].ImGuiHandle, new Vector2(48 * ImGuiHelpers.GlobalScale));

        ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.SetWindowFontScale(1.6f);
        var currencyName = C.AllCurrencies[selectedCurrencyID];
        ImGui.Text($"{currencyName}");

        if (ImGui.IsItemClicked()) editedCurrencyName = currencyName;

        ImGui.SameLine();
        ImGui.Text("");

        if (!M.characterCurrencyInfos.Any()) M.LoadDataMCS();
        ImGui.SetWindowFontScale(1);
        ImGui.Text($"{Service.Lang.GetText("Amount")}: {(M.characterCurrencyInfos[P.CurrentCharacter].CurrencyAmount.TryGetValue(selectedCurrencyID, out var amount) ? amount : 0)}");

        ImGui.SameLine();
        ImGui.Text("");

        ImGui.EndGroup();
        ImGui.EndGroup();

        currencyInfoGroupWidth = (int)(ImGui.GetItemRectSize().X);
    }

    public void Dispose() 
    {

        searchTimerTR.Elapsed -= SearchTimerTRElapsed;
        searchTimerTR.Stop();
        searchTimerTR.Dispose();
    }
}
