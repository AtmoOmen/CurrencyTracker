using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Transactions;
using Dalamud.Interface.Utility;
using ImGuiNET;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public partial class Main
{
    private static bool isLocationFilterEnabled;
    private static string? searchLocationName = string.Empty;
    private static string? editedLocationName = string.Empty;

    private static void LocationColumnHeaderUI()
    {
        ImGui.BeginDisabled(SelectedCurrencyID == 0 || currentTypeTransactions.Count <= 0);
        ImGuiOm.SelectableFillCell(Service.Lang.GetText("Location"));
        ImGui.EndDisabled();

        if (ImGui.BeginPopupContextItem("LocationSearch"))
        {
            ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
            if (ImGui.InputTextWithHint("###LocationSearch", Service.Lang.GetText("PleaseSearch"), ref searchLocationName, 80))
            {
                isLocationFilterEnabled = !string.IsNullOrEmpty(searchLocationName);
                RefreshTransactionsView();
            }
            ImGui.EndPopup();
        }
    }

    private static void LocationColumnCellUI(int i, DisplayTransaction transaction)
    {
        var locationName = transaction.Transaction.LocationName;

        ImGui.Selectable($"{locationName}##_{i}");

        if (!string.IsNullOrEmpty(locationName)) ImGuiOm.TooltipHover(locationName);

        if (!ImGui.IsKeyDown(ImGuiKey.LeftCtrl) && !transaction.Selected && ImGui.BeginPopupContextItem($"LocationEdit{i}"))
        {
            if (ImGui.IsWindowAppearing())
                editedLocationName = locationName;
            
            if (!string.IsNullOrEmpty(editedLocationName)) ImGui.TextWrapped(editedLocationName);

            ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
            if (ImGui.InputText($"##EditLocationContent_{i}", ref editedLocationName, 150,
                                ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll))
            {
                var failCount = TransactionsHandler.EditSpecificTransactions(
                    SelectedCurrencyID, [transaction.Transaction], editedLocationName, "None", currentView,
                    currentViewID);

                if (failCount == 0)
                    RefreshTransactionsView();
                else Service.Chat.PrintError($"{Service.Lang.GetText("EditFailed")}");
            }
            ImGui.EndPopup();
        }
    }
}
