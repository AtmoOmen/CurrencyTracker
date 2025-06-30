using System.Collections.Generic;
using System.Linq;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager;
using ImGuiNET;

namespace CurrencyTracker.Windows;

public partial class CurrencySettings
{
    private static IReadOnlyDictionary<TransactionFileCategoryInfo, long> currencyAmountInfoDic =
        new Dictionary<TransactionFileCategoryInfo, long>();

    private static void CurrencyAmountInfoUI()
    {
        if (Throttler.Throttle("CurrencyAmountInfoUI", 1000))
        {
            Main.CharacterCurrencyInfos.FirstOrDefault(x => x.Character.ContentID == LocalPlayerState.ContentID).SubCurrencyAmount.TryGetValue(Main.SelectedCurrencyID, out currencyAmountInfoDic);
        }

        if (currencyAmountInfoDic.Count != 0)
        {
            if (ImGui.CollapsingHeader($"{Service.Lang.GetText("Amount")}"))
            {
                ImGui.BeginGroup();
                foreach (var source in currencyAmountInfoDic)
                {
                    if (source.Value == 0) continue;
                    ImGui.Text(GetSelectedViewName(source.Key.Category, source.Key.ID));
                }

                ImGui.EndGroup();

                ImGui.SameLine();
                ImGui.Spacing();

                ImGui.SameLine();
                ImGui.BeginGroup();
                foreach (var source in currencyAmountInfoDic)
                {
                    if (source.Value == 0) continue;
                    ImGui.Text(source.Value.ToString("N0"));
                }

                ImGui.EndGroup();
            }
        }
    }
}
