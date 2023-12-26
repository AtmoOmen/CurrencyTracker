namespace CurrencyTracker.Windows;

public partial class Main : Window, IDisposable
{
    private bool isNoteFilterEnabled = false;
    private string? searchNoteContent = string.Empty;
    private string editedNoteContent = string.Empty;

    private void NoteColumnHeaderUI()
    {
        ImGui.Selectable($" {Service.Lang.GetText("Note")}");
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            ImGui.OpenPopup("NoteSearch");
        }

        using (var popup = ImRaii.Popup("NoteSearch"))
        {
            if (popup)
            {
                ImGui.SetNextItemWidth(250);
                if (ImGui.InputTextWithHint("##NoteSearch", Service.Lang.GetText("PleaseSearch"), ref searchNoteContent, 80))
                {
                    isNoteFilterEnabled = !searchNoteContent.IsNullOrEmpty();
                    searchTimer.Restart();
                }
            }
        }
    }

    private void NoteColumnCellUI(int i, bool selected, TransactionsConvertor transaction)
    {
        ImGui.Selectable($"{transaction.Note}##_{i}");

        if (!transaction.Note.IsNullOrEmpty()) HoverTooltip(transaction.Note);

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && !ImGui.IsKeyDown(ImGuiKey.LeftCtrl))
        {
            editedNoteContent = transaction.Note;
            ImGui.OpenPopup($"EditTransactionNote##_{i}");
        }

        using (var popup = ImRaii.Popup($"EditTransactionNote##_{i}"))
        {
            if (popup)
            {
                if (!editedNoteContent.IsNullOrEmpty()) ImGui.TextWrapped(editedNoteContent);

                ImGui.SetNextItemWidth(270);
                if (ImGui.InputText($"##EditNoteContent_{i}", ref editedNoteContent, 150, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll))
                {
                    var failCount = Transactions.EditSpecificTransactions(selectedCurrencyID, new List<TransactionsConvertor> { transaction }, "None", editedNoteContent, currentView, currentViewID);

                    if (failCount == 0) searchTimer.Restart();
                    else Service.Chat.PrintError($"{Service.Lang.GetText("EditFailed")}");
                }
            }
        }
    }
}
