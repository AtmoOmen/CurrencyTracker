namespace CurrencyTracker.Windows;

public partial class Main : Window, IDisposable
{
    private void AmountColumnHeaderUI() => ImGui.Text($" {Service.Lang.GetText("Amount")}");

    private void AmountColumnCellUI(int i, bool selected, TransactionsConvertor transaction)
    {
        var text = transaction.Amount.ToString("#,##0");
        ImGui.Selectable($"{text}##{i}");
        ImGuiOm.ClickToCopy(text, ImGuiMouseButton.Right, null, ImGuiKey.LeftCtrl);
    }
}
