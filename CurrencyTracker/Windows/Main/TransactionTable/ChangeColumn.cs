using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using CurrencyTracker.Manager;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public partial class Main
{
    private static bool isChangeFilterEnabled;

    private static int filterMode;
    private static int filterValue;
    private static CancellationTokenSource? colorSaveCancelTokenSource;

    private static void ChangeColumnHeaderUI()
    {
        ImGui.BeginDisabled(SelectedCurrencyID == 0 || currentTypeTransactions.Count <= 0);
        ImGuiOm.SelectableFillCell($"{Service.Lang.GetText("Change")}");
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) ImGui.OpenPopup("ChangeFunctions");
        ImGui.EndDisabled();

        using var popup = ImRaii.Popup("ChangeFunctions");
        if (popup.Success)
        {
            FilterByChangeUI();
            ColoringByChangeUI();
        }
    }

    private static void FilterByChangeUI()
    {
        if (ImGui.Checkbox($"{Service.Lang.GetText("ChangeFilterEnabled")}##ChangeFilter", ref isChangeFilterEnabled))
            RefreshTransactionsView();

        if (isChangeFilterEnabled)
        {
            if (ImGui.RadioButton($"{Service.Lang.GetText("Greater")}##FilterMode", ref filterMode, 0))
                RefreshTransactionsView();

            ImGui.SameLine();
            if (ImGui.RadioButton($"{Service.Lang.GetText("Less")}##FilterMode", ref filterMode, 1))
                RefreshTransactionsView();

            ImGui.SetNextItemWidth(130);
            if (ImGui.InputInt("##FilterValue", ref filterValue, 100, 100000, ImGuiInputTextFlags.EnterReturnsTrue))
                RefreshTransactionsView();

            ImGuiOm.HelpMarker(
                $"{Service.Lang.GetText("CurrentSettings")}:\n{Service.Lang.GetText("ChangeFilterLabel", Service.Lang.GetText(filterMode == 0 ? "Greater" : "Less"), filterValue)}");
        }
    }

    private static void ColoringByChangeUI()
    {
        var isChangeColoring = Service.Config.ChangeTextColoring;
        if (ImGui.Checkbox($"{Service.Lang.GetText("ChangeTextColoring")}##ChangeColoring", ref isChangeColoring))
        {
            Service.Config.ChangeTextColoring = isChangeColoring;
            Service.Config.Save();
        }

        if (Service.Config.ChangeTextColoring)
        {
            var positiveChangeColor = Service.Config.PositiveChangeColor;
            var negativeChangeColor = Service.Config.NegativeChangeColor;

            ColoringByChangeHandler("PositiveColor", Service.Lang.GetText("PositiveChange"), ref positiveChangeColor,
                                    color => Service.Config.PositiveChangeColor = color);
            ImGui.SameLine();
            ColoringByChangeHandler("NegativeColor", Service.Lang.GetText("NegativeChange"), ref negativeChangeColor,
                                    color => Service.Config.NegativeChangeColor = color);
        }
    }

    private static void ColoringByChangeHandler(
        string popupId, string text, ref Vector4 color, Action<Vector4> saveColorAction)
    {
        if (ImGui.ColorButton($"##{popupId}", color)) ImGui.OpenPopup(popupId);

        ImGui.SameLine();
        ImGui.Text(text);

        using var popup = ImRaii.Popup(popupId);
        if (popup.Success)
        {
            if (ImGui.ColorPicker4("", ref color))
            {
                Service.Config.ChangeTextColoring = true;
                saveColorAction(color);
                DelayedColorSave();
            }
        }
    }

    private static void DelayedColorSave()
    {
        colorSaveCancelTokenSource?.Cancel();
        colorSaveCancelTokenSource = new CancellationTokenSource();
        var token = colorSaveCancelTokenSource.Token;

        Task.Delay(TimeSpan.FromSeconds(1), token).ContinueWith(t =>
        {
            if (!t.IsCanceled) Service.Config.Save();
        }, token);
    }

    private static void ChangeColumnCellUI(int i, DisplayTransaction transaction)
    {
        var textColor = Service.Config.ChangeTextColoring
                            ? transaction.Transaction.Change > 0 ? Service.Config.PositiveChangeColor :
                              transaction.Transaction.Change < 0 ? Service.Config.NegativeChangeColor :
                              new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
                            : new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

        using (ImRaii.PushColor(ImGuiCol.Text, textColor))
        {
            var text = transaction.Transaction.Change.ToString("+ #,##0;- #,##0;0");
            ImGui.Selectable($"{text}##{i}");
            ImGuiOm.ClickToCopy(text, ImGuiMouseButton.Right, null, ImGuiKey.LeftCtrl);
        }
    }
}
