namespace CurrencyTracker.Windows;

public partial class Main
{
    private void OrderColumnHeaderUI()
    {
        ImGui.BeginDisabled(selectedCurrencyID == 0 || currentTypeTransactions.Count <= 0);
        var icon = C.ReverseSort ? FontAwesomeIcon.AngleUp : FontAwesomeIcon.AngleDown;
        if (ImGuiOm.SelectableIconCentered("ReverseSort", icon))
        {
            C.ReverseSort = !C.ReverseSort;
            C.Save();

            searchTimer.Restart();
        }
        ImGui.EndDisabled();
    }

    private void OrderColumnCellUI(int i, bool selected, TransactionsConvertor transaction)
    {
        ImGuiOm.TextCentered(C.ReverseSort ? (currentTypeTransactions.Count - i).ToString() : (i + 1).ToString());
    }
}
