using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Transactions;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
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
        ImGui.EndDisabled();
        if (ImGui.BeginPopupContextItem("NoteSearch"))
        {
            ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
            if (ImGui.InputTextWithHint("###NoteSearch", Service.Lang.GetText("PleaseSearch"), ref searchNoteContent, 80))
            {
                isNoteFilterEnabled = !string.IsNullOrEmpty(searchNoteContent);
                RefreshTransactionsView();
            }
            ImGui.EndPopup();
        }
    }

    private static void NoteColumnCellUI(int i, DisplayTransaction transaction)
    {
        ImGui.Selectable($"{transaction.Transaction.Note}##_{i}");

        if (!string.IsNullOrEmpty(transaction.Transaction.Note)) ImGuiOm.TooltipHover(transaction.Transaction.Note);

        if (!ImGui.IsKeyDown(ImGuiKey.LeftCtrl) && !transaction.Selected  && ImGui.BeginPopupContextItem($"NoteEditPopup{i}"))
        {
            if (ImGui.IsWindowAppearing())
                editedNoteContent = transaction.Transaction.Note;

            if (!string.IsNullOrEmpty(editedNoteContent)) ImGui.TextWrapped(editedNoteContent);

            ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
            if (ImGui.InputText($"##EditNoteContent_{i}", ref editedNoteContent, 150, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll))
            {
                var failCount = TransactionsHandler.EditSpecificTransactions(SelectedCurrencyID, [transaction.Transaction], "None", editedNoteContent, currentView, currentViewID);

                if (failCount == 0) 
                    RefreshTransactionsView();
                else Service.Chat.PrintError($"{Service.Lang.GetText("EditFailed")}");
            }

            ImGui.EndPopup();
        }
    }
}
