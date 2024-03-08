using System.Linq;
using Dalamud.Interface.Colors;
using ImGuiNET;
using static CurrencyTracker.Manager.Tools.Helpers;

namespace CurrencyTracker.Windows;

public partial class CurrencySettings
{
    private void CurrencyAmountInfoUI()
    {
        M.characterCurrencyInfos[P.CurrentCharacter].SubCurrencyAmount.TryGetValue(selectedCurrencyID, out var infoDic);

        if (infoDic.Any())
        {
            if (YellowTextHeader($"{Service.Lang.GetText("Amount")}:"))
            {
                ImGui.BeginGroup();
                foreach (var source in infoDic)
                {
                    if (source.Value == 0) continue;
                    ImGui.Text(GetSelectedViewName(source.Key.Category, source.Key.Id));
                }

                ImGui.EndGroup();

                ImGui.SameLine();
                ImGui.Spacing();

                ImGui.SameLine();
                ImGui.BeginGroup();
                foreach (var source in infoDic)
                {
                    if (source.Value == 0) continue;
                    ImGui.Text(source.Value.ToString("N0"));
                }

                ImGui.EndGroup();
            }
        }

        return;

        bool YellowTextHeader(string text)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudYellow);
            var result = ImGui.CollapsingHeader(text);
            ImGui.PopStyleColor();
            return result;
        }
    }
}
