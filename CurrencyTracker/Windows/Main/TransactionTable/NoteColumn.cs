using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Transactions;
using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public class NoteColumn : TableColumn
{
    internal static bool IsNoteFilterEnabled;
    internal static string? SearchNoteContent = string.Empty;
    internal static string editedNoteContent = string.Empty;

    public override void Header()
    {
        ImGui.BeginDisabled(SelectedCurrencyID == 0 || CurrentTransactions.Count <= 0);
        ImGuiOm.SelectableFillCell(Service.Lang.GetText("Note"));
        ImGui.EndDisabled();

        if (ImGui.BeginPopupContextItem("NoteSearch"))
        {
            ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
            ImGui.InputTextWithHint("###NoteSearch", Service.Lang.GetText("PleaseSearch"), ref SearchNoteContent, 128);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                IsNoteFilterEnabled = !string.IsNullOrEmpty(SearchNoteContent);
                RefreshTable();
            }
            ImGui.EndPopup();
        }
    }

    public override void Cell(int i, DisplayTransaction transaction)
    {
        if (i < 0) return;
        ImGui.Selectable($"{transaction.Transaction.Note}##_{i}");

        if (!string.IsNullOrEmpty(transaction.Transaction.Note)) 
            ImGuiOm.TooltipHover(transaction.Transaction.Note);

        if (!ImGui.IsKeyDown(ImGuiKey.LeftCtrl) && !transaction.Selected && ImGui.BeginPopupContextItem($"NoteEditPopup{i}"))
        {
            if (ImGui.IsWindowAppearing())
                editedNoteContent = transaction.Transaction.Note;

            if (!string.IsNullOrEmpty(editedNoteContent)) ImGui.TextWrapped(editedNoteContent);

            ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
            if (ImGui.InputText($"##EditNoteContent_{i}", ref editedNoteContent, 150, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll))
            {
                var failCount = TransactionsHandler.EditSpecificTransactions(SelectedCurrencyID, [transaction.Transaction], "None", editedNoteContent, CurrentView, CurrentViewID);

                if (failCount == 0) RefreshTable();
                else DService.Chat.PrintError($"{Service.Lang.GetText("EditFailed")}");
            }

            ImGui.EndPopup();
        }
    }
}
