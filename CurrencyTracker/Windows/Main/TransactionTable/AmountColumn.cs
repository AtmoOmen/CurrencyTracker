using CurrencyTracker.Manager.Transactions;
using ImGuiNET;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public partial class Main
{
    private static void AmountColumnHeaderUI()
    {
        ImGui.BeginDisabled(_selectedCurrencyID == 0 || currentTypeTransactions.Count <= 0);
        ImGuiOm.Text(Service.Lang.GetText("Amount"));
        ImGui.EndDisabled();
    }

    private static void AmountColumnCellUI(int i, bool selected, TransactionsConvertor transaction)
    {
        var text = transaction.Amount.ToString("#,##0");
        ImGui.Selectable($"{text}##{i}");
        ImGuiOm.ClickToCopy(text, ImGuiMouseButton.Right, null, ImGuiKey.LeftCtrl);
    }
}
