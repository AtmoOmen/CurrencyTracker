namespace CurrencyTracker.Windows;

public partial class CurrencySettings
{
    private Dictionary<uint, string>? TerritoryNamesTR;
    private string searchFilterTR = string.Empty;
    private uint selectedAreaIDTR = 0;
    private uint selectedAreaIDDeleteTR = 0;
    private readonly Timer searchTimerTR = new(100);

    private int radioButtonsTRWidth = 250;

    private void TerrioryRestrictedUI()
    {
        var rules = C.CurrencyRules[selectedCurrencyID];
        var isBlacklist = !rules.RegionRulesMode;

        ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("Main-CS-AreaRestriction")}:");

        ImGui.BeginGroup();
        if (ImGui.RadioButton($"{Service.Lang.GetText("Blacklist")}", isBlacklist))
        {
            rules.RegionRulesMode = false;
            C.Save();
        }

        ImGui.SameLine();
        if (ImGui.RadioButton($"{Service.Lang.GetText("Whitelist")}", !isBlacklist))
        {
            rules.RegionRulesMode = true;
            C.Save();
        }

        ImGui.SameLine();
        ImGui.Text("");
        ImGui.EndGroup();

        radioButtonsTRWidth = (int)ImGui.GetItemRectSize().X;

        ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("Main-CS-SelectArea")}:");

        ImGui.SetNextItemWidth(radioButtonsTRWidth);
        if (ImGui.BeginCombo("##AreaResticted", TerrioryHandler.TerritoryNames.TryGetValue(selectedAreaIDTR, out var selectedAreaName) ? selectedAreaName : Service.Lang.GetText("PleaseSelect"), ImGuiComboFlags.HeightLarge))
        {
            ImGui.TextUnformatted("");
            ImGui.SameLine(8f, 0);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 8f);
            if (ImGui.InputText("", ref searchFilterTR, 50)) searchTimerTR.Restart();

            foreach (var area in TerritoryNamesTR ?? TerrioryHandler.TerritoryNames) if (ImGui.Selectable($"{area.Key} | {area.Value}")) selectedAreaIDTR = area.Key;
            ImGui.EndCombo();
        }

        if (!string.IsNullOrEmpty(selectedAreaName)) ImGuiOm.TooltipHover(selectedAreaName);

        ImGui.SameLine();
        if (ImGuiOm.ButtonIcon("AddRestrictedAreas", FontAwesomeIcon.Plus) && !rules.RestrictedAreas.Contains(selectedAreaIDTR))
        {
            rules.RestrictedAreas.Add(selectedAreaIDTR);
            selectedAreaIDTR = 0;
            C.Save();
        }

        ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("Main-CS-RestrictedArea")}:");

        ImGui.SetNextItemWidth(radioButtonsTRWidth);
        using (var combo = ImRaii.Combo("##RestrictedAreas", selectedAreaIDDeleteTR != 0 ? TerrioryHandler.TerritoryNames[selectedAreaIDDeleteTR] : "", ImGuiComboFlags.HeightLarge))
        {
            if (combo) foreach (var area in rules.RestrictedAreas) if (ImGui.Selectable($"{area} | {TerrioryHandler.TerritoryNames[area]}")) selectedAreaIDDeleteTR = area;
        }

        ImGui.SameLine();
        if (ImGuiOm.ButtonIcon("DeleteRestrictedAreas", FontAwesomeIcon.TrashAlt))
        {
            if (rules.RestrictedAreas.Contains(selectedAreaIDDeleteTR))
            {
                rules.RestrictedAreas.Remove(selectedAreaIDDeleteTR);
                selectedAreaIDDeleteTR = 0;
                C.Save();
            }
        }

    }

    private void LoadDataTR()
    {
        if (string.IsNullOrEmpty(searchFilterTR))
        {
            TerritoryNamesTR = TerrioryHandler.TerritoryNames;
        }
        else
        {
            TerritoryNamesTR = TerrioryHandler.TerritoryNames
                .Where(x => x.Value.Contains(searchFilterTR, StringComparison.OrdinalIgnoreCase) || x.Key.ToString().Contains(searchFilterTR, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(x => x.Key, x => x.Value);
        }
    }

    private void SearchTimerTRElapsed(object? sender, ElapsedEventArgs e)
    {
        LoadDataTR();
    }
}
