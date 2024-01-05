namespace CurrencyTracker.Windows;

public partial class Main
{
    private void AmountColumnHeaderUI() => ImGuiOm.Text(Service.Lang.GetText("Amount"));

    private void AmountColumnCellUI(int i, bool selected, TransactionsConvertor transaction)
    {
        var text = transaction.Amount.ToString("#,##0");
        ImGui.Selectable($"{text}##{i}");
        ImGuiOm.ClickToCopy(text, ImGuiMouseButton.Right, null, ImGuiKey.LeftCtrl);
    }
}
