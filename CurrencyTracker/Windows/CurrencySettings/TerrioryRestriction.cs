using System;
using System.Collections.Generic;
using System.Linq;
using CurrencyTracker.Manager;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using ImGuiNET;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public partial class CurrencySettings
{
    private Dictionary<uint, string>? TerritoryNamesTR;
    private string searchFilterTR = string.Empty;
    private uint selectedAreaIDTR;
    private uint selectedAreaIDDeleteTR;

    private int radioButtonsTRWidth = 250;

    private void TerritoryRestrictedUI()
    {
        var rules = Service.Config.CurrencyRules[Main.SelectedCurrencyID];
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
        ImGui.Text("");
        ImGui.EndGroup();

        radioButtonsTRWidth = (int)ImGui.GetItemRectSize().X;

        ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("Main-CS-SelectArea")}:");

        ImGui.SetNextItemWidth(radioButtonsTRWidth);
        if (ImGui.BeginCombo("##AreaRestricted", TerritoryNames.TryGetValue(selectedAreaIDTR, out var selectedAreaName) ? selectedAreaName : Service.Lang.GetText("PleaseSelect"), ImGuiComboFlags.HeightLarge))
        {
            ImGui.TextUnformatted("");
            ImGui.SameLine(8f, 0);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 8f);
            if (ImGui.InputText("", ref searchFilterTR, 50))
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

            foreach (var area in TerritoryNamesTR ?? TerritoryNames) 
                if (ImGui.Selectable($"{area.Value} ({area.Key})")) 
                    selectedAreaIDTR = area.Key;
            ImGui.EndCombo();
        }

        if (!string.IsNullOrEmpty(selectedAreaName)) ImGuiOm.TooltipHover(selectedAreaName);

        ImGui.SameLine();
        if (ImGuiOm.ButtonIcon("AddRestrictedAreas", FontAwesomeIcon.Plus) && !rules.RestrictedAreas.Contains(selectedAreaIDTR))
        {
            rules.RestrictedAreas.Add(selectedAreaIDTR);
            selectedAreaIDTR = 0;
            Service.Config.Save();
        }

        ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("Main-CS-RestrictedArea")}:");

        ImGui.SetNextItemWidth(radioButtonsTRWidth);
        if (ImGui.BeginCombo("##RestrictedAreas", selectedAreaIDDeleteTR != 0 ? TerritoryNames[selectedAreaIDDeleteTR] : "", ImGuiComboFlags.HeightLarge))
        {
            foreach (var area in rules.RestrictedAreas) 
                if (ImGui.Selectable($"{TerritoryNames[area]} ({area})"))
                    selectedAreaIDDeleteTR = area;
            ImGui.EndCombo();
        }

        ImGui.SameLine();
        if (ImGuiOm.ButtonIcon("DeleteRestrictedAreas", FontAwesomeIcon.TrashAlt))
        {
            if (rules.RestrictedAreas.Remove(selectedAreaIDDeleteTR))
            {
                selectedAreaIDDeleteTR = 0;
                Service.Config.Save();
            }
        }

    }
}
