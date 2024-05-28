using CurrencyTracker.Manager;
using Dalamud.Interface;
using ImGuiNET;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public class OrderColumn : TableColumn
{
    public override ImGuiTableColumnFlags ColumnFlags { get; set; } = ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize;
    public override float ColumnWidthOrWeight { get; set; } = columnWidth;

    private static float columnWidth = 20;

    public override void Header()
    {
        columnWidth = ImGui.CalcTextSize((CurrentTransactions.Count + 1).ToString()).X + 10;

        ImGui.BeginDisabled(SelectedCurrencyID == 0 || CurrentTransactions.Count <= 0);

        var icon = Service.Config.ReverseSort ? FontAwesomeIcon.AngleUp : FontAwesomeIcon.AngleDown;
        if (ImGuiOm.SelectableIconCentered("ReverseSort", icon))
        {
            Service.Config.ReverseSort = !Service.Config.ReverseSort;
            Service.Config.Save();

            RefreshTable();
        }

        ImGui.EndDisabled();
    }

    public override void Cell(int i, DisplayTransaction transaction)
    {
        ImGuiOm.TextCentered(Service.Config.ReverseSort ? $"{CurrentTransactions.Count - i}" : $"{i + 1}");
    }
}
