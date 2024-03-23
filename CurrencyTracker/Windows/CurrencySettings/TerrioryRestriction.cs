using System;
using System.Collections.Generic;
using System.Linq;
using CurrencyTracker.Manager;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using ImGuiNET;

namespace CurrencyTracker.Windows;

public partial class CurrencySettings
{
    private Dictionary<uint, string>? TerritoryNamesTR;
    private string searchFilterTR = string.Empty;

    private int radioButtonsTRWidth = 250;

    private void TerritoryRestrictedUI()
    {
        if (!Service.Config.CurrencyRules.TryGetValue(Main.SelectedCurrencyID, out var rules))
        {
            rules = new();
            Service.Config.CurrencyRules.Add(Main.SelectedCurrencyID, rules);
            Service.Config.Save();
        }

        var isBlacklist = !rules.RegionRulesMode;

        ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("Main-CS-AreaRestriction")}:");

        ImGui.BeginGroup();
        if (ImGui.RadioButton($"{Service.Lang.GetText("Blacklist")}", isBlacklist))
        {
            rules.RegionRulesMode = false;
            Service.Config.Save();
        }

        ImGui.SameLine();
        if (ImGui.RadioButton($"{Service.Lang.GetText("Whitelist")}", !isBlacklist))
        {
            rules.RegionRulesMode = true;
            Service.Config.Save();
        }

        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.EndGroup();

        radioButtonsTRWidth = (int)ImGui.GetItemRectSize().X;

        ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("Main-CS-SelectArea")}:");

        ImGui.SetNextItemWidth(160f * ImGuiHelpers.GlobalScale);
        if (ImGui.BeginCombo("##AreaRestricted", Service.Lang.GetText("PleaseSelect"), ImGuiComboFlags.HeightLarge))
        {
            TerritoryNamesTR ??= TerritoryNames;
            rules.RestrictedAreas ??= [];

            ImGui.TextUnformatted("");
            ImGui.SameLine(8f, 0);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 8f);
            if (ImGui.InputTextWithHint("", Service.Lang.GetText("PleaseSearch"), ref searchFilterTR, 50))
            {
                TaskManager.Abort();

                TaskManager.DelayNext(500);
                TaskManager.Enqueue(() =>
                {
                    TerritoryNamesTR = string.IsNullOrEmpty(searchFilterTR) ? TerritoryNames : TerritoryNames
                                           .Where(x => x.Value.Contains(searchFilterTR, StringComparison.OrdinalIgnoreCase) 
                                                       || x.Key.ToString().Contains(searchFilterTR, StringComparison.OrdinalIgnoreCase))
                                           .ToDictionary(x => x.Key, x => x.Value);
                });
            }

            foreach (var area in TerritoryNamesTR)
                if (ImGui.Selectable($"{area.Value} ({area.Key})", rules.RestrictedAreas.Contains(area.Key), ImGuiSelectableFlags.DontClosePopups))
                {
                    if (!rules.RestrictedAreas.Remove(area.Key))
                        rules.RestrictedAreas.Add(area.Key);

                    Service.Config.Save();
                }
            ImGui.EndCombo();
        }
    }
}
