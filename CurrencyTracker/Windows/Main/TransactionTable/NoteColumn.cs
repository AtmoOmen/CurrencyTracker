using System.Collections.Generic;
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
        ImGui.BeginDisabled(_selectedCurrencyID == 0 || currentTypeTransactions.Count <= 0);
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

    private static void NoteColumnCellUI(int i, bool selected, TransactionsConvertor transaction)
    {
        ImGui.Selectable($"{transaction.Note}##_{i}");

        if (!string.IsNullOrEmpty(transaction.Note)) ImGuiOm.TooltipHover(transaction.Note);

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && !ImGui.IsKeyDown(ImGuiKey.LeftCtrl))
        {
            editedNoteContent = transaction.Note;
            ImGui.OpenPopup($"EditTransactionNote##_{i}");
        }

        using var popup = ImRaii.Popup($"EditTransactionNote##_{i}");
        if (popup.Success)
        {
            if (!string.IsNullOrEmpty(editedNoteContent)) ImGui.TextWrapped(editedNoteContent);

            ImGui.SetNextItemWidth(270);
            if (ImGui.InputText($"##EditNoteContent_{i}", ref editedNoteContent, 150, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll))
            {
                var failCount = TransactionsHandler.EditSpecificTransactions(_selectedCurrencyID, new List<TransactionsConvertor> { transaction }, "None", editedNoteContent, currentView, currentViewID);

                if (failCount == 0) 
                    RefreshTransactionsView();
                else Service.Chat.PrintError($"{Service.Lang.GetText("EditFailed")}");
            }
        }
    }
}
