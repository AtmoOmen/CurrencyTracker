namespace CurrencyTracker.Windows;

public partial class Main : Window, IDisposable
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
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ObjectGroup, Service.Lang.GetText("Merge"))) 
        {
            if (selectedCurrencyID == 0)
            {
                Service.Chat.PrintError(Service.Lang.GetText("TransactionsHelp1"));
                return;
            }

            ImGui.OpenPopup("MergeTransactions");
        } 

        using (var popup = ImRaii.Popup("MergeTransactions"))
        {
            if (popup)
            {
                ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("ManualMerge"));

                ImGui.Text(Service.Lang.GetText("Threshold"));

                ImGui.SameLine();
                ImGui.SetNextItemWidth(150f);
                if (ImGui.InputInt("##MergeThreshold", ref mergeThreshold, 100, 100, ImGuiInputTextFlags.EnterReturnsTrue)) mergeThreshold = Math.Max(0, mergeThreshold);

                ImGui.SameLine();
                HelpMarker($"{Service.Lang.GetText("MergeTransactionsHelp3")}");

                if (ImGui.Button(Service.Lang.GetText("TwoWayMerge"))) MergeTransactionHandler(false);

                ImGui.SameLine();
                if (ImGui.Button(Service.Lang.GetText("OneWayMerge"))) MergeTransactionHandler(true);
            }
        }
    }

    private int MergeTransactionHandler(bool oneWay)
    {
        var threshold = (mergeThreshold == 0) ? int.MaxValue : mergeThreshold;
        var mergeCount = Transactions.MergeTransactionsByLocationAndThreshold(selectedCurrencyID, threshold, oneWay);

        if (mergeCount > 0)
            Service.Chat.Print($"{Service.Lang.GetText("MergeTransactionsHelp1", mergeCount)}");
        else
            Service.Chat.PrintError(Service.Lang.GetText("TransactionsHelp"));

        UpdateTransactions(selectedCurrencyID, currentView, currentViewID);
        return mergeCount;
    }

    private void ExportDataUI()
    {
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.FileExport, Service.Lang.GetText("Export")))
        {
            if (selectedCurrencyID == 0)
            {
                Service.Chat.PrintError(Service.Lang.GetText("TransactionsHelp1"));
                return;
            }

            ImGui.OpenPopup("ExportFileRename");
        }

        using (var popup = ImRaii.Popup("ExportFileRename"))
        {
            if (popup)
            {
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("ExportFileType")}:");

                var exportDataFileType = C.ExportDataFileType;
                ImGui.SameLine();
                if (ImGui.RadioButton(".csv", ref exportDataFileType, 0))
                {
                    C.ExportDataFileType = 0;
                    C.Save();
                }

                ImGui.SameLine();
                if (ImGui.RadioButton(".md", ref exportDataFileType, 1))
                {
                    C.ExportDataFileType = 1;
                    C.Save();
                }

                ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("FileRenameLabel") + $"({Service.Lang.GetText("PressEnterToConfirm")})");

                ImGui.SetNextItemWidth(200f);
                if (ImGui.InputText($"_{C.AllCurrencies[selectedCurrencyID]}_{Service.Lang.GetText("FileRenameLabel2")}{(exportDataFileType == 0 ? ".csv" : ".md")}", ref exportFileName, 64, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    if (currentTypeTransactions == null || currentTypeTransactions.Count == 0) return;

                    Service.Chat.Print($"{Service.Lang.GetText("ExportFileMessage")} {Transactions.ExportData(currentTypeTransactions, exportFileName, selectedCurrencyID, exportDataFileType)}");
                }

                HoverTooltip($"{Service.Lang.GetText("FileRenameHelp1")} {C.AllCurrencies[selectedCurrencyID]}_{Service.Lang.GetText("FileRenameLabel2")}.csv");
            }
        }
    }
}
