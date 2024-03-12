using System;
using CurrencyTracker.Manager.Transactions;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public partial class Main
{
    private string exportFileName = string.Empty;
    private int mergeThreshold;

    private void RecordOptionsUI()
    {
        RecordSettingsUI();
        ImGui.SameLine();
        MergeTransactionUI();
        ImGui.SameLine();
        ExportDataUI();
        ImGui.SameLine();
        BackupUI();
    }

    private void RecordSettingsUI()
    {
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Cog, Service.Lang.GetText("Settings"))) P.Settings.IsOpen = !P.Settings.IsOpen;
    }

    private void MergeTransactionUI()
    {
        ImGui.BeginDisabled(_selectedCurrencyID == 0 || currentTypeTransactions.Count <= 1);
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ObjectGroup, Service.Lang.GetText("Merge"))) ImGui.OpenPopup("MergeTransactions");
        ImGui.EndDisabled();

        using (var popup = ImRaii.Popup("MergeTransactions"))
        {
            if (popup.Success)
            {
                ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("ManualMerge"));

                ImGui.Text(Service.Lang.GetText("Threshold"));

                ImGui.SameLine();
                ImGui.SetNextItemWidth(150f);
                if (ImGui.InputInt("##MergeThreshold", ref mergeThreshold, 100, 100, ImGuiInputTextFlags.EnterReturnsTrue)) mergeThreshold = Math.Max(0, mergeThreshold);

                ImGui.SameLine();
                ImGuiOm.HelpMarker($"{Service.Lang.GetText("MergeTransactionsHelp3")}");

                if (ImGui.Button(Service.Lang.GetText("TwoWayMerge"))) MergeTransactionHandler(false);

                ImGui.SameLine();
                if (ImGui.Button(Service.Lang.GetText("OneWayMerge"))) MergeTransactionHandler(true);
            }
        }
    }

    private void MergeTransactionHandler(bool oneWay)
    {
        var threshold = (mergeThreshold == 0) ? int.MaxValue : mergeThreshold;
        var mergeCount = TransactionsHandler.MergeTransactionsByLocationAndThreshold(_selectedCurrencyID, threshold, oneWay);

        if (mergeCount > 0)
            Service.Chat.Print($"{Service.Lang.GetText("MergeTransactionsHelp1", mergeCount)}");
        else
            Service.Chat.PrintError(Service.Lang.GetText("TransactionsHelp"));

        UpdateTransactions(_selectedCurrencyID, currentView, currentViewID);
    }

    private void ExportDataUI()
    {
        ImGui.BeginDisabled(_selectedCurrencyID == 0 || currentTypeTransactions.Count <= 0);
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.FileExport, Service.Lang.GetText("Export"))) ImGui.OpenPopup("ExportFileRename");
        ImGui.EndDisabled();

        using var popup = ImRaii.Popup("ExportFileRename");
        if (popup.Success)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("ExportFileType")}:");

            var exportDataFileType = Service.Config.ExportDataFileType;
            ImGui.SameLine();
            if (ImGui.RadioButton(".csv", ref exportDataFileType, 0))
            {
                Service.Config.ExportDataFileType = 0;
                Service.Config.Save();
            }

            ImGui.SameLine();
            if (ImGui.RadioButton(".md", ref exportDataFileType, 1))
            {
                Service.Config.ExportDataFileType = 1;
                Service.Config.Save();
            }

            var nowTime = DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss");

            ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("FileRenameLabel") + $"({Service.Lang.GetText("PressEnterToConfirm")})");

            ImGui.SetNextItemWidth(200f);
            if (ImGui.InputText("###ExportFileNameInput", ref exportFileName, 64, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                if (currentTypeTransactions.Count == 0) return;

                Service.Chat.Print($"{Service.Lang.GetText("ExportFileMessage")} {TransactionsHandler.ExportData(currentTypeTransactions, exportFileName, _selectedCurrencyID, exportDataFileType)}");
            }
            ImGuiOm.TooltipHover($"{Service.Lang.GetText("FileRenameHelp1")} {Service.Config.AllCurrencies[_selectedCurrencyID]}_{nowTime}.csv");

            ImGui.SameLine();
            ImGui.Text($"_{Service.Config.AllCurrencies[_selectedCurrencyID]}_{nowTime}{(exportDataFileType == 0 ? ".csv" : ".md")}");
        }
    }
}
