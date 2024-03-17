using System.Linq;
using CurrencyTracker.Manager;
using ImGuiNET;

namespace CurrencyTracker.Windows;

public partial class CurrencySettings
{
    private void CurrencyAmountInfoUI()
    {
        Main.CharacterCurrencyInfos[P.CurrentCharacter].SubCurrencyAmount.TryGetValue(selectedCurrencyID, out var infoDic);

        if (infoDic.Any())
        {
            if (ImGui.CollapsingHeader($"{Service.Lang.GetText("Amount")}"))
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
    }
}
