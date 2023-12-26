namespace CurrencyTracker.Windows;

public partial class Main : Window, IDisposable
{
    private void AmountColumnHeaderUI() => ImGui.Text($" {Service.Lang.GetText("Amount")}");

    private void AmountColumnCellUI(int i, bool selected, TransactionsConvertor transaction) => SelectableClickToCopy(transaction.Amount.ToString("#,##0"), null, i);
}
