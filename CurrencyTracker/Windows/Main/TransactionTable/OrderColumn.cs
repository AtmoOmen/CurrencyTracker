using CurrencyTracker.Internal;
using CurrencyTracker.Manager;

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

        var icon = PluginConfig.Instance().ReverseSort ? FontAwesomeIcon.AngleUp : FontAwesomeIcon.AngleDown;

        if (ImGuiOm.SelectableIconCentered("ReverseSort", icon, false, ImGuiSelectableFlags.None, true))
        {
            PluginConfig.Instance().ReverseSort = !PluginConfig.Instance().ReverseSort;
            PluginConfig.Instance().Save();

            RefreshTable();
        }

        ImGui.EndDisabled();
    }

    public override void Cell(int i, DisplayTransaction transaction)
    {
        if (i < 0) return;
        ImGuiOm.TextCentered(PluginConfig.Instance().ReverseSort ? $"{CurrentTransactions.Count - i}" : $"{i + 1}");
    }
}
