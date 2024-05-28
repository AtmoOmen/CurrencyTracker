using CurrencyTracker.Manager;
using ImGuiNET;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public class AmountColumn : TableColumn
{
    public override void Header()
    {
        ImGui.BeginDisabled(SelectedCurrencyID == 0 || CurrentTransactions.Count <= 0);
        ImGuiOm.Text(Service.Lang.GetText("Amount"));
        ImGui.EndDisabled();
    }

    public override void Cell(int i, DisplayTransaction transaction)
    {
        var text = transaction.Transaction.Amount.ToString("#,##0");
        ImGui.Selectable($"{text}##{i}");
        if (!transaction.Selected) ImGuiOm.ClickToCopy(text, ImGuiMouseButton.Right, null, ImGuiKey.LeftCtrl);
    }
}
