namespace CurrencyTracker.Windows;

public partial class Main
{
    private void OrderColumnHeaderUI()
    {
        var icon = C.ReverseSort ? FontAwesomeIcon.AngleUp : FontAwesomeIcon.AngleDown;
        if (ImGuiOm.SelectableIconCentered("ReverseSort", icon))
        {
            C.ReverseSort = !C.ReverseSort;
            C.Save();

            searchTimer.Restart();
        }
    }

    private void OrderColumnCellUI(int i, bool selected, TransactionsConvertor transaction)
    {
        ImGuiOm.TextCentered(i.ToString(), C.ReverseSort ? (currentTypeTransactions.Count - i).ToString() : (i + 1).ToString());
    }
}
