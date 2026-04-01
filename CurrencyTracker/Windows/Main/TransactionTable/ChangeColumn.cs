using System;
using System.Numerics;
using CurrencyTracker.Internal;
using CurrencyTracker.Manager;
using Dalamud.Interface.Utility;

namespace CurrencyTracker.Windows;

public class ChangeColumn : TableColumn
{
    internal static bool IsChangeFilterEnabled;
    internal static int  FilterMode;
    internal static int  FilterValue;

    public override void Header()
    {
        ImGui.BeginDisabled(SelectedCurrencyID == 0 || CurrentTransactions.Count <= 0);
        ImGuiOm.SelectableFillCell($"{Service.Lang.GetText("Change")}");
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) ImGui.OpenPopup("ChangeFunctions");
        ImGui.EndDisabled();

        if (ImGui.BeginPopupContextItem("ChangeColumnHeaderFunctions"))
        {
            FilterByChangeUI();
            ColoringByChangeUI();
            ImGui.EndPopup();
        }
    }

    public override void Cell(int i, DisplayTransaction transaction)
    {
        if (i < 0) return;
        var textColor = PluginConfig.Instance().ChangeTextColoring
                            ? transaction.Transaction.Change > 0 ? PluginConfig.Instance().PositiveChangeColor :
                              transaction.Transaction.Change < 0 ? PluginConfig.Instance().NegativeChangeColor :
                                                                   new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
                            : new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

        using (ImRaii.PushColor(ImGuiCol.Text, textColor))
        {
            var text = transaction.Transaction.Change.ToString("+ #,##0;- #,##0;0");
            ImGui.Selectable($"{text}##{i}");
            if (!transaction.Selected) ImGuiOm.ClickToCopy(text, ImGuiMouseButton.Right, null, ImGuiKey.LeftCtrl);
        }
    }

    private static void FilterByChangeUI()
    {
        if (ImGui.Checkbox($"{Service.Lang.GetText("ChangeFilterEnabled")}##ChangeFilter", ref IsChangeFilterEnabled))
            RefreshTable();

        if (IsChangeFilterEnabled)
        {
            if (ImGui.RadioButton($"{Service.Lang.GetText("Greater")}##FilterMode", ref FilterMode, 0))
                RefreshTable();

            ImGui.SameLine();
            if (ImGui.RadioButton($"{Service.Lang.GetText("Less")}##FilterMode", ref FilterMode, 1))
                RefreshTable();

            ImGui.SetNextItemWidth(100f * ImGuiHelpers.GlobalScale);
            if (ImGui.InputInt("##FilterValue", ref FilterValue, 100, 100000, flags: ImGuiInputTextFlags.EnterReturnsTrue))
                RefreshTable();

            ImGuiOm.HelpMarker
            (
                $"{Service.Lang.GetText("CurrentSettings")}:\n{Service.Lang.GetText("ChangeFilterLabel", Service.Lang.GetText(FilterMode == 0 ? "Greater" : "Less"), FilterValue)}"
            );
        }
    }

    private static void ColoringByChangeUI()
    {
        var isChangeColoring = PluginConfig.Instance().ChangeTextColoring;

        if (ImGui.Checkbox($"{Service.Lang.GetText("ChangeTextColoring")}##ChangeColoring", ref isChangeColoring))
        {
            PluginConfig.Instance().ChangeTextColoring = isChangeColoring;
            PluginConfig.Instance().Save();
        }

        if (PluginConfig.Instance().ChangeTextColoring)
        {
            var positiveChangeColor = PluginConfig.Instance().PositiveChangeColor;
            var negativeChangeColor = PluginConfig.Instance().NegativeChangeColor;

            ColoringByChangeHandler
            (
                "PositiveColor",
                Service.Lang.GetText("PositiveChange"),
                ref positiveChangeColor,
                color => PluginConfig.Instance().PositiveChangeColor = color
            );

            ImGui.SameLine();
            ColoringByChangeHandler
            (
                "NegativeColor",
                Service.Lang.GetText("NegativeChange"),
                ref negativeChangeColor,
                color => PluginConfig.Instance().NegativeChangeColor = color
            );
        }
    }

    private static void ColoringByChangeHandler
    (
        string          popupId,
        string          text,
        ref Vector4     color,
        Action<Vector4> saveColorAction
    )
    {
        if (ImGui.ColorButton($"##{popupId}", color)) ImGui.OpenPopup(popupId);

        ImGui.SameLine();
        ImGui.Text(text);

        using var popup = ImRaii.Popup(popupId);
        if (!popup.Success) return;

        ImGui.ColorPicker4($"###{popupId}{text}", ref color);

        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            PluginConfig.Instance().ChangeTextColoring = true;
            saveColorAction.Invoke(color);
            PluginConfig.Instance().Save();
        }
    }
}
