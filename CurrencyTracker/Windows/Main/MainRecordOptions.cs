namespace CurrencyTracker.Windows
{
    // 合并记录 / 清除异常记录 / 导出记录
    public partial class Main : Window, IDisposable
    {
        private void RecordOptionsUI()
        {
            RecordSettingsUI();
            ImGui.SameLine();
            MergeTransactionUI();
            ImGui.SameLine();
            ClearExceptionUI();
            ImGui.SameLine();
            ExportDataUI();
        }

        // 记录设置界面 Record Settings
        private void RecordSettingsUI()
        {
            if (ImGui.Button(Service.Lang.GetText("RecordSettings") + "[DEV]"))
            {
                P.RecordSettings.IsOpen = !P.RecordSettings.IsOpen;
            }
        }

        // 按临界值合并记录界面 Merge Transactions By Threshold
        private void MergeTransactionUI()
        {
            if (ImGui.Button(Service.Lang.GetText("MergeTransactionsLabel")))
            {
                ImGui.OpenPopup("MergeTransactions");
            }

            if (ImGui.BeginPopup("MergeTransactions"))
            {
                ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("MergeTransactionsLabel4"));
                ImGui.Text(Service.Lang.GetText("Threshold"));
                ImGui.SameLine();
                ImGui.SetNextItemWidth(150f);
                if (ImGui.InputInt("##MergeThreshold", ref mergeThreshold, 100, 100, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    mergeThreshold = Math.Max(0, mergeThreshold);
                }
                ImGui.SameLine();
                ImGuiComponents.HelpMarker($"{Service.Lang.GetText("MergeTransactionsHelp3")}{Service.Lang.GetText("TransactionsHelp2")}");

                // 双向合并 Two-Way Merge
                if (ImGui.Button(Service.Lang.GetText("TwoWayMerge")))
                {
                    var mergeCount = MergeTransactionHandler(false);
                    if (mergeCount == 0)
                        return;
                }

                ImGui.SameLine();

                // 单向合并 One-Way Merge
                if (ImGui.Button(Service.Lang.GetText("OneWayMerge")))
                {
                    var mergeCount = MergeTransactionHandler(true);
                    if (mergeCount == 0)
                        return;
                }
                ImGui.EndPopup();
            }
        }

        // 合并交易记录 Simplified merging transactions code
        private int MergeTransactionHandler(bool oneWay)
        {
            if (!C.AllCurrencies.ContainsKey(selectedCurrencyID))
            {
                Service.Chat.PrintError(Service.Lang.GetText("TransactionsHelp1"));
                return 0;
            }

            var threshold = (mergeThreshold == 0) ? int.MaxValue : mergeThreshold;
            var mergeCount = Transactions.MergeTransactionsByLocationAndThreshold(selectedCurrencyID, threshold, oneWay);

            if (mergeCount > 0)
                Service.Chat.Print($"{Service.Lang.GetText("MergeTransactionsHelp1", mergeCount)}");
            else
                Service.Chat.PrintError(Service.Lang.GetText("TransactionsHelp"));

            UpdateTransactions();
            return mergeCount;
        }

        // 清除异常记录界面 Clear Exceptional Transactions
        private void ClearExceptionUI()
        {
            if (ImGui.Button(Service.Lang.GetText("ClearExTransactionsLabel")))
            {
                ImGui.OpenPopup("ClearExceptionNote");
            }

            if (ImGui.BeginPopup("ClearExceptionNote"))
            {
                ImGui.Text($"{Service.Lang.GetText("ClearExTransactionsHelp")}{Service.Lang.GetText("ClearExTransactionsHelp1")}\n{Service.Lang.GetText("TransactionsHelp2")}");

                if (ImGui.Button(Service.Lang.GetText("Confirm")))
                {
                    if (selectedCurrencyID == 0)
                    {
                        Service.Chat.PrintError(Service.Lang.GetText("TransactionsHelp1"));
                        return;
                    }

                    var removedCount = Transactions.ClearExceptionRecords(selectedCurrencyID);
                    if (removedCount > 0)
                    {
                        Service.Chat.Print($"{Service.Lang.GetText("ClearExTransactionsHelp2", removedCount)}");
                        UpdateTransactions();
                    }
                    else
                    {
                        Service.Chat.PrintError(Service.Lang.GetText("TransactionsHelp"));
                    }
                }
                ImGui.EndPopup();
            }
        }

        // 导出数据界面 Export Transactions
        private void ExportDataUI()
        {
            if (ImGui.Button(Service.Lang.GetText("Export")))
            {
                if (selectedCurrencyID == 0)
                {
                    Service.Chat.PrintError(Service.Lang.GetText("TransactionsHelp1"));
                    return;
                }

                ImGui.OpenPopup("ExportFileRename");
            }

            if (ImGui.BeginPopup("ExportFileRename"))
            {
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("ExportFileType")}:");
                ImGui.SameLine();

                var exportDataFileType = C.ExportDataFileType;
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

                ImGui.SameLine();
                ImGuiComponents.HelpMarker(Service.Lang.GetText("ExportFileHelp"));

                ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("FileRenameLabel"));
                ImGui.SameLine();
                ImGuiComponents.HelpMarker(Service.Lang.GetText("ExportFileHelp1"));

                ImGui.SetNextItemWidth(200);
                if (ImGui.InputText($"_{C.AllCurrencies[selectedCurrencyID]}_{Service.Lang.GetText("FileRenameLabel2")}{(exportDataFileType == 0 ? ".csv" : ".md")}", ref fileName, 64, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    if (currentTypeTransactions == null || currentTypeTransactions.Count == 0)
                    {
                        Service.Chat.PrintError(Service.Lang.GetText("ExportCsvMessage1"));
                        return;
                    }

                    var filePath = Transactions.ExportData(currentTypeTransactions, fileName, selectedCurrencyID, exportDataFileType);
                    Service.Chat.Print($"{Service.Lang.GetText("ExportCsvMessage3")}{filePath}");
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip($"{Service.Lang.GetText("FileRenameHelp1")} {C.AllCurrencies[selectedCurrencyID]}_{Service.Lang.GetText("FileRenameLabel2")}.csv");
                }
                ImGui.EndPopup();
            }
        }
    }
}
