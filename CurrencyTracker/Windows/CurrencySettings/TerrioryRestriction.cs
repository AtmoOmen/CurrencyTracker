using System;
using System.Linq;
using System.Numerics;
using CurrencyTracker.Manager;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

namespace CurrencyTracker.Windows;

public partial class CurrencySettings
{
    private string searchFilterTR = string.Empty;

    private static Vector2 CheckboxSize = ImGuiHelpers.ScaledVector2(20f);

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

        var childSize = ImGuiHelpers.ScaledVector2(280f, 250f);
        using (ImRaii.Child("TerritoryRestrictedChildOut", childSize, true))
        {
            ImGui.SetNextItemWidth(-1f);
            ImGui.InputTextWithHint
                ("###ZoneSearchInput", Service.Lang.GetText("PleaseSearch"), ref searchFilterTR, 32);

            ImGui.Separator();

            using (ImRaii.Child("TerritoryRestrictedChildInner", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.NoScrollbar))
            {
                var tableSize = new Vector2(ImGui.GetContentRegionAvail().X, 0);
                if (ImGui.BeginTable("###ZoneSelectTable", 2, ImGuiTableFlags.Borders, tableSize))
                {
                    ImGui.TableSetupColumn("Checkbox", ImGuiTableColumnFlags.WidthFixed, CheckboxSize.X);
                    ImGui.TableSetupColumn("PlaceName");

                    ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
                    ImGui.TableNextColumn();
                    ImGui.Text("");
                    ImGui.TableNextColumn();
                    ImGui.Text("Location");

                    var selectedCopy = rules.RestrictedAreas;
                    var data = TerritoryNames.OrderByDescending(x => selectedCopy.Contains(x.Key));
                    foreach (var (placeID, placeName) in data)
                    {
                        if (!string.IsNullOrWhiteSpace(searchFilterTR) &&
                            !placeName.Contains(searchFilterTR, StringComparison.OrdinalIgnoreCase)) continue;

                        using (ImRaii.PushId($"{placeName}_{placeID}"))
                        {
                            ImGui.TableNextRow();

                            ImGui.TableNextColumn();
                            ImGui.BeginDisabled();
                            var state = selectedCopy.Contains(placeID);
                            ImGui.Checkbox("", ref state);
                            CheckboxSize = ImGui.GetItemRectSize();
                            ImGui.EndDisabled();

                            ImGui.TableNextColumn();
                            if (ImGui.Selectable($"{placeName} ({placeID})", state,
                                                 ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.DontClosePopups))
                            {
                                if (!rules.RestrictedAreas.Remove(placeID))
                                    rules.RestrictedAreas.Add(placeID);
                            }
                        }
                    }

                    ImGui.EndTable();
                }
            }
        }
    }
}
