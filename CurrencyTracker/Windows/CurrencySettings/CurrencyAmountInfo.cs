using System.Linq;
using CurrencyTracker.Manager;
using ImGuiNET;

namespace CurrencyTracker.Windows;

public partial class CurrencySettings
{
    private static void CurrencyAmountInfoUI()
    {
        Main.CharacterCurrencyInfos.FirstOrDefault(x => x.Character.ContentID == Service.ClientState.LocalContentId).SubCurrencyAmount.TryGetValue(Main.SelectedCurrencyID, out var infoDic);

        if (infoDic.Count != 0)
        {
            if (ImGui.CollapsingHeader($"{Service.Lang.GetText("Amount")}"))
            {
                ImGui.BeginGroup();
                foreach (var source in infoDic)
                {
                    if (source.Value == 0) continue;
                    ImGui.Text(GetSelectedViewName(source.Key.Category, source.Key.ID));
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
