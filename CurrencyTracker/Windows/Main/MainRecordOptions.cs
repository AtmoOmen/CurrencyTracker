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
            ImGui.SameLine();
            BackupUI();
        }

        // 记录设置界面 Record Settings
        private void RecordSettingsUI()
        {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Cog, Service.Lang.GetText("RecordSettings") + "[DEV]"))
            {
                P.RecordSettings.IsOpen = !P.RecordSettings.IsOpen;
            }
        }

        // 按临界值合并记录界面 Merge Transactions By Threshold
        private void MergeTransactionUI()
        {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ObjectGroup, Service.Lang.GetText("MergeTransactionsLabel")))
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
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ExclamationCircle, Service.Lang.GetText("ClearExTransactionsLabel")))
            {
                ImGui.OpenPopup("ClearExceptionNote");
            }

            if (ImGui.BeginPopup("ClearExceptionNote"))
            {
                ImGui.Text($"{Service.Lang.GetText("ClearExTransactionsHelp")}{Service.Lang.GetText("ClearExTransactionsHelp1")}\n{Service.Lang.GetText("TransactionsHelp2")}");

                ImGui.Separator();

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
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.FileExport, Service.Lang.GetText("Export")))
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

        // 备份数据界面 Backup Interface
        private void BackupUI()
        {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Save, Service.Lang.GetText("Backup")))
            {
                ImGui.OpenPopup("BackupUI");
            }

            if (ImGui.BeginPopup("BackupUI"))
            {
                ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("ManualBackup"));
                ImGui.Separator();

                if (ImGui.Button($"{Service.Lang.GetText("BackupCurrentCharacter")}"))
                {
                    var filePath = BackupHandler(P.PlayerDataFolder);
                    Service.Chat.Print(Service.Lang.GetText("BackupHelp4", filePath));
                }

                ImGui.SameLine();
                if (ImGui.Button($"{Service.Lang.GetText("BackupAllCharacter")}"))
                {
                    var failCharacters = C.CurrentActiveCharacter
                        .Where(character => BackupHandler(Path.Join(P.PluginInterface.ConfigDirectory.FullName, $"{character.Name}_{character.Server}")).IsNullOrEmpty())
                        .Select(character => $"{character.Name}@{character.Server}")
                        .ToList();

                    var successCount = C.CurrentActiveCharacter.Count - failCharacters.Count;
                    Service.Chat.Print(Service.Lang.GetText("BackupHelp1", successCount) + (failCharacters.Any() ? Service.Lang.GetText("BackupHelp2", failCharacters.Count) : ""));

                    if (failCharacters.Any())
                    {
                        Service.Chat.PrintError(Service.Lang.GetText("BackupHelp3"));
                        foreach(var chara in failCharacters)
                        {
                            Service.Chat.PrintError(chara);
                        }
                    }
                }
                ImGui.EndPopup();
            }
        }

        // 备份数据处理 Backup Handler
        private string BackupHandler(string dataFolder)
        {
            var backupFolder = Path.Combine(dataFolder, "Backups");
            Directory.CreateDirectory(backupFolder);

            var tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempFolder);

            var zipFilePath = string.Empty;
            try
            {
                var files = Directory.GetFiles(dataFolder);
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var destFile = Path.Combine(tempFolder, fileName);
                    File.Copy(file, destFile, true);
                }

                var zipFileName = "Backup_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".zip";
                zipFilePath = Path.Combine(backupFolder, zipFileName);

                ZipFile.CreateFromDirectory(tempFolder, zipFilePath);
            }
            finally
            {
                Directory.Delete(tempFolder, true);
            }
            return zipFilePath;
        }
    }
}
