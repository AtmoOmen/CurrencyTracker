using CurrencyTracker.Manager;
using Dalamud.Interface;
using Dalamud.Bindings.ImGui;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public class OrderColumn : TableColumn
{
    public override ImGuiTableColumnFlags ColumnFlags { get; protected set; } = 
        ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize;

    public override float ColumnWidthOrWeight { get; protected set; }

    public override void Header()
    {
        ColumnWidthOrWeight = ImGui.CalcTextSize($"{CurrentTransactions.Count}11").X;

        ImGui.BeginDisabled(SelectedCurrencyID == 0 || CurrentTransactions.Count <= 0);

        var icon = Service.Config.ReverseSort ? FontAwesomeIcon.AngleUp : FontAwesomeIcon.AngleDown;
        if (ImGuiOm.SelectableIconCentered("ReverseSort", icon, false, ImGuiSelectableFlags.None, true))
        {
            Service.Config.ReverseSort = !Service.Config.ReverseSort;
            Service.Config.Save();

            RefreshTable();
        }

        ImGui.EndDisabled();
    }

    public override void Cell(int i, DisplayTransaction transaction)
    {
        if (i < 0) return;
        ImGuiOm.TextCentered(Service.Config.ReverseSort ? $"{CurrentTransactions.Count - i}" : $"{i + 1}");
    }
}
