using System.Collections.Generic;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Transactions;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using OmenTools.Helpers;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public partial class Main
{
    private static bool isNoteFilterEnabled;
    private static string? searchNoteContent = string.Empty;
    private static string editedNoteContent = string.Empty;

    private static void NoteColumnHeaderUI()
    {
        ImGui.BeginDisabled(SelectedCurrencyID == 0 || currentTypeTransactions.Count <= 0);
        ImGuiOm.SelectableFillCell(Service.Lang.GetText("Note"));
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            ImGui.OpenPopup("NoteSearch");
        }
        ImGui.EndDisabled();

        using var popup = ImRaii.Popup("NoteSearch");
        if (popup.Success)
        {
            ImGui.SetNextItemWidth(250);
            if (ImGui.InputTextWithHint("##NoteSearch", Service.Lang.GetText("PleaseSearch"), ref searchNoteContent, 80))
            {
                isNoteFilterEnabled = !string.IsNullOrEmpty(searchNoteContent);
                RefreshTransactionsView();
            }
        }
    }

    private static void NoteColumnCellUI(int i, DisplayTransaction transaction)
    {
        ImGui.Selectable($"{transaction.Transaction.Note}##_{i}");

        if (!string.IsNullOrEmpty(transaction.Transaction.Note)) ImGuiOm.TooltipHover(transaction.Transaction.Note);

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && !ImGui.IsKeyDown(ImGuiKey.LeftCtrl))
        {
            editedNoteContent = transaction.Transaction.Note;
            ImGui.OpenPopup($"EditTransactionNote##_{i}");
        }

        using var popup = ImRaii.Popup($"EditTransactionNote##_{i}");
        if (popup.Success)
        {
            if (!string.IsNullOrEmpty(editedNoteContent)) ImGui.TextWrapped(editedNoteContent);

            ImGui.SetNextItemWidth(270);
            if (ImGui.InputText($"##EditNoteContent_{i}", ref editedNoteContent, 150, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll))
            {
                var failCount = TransactionsHandler.EditSpecificTransactions(SelectedCurrencyID, [transaction.Transaction], "None", editedNoteContent, currentView, currentViewID);

                if (failCount == 0) 
                    RefreshTransactionsView();
                else Service.Chat.PrintError($"{Service.Lang.GetText("EditFailed")}");
            }
        }
    }
}
