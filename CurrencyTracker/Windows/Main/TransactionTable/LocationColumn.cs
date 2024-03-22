using System.Collections.Generic;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Transactions;
using Dalamud.Interface.Utility.Raii;
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
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            ImGui.OpenPopup("LocationSearch");
        }
        ImGui.EndDisabled();

        using var popup = ImRaii.Popup("LocationSearch");
        if (popup.Success)
        {
            ImGui.SetNextItemWidth(250);
            if (ImGui.InputTextWithHint("##LocationSearch", Service.Lang.GetText("PleaseSearch"), ref searchLocationName, 80))
            {
                isLocationFilterEnabled = !string.IsNullOrEmpty(searchLocationName);
                RefreshTransactionsView();
            }
        }
    }

    private static void LocationColumnCellUI(int i, bool selected, Transaction transaction)
    {
        var locationName = transaction.LocationName;

        ImGui.Selectable($"{locationName}##_{i}");

        if (!string.IsNullOrEmpty(locationName)) ImGuiOm.TooltipHover(locationName);

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && !ImGui.IsKeyDown(ImGuiKey.LeftCtrl))
        {
            editedLocationName = locationName;
            ImGui.OpenPopup($"EditLocationName##_{i}");
        }

        using var popup = ImRaii.Popup($"EditLocationName##_{i}");
        if (popup.Success)
        {
            if (!string.IsNullOrEmpty(editedLocationName)) ImGui.TextWrapped(editedLocationName);

            ImGui.SetNextItemWidth(270);
            if (ImGui.InputText($"##EditLocationContent_{i}", ref editedLocationName, 150, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll))
            {
                var failCount = TransactionsHandler.EditSpecificTransactions(SelectedCurrencyID, new List<Transaction> { transaction }, editedLocationName, "None", currentView, currentViewID);

                if (failCount == 0)
                    RefreshTransactionsView();
                else Service.Chat.PrintError($"{Service.Lang.GetText("EditFailed")}");
            }
        }
    }
}
