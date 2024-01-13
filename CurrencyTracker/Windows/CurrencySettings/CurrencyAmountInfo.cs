namespace CurrencyTracker.Windows;

public partial class CurrencySettings
{
    private void CurrencyAmountInfoUI()
    {
        M.characterCurrencyInfos[P.CurrentCharacter].SubCurrencyAmount.TryGetValue(selectedCurrencyID, out var infoDic);

        if (infoDic.Any())
        {
            ImGui.TextColored(ImGuiColors.DalamudYellow, "Amount:");

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
