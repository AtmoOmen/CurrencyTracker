using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Transactions;
using Dalamud.Interface.Utility;
using ImGuiNET;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public class LocationColumn : TableColumn
{
    internal static bool IsLocationFilterEnabled;
    internal static string? SearchLocationName = string.Empty;
    internal static string? editedLocationName = string.Empty;

    public override void Header()
    {
        ImGui.BeginDisabled(SelectedCurrencyID == 0 || CurrentTransactions.Count <= 0);
        ImGuiOm.SelectableFillCell(Service.Lang.GetText("Location"));
        ImGui.EndDisabled();

        if (ImGui.BeginPopupContextItem("LocationSearch"))
        {
            ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
            ImGui.InputTextWithHint("###LocationSearch", Service.Lang.GetText("PleaseSearch"), ref SearchLocationName, 128);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                IsLocationFilterEnabled = !string.IsNullOrEmpty(SearchLocationName);
                RefreshTable();
            }

            ImGui.EndPopup();
        }
    }

    public override void Cell(int i, DisplayTransaction transaction)
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
                    SelectedCurrencyID, [transaction.Transaction], editedLocationName, "None", CurrentView, CurrentViewID);

                if (failCount == 0) RefreshTable();
                else Service.Chat.PrintError($"{Service.Lang.GetText("EditFailed")}");
            }
            ImGui.EndPopup();
        }
    }
}
