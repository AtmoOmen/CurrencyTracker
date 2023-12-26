namespace CurrencyTracker.Windows;

public partial class Main : Window, IDisposable
{
    private void OrderColumnHeaderUI()
    {
        ImGui.SetCursorPosX(1f);
        if (SelectableIconButton(C.ReverseSort ? FontAwesomeIcon.AngleUp : FontAwesomeIcon.AngleDown, "", "OrderControl", new Vector2(ImGui.GetContentRegionAvail().X + 10f, 20.0f * ImGuiHelpers.GlobalScale)))
        {
            C.ReverseSort = !C.ReverseSort;
            C.Save();

            searchTimer.Restart();
        }
    }

    private void OrderColumnCellUI(int i, bool selected, TransactionsConvertor transaction)
    {
        var displayText = C.ReverseSort ? (currentTypeTransactions.Count - i).ToString() : (i + 1).ToString();
        ImGui.SetCursorPosX(SetColumnCenterAligned(displayText, 0, 8));
        ImGui.Text(displayText);
    }
}
