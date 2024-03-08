using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using CurrencyTracker.Manager.Transactions;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using OmenTools.Helpers;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public partial class Main
{
    private bool isChangeFilterEnabled;

    private int filterMode;
    private int filterValue;
    private CancellationTokenSource? colorSaveCancelTokenSource;

    private void ChangeColumnHeaderUI()
    {
        ImGui.BeginDisabled(selectedCurrencyID == 0 || currentTypeTransactions.Count <= 0);
        ImGuiOm.SelectableFillCell($"{Service.Lang.GetText("Change")}");
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            ImGui.OpenPopup("ChangeFunctions");
        }
        ImGui.EndDisabled();

        using var popup = ImRaii.Popup("ChangeFunctions");
        if (popup.Success)
        {
            FilterByChangeUI();
            ColoringByChangeUI();
        }
    }

    private void FilterByChangeUI()
    {
        if (ImGui.Checkbox($"{Service.Lang.GetText("ChangeFilterEnabled")}##ChangeFilter", ref isChangeFilterEnabled)) searchTimer.Restart();

        if (isChangeFilterEnabled)
        {
            if (ImGui.RadioButton($"{Service.Lang.GetText("Greater")}##FilterMode", ref filterMode, 0)) searchTimer.Restart();

            ImGui.SameLine();
            if (ImGui.RadioButton($"{Service.Lang.GetText("Less")}##FilterMode", ref filterMode, 1)) searchTimer.Restart();

            ImGui.SetNextItemWidth(130);
            if (ImGui.InputInt($"##FilterValue", ref filterValue, 100, 100000, ImGuiInputTextFlags.EnterReturnsTrue)) searchTimer.Restart();

            ImGuiOm.HelpMarker($"{Service.Lang.GetText("CurrentSettings")}:\n{Service.Lang.GetText("ChangeFilterLabel", Service.Lang.GetText(filterMode == 0 ? "Greater" : "Less"), filterValue)}");
        }
    }

    private void ColoringByChangeUI()
    {
        var isChangeColoring = C.ChangeTextColoring;
        if (ImGui.Checkbox($"{Service.Lang.GetText("ChangeTextColoring")}##ChangeColoring", ref isChangeColoring))
        {
            C.ChangeTextColoring = isChangeColoring;
            C.Save();
        }

        if (C.ChangeTextColoring)
        {
            var positiveChangeColor = C.PositiveChangeColor;
            var negativeChangeColor = C.NegativeChangeColor;

            ColoringByChangeHandler("PositiveColor", Service.Lang.GetText("PositiveChange"), ref positiveChangeColor, color => C.PositiveChangeColor = color);
            ImGui.SameLine();
            ColoringByChangeHandler("NegativeColor", Service.Lang.GetText("NegativeChange"), ref negativeChangeColor, color => C.NegativeChangeColor = color);
        }
    }

    private void ColoringByChangeHandler(string popupId, string text, ref Vector4 color, Action<Vector4> saveColorAction)
    {
        if (ImGui.ColorButton($"##{popupId}", color))
        {
            ImGui.OpenPopup(popupId);
        }

        ImGui.SameLine();
        ImGui.Text(text);

        using (var popup = ImRaii.Popup(popupId))
        {
            if (popup.Success)
            {
                if (ImGui.ColorPicker4("", ref color))
                {
                    C.ChangeTextColoring = true;
                    saveColorAction(color);
                    DelayedColorSave();
                }
            }
        }
    }

    private void DelayedColorSave()
    {
        colorSaveCancelTokenSource?.Cancel();
        colorSaveCancelTokenSource = new CancellationTokenSource();
        var token = colorSaveCancelTokenSource.Token;

        Task.Delay(TimeSpan.FromSeconds(1), token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                C.Save();
            }
        }, token);
    }

    private void ChangeColumnCellUI(int i, bool selected, TransactionsConvertor transaction)
    {
        var textColor = C.ChangeTextColoring
            ? transaction.Change > 0 ? C.PositiveChangeColor : transaction.Change < 0 ? C.NegativeChangeColor : new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
            : new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

        using (ImRaii.PushColor(ImGuiCol.Text, textColor))
        {
            var text = transaction.Change.ToString("+ #,##0;- #,##0;0");
            ImGui.Selectable($"{text}##{i}");
            ImGuiOm.ClickToCopy(text, ImGuiMouseButton.Right, null, ImGuiKey.LeftCtrl);
        }
    }
}
