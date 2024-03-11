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
        var icon = Service.Config.ReverseSort ? FontAwesomeIcon.AngleUp : FontAwesomeIcon.AngleDown;
        if (ImGuiOm.SelectableIconCentered("ReverseSort", icon))
        {
            Service.Config.ReverseSort = !Service.Config.ReverseSort;
            Service.Config.Save();

            RefreshTransactionsView();
        }
        ImGui.EndDisabled();
    }

    private static void OrderColumnCellUI(int i, bool selected, TransactionsConvertor transaction)
    {
        ImGuiOm.TextCentered(Service.Config.ReverseSort ? (currentTypeTransactions.Count - i).ToString() : (i + 1).ToString());
    }
}
