using CurrencyTracker.Manager.Transactions;
using Dalamud.Interface;
using ImGuiNET;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public partial class Main
{
    private static void OrderColumnHeaderUI()
    {
        ImGui.BeginDisabled(selectedCurrencyID == 0 || currentTypeTransactions.Count <= 0);
        var icon = C.ReverseSort ? FontAwesomeIcon.AngleUp : FontAwesomeIcon.AngleDown;
        if (ImGuiOm.SelectableIconCentered("ReverseSort", icon))
        {
            C.ReverseSort = !C.ReverseSort;
            C.Save();

            RefreshTransactionsView();
        }
        ImGui.EndDisabled();
    }

    private static void OrderColumnCellUI(int i, bool selected, TransactionsConvertor transaction)
    {
        ImGuiOm.TextCentered(C.ReverseSort ? (currentTypeTransactions.Count - i).ToString() : (i + 1).ToString());
    }
}
