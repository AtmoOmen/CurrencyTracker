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
            ExportDataUI();
            ImGui.SameLine();
            BackupUI();
        }

        // 记录设置界面 Record Settings
        private void RecordSettingsUI()
        {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Cog, Service.Lang.GetText("Settings")))
            {
                P.RecordSettings.IsOpen = !P.RecordSettings.IsOpen;
            }
        }

        // 按临界值合并记录界面 Merge Transactions By Threshold
        private void MergeTransactionUI()
        {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ObjectGroup, Service.Lang.GetText("Merge")))
            {
                ImGui.OpenPopup("MergeTransactions");
            }

            if (ImGui.BeginPopup("MergeTransactions"))
            {
                ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("ManualMerge"));
                ImGui.Text(Service.Lang.GetText("Threshold"));
                ImGui.SameLine();
                ImGui.SetNextItemWidth(150f);
                if (ImGui.InputInt("##MergeThreshold", ref mergeThreshold, 100, 100, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    mergeThreshold = Math.Max(0, mergeThreshold);
                }
                ImGui.SameLine();
                ImGuiComponents.HelpMarker($"{Service.Lang.GetText("MergeTransactionsHelp3")}");

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

            UpdateTransactions(selectedCurrencyID, currentView, currentViewID);
            return mergeCount;
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

                ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("FileRenameLabel") + $"({Service.Lang.GetText("PressEnterToConfirm")})");

                ImGui.SetNextItemWidth(200);
                if (ImGui.InputText($"_{C.AllCurrencies[selectedCurrencyID]}_{Service.Lang.GetText("FileRenameLabel2")}{(exportDataFileType == 0 ? ".csv" : ".md")}", ref fileName, 64, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    if (currentTypeTransactions == null || currentTypeTransactions.Count == 0)
                    {
                        return;
                    }

                    var filePath = Transactions.ExportData(currentTypeTransactions, fileName, selectedCurrencyID, exportDataFileType);
                    Service.Chat.Print($"{Service.Lang.GetText("ExportFileMessage")}{filePath}");
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
                ManualBackupUI();
                AutoBackupUI();

                ImGui.Separator();
                ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("MaxBackupFiles")}:");

                ImGui.SetNextItemWidth(210f);
                var maxBackupFilesCount = C.MaxBackupFilesCount;
                if (ImGui.InputInt("", ref maxBackupFilesCount))
                {
                    maxBackupFilesCount = Math.Max(maxBackupFilesCount, 0);
                    C.MaxBackupFilesCount = maxBackupFilesCount;
                    C.Save();
                }
                ImGuiComponents.HelpMarker(Service.Lang.GetText("BackupHelp6"));

                ImGui.EndPopup();
            }
        }

        // 自动备份界面 Manual Backup UI
        private void ManualBackupUI()
        {
            ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("ManualBackup"));
            ImGui.Separator();

            if (ImGui.Button($"{Service.Lang.GetText("BackupCurrentCharacter")}"))
            {
                var filePath = Transactions.BackupTransactions(P.PlayerDataFolder, C.MaxBackupFilesCount);
                Service.Chat.Print(Service.Lang.GetText("BackupHelp4", filePath));
            }

            ImGui.SameLine();
            if (ImGui.Button($"{Service.Lang.GetText("BackupAllCharacter")}"))
            {
                var failCharacters = C.CurrentActiveCharacter
                    .Where(character => Transactions.BackupTransactions(Path.Join(P.PluginInterface.ConfigDirectory.FullName, $"{character.Name}_{character.Server}"), C.MaxBackupFilesCount).IsNullOrEmpty())
                    .Select(character => $"{character.Name}@{character.Server}")
                    .ToList();

                var successCount = C.CurrentActiveCharacter.Count - failCharacters.Count;
                Service.Chat.Print(Service.Lang.GetText("BackupHelp1", successCount) + (failCharacters.Any() ? Service.Lang.GetText("BackupHelp2", failCharacters.Count) : ""));

                if (failCharacters.Any())
                {
                    Service.Chat.PrintError(Service.Lang.GetText("BackupHelp3"));
                    foreach (var chara in failCharacters)
                    {
                        Service.Chat.PrintError(chara);
                    }
                }
            }
        }

        // 自动备份界面 Auto Backup UI
        private void AutoBackupUI()
        {
            var autoSaveEnabled = C.ComponentEnabled["AutoSave"];
            var time = DateTime.Today.Add(ComponentManager.Components.OfType<AutoSave>().FirstOrDefault().Initialized ?
            (AutoSave.LastAutoSave + TimeSpan.FromMinutes(C.AutoSaveInterval) - DateTime.Now) : TimeSpan.FromMinutes(0));
            var timeFormat = time.Hour == 0 ? "mm:ss" : "HH:mm:ss";
            var autoBackupText = autoSaveEnabled ? $"{Service.Lang.GetText("AutoBackup")} ({time.ToString(timeFormat)})" : Service.Lang.GetText("AutoBackup");

            ImGui.TextColored(autoSaveEnabled ? ImGuiColors.DalamudYellow : ImGuiColors.DalamudGrey, autoBackupText);
            TextTooltip(Service.Lang.GetText("BackupHelp7"));
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                C.ComponentEnabled["AutoSave"] = !C.ComponentEnabled["AutoSave"];
                var component = ComponentManager.Components.FirstOrDefault(c => c.GetType() == typeof(AutoSave));
                if (component != null)
                {
                    if (C.ComponentEnabled["AutoSave"])
                    {
                        ComponentManager.Load(component);
                    }
                    else
                    {
                        ComponentManager.Unload(component);
                    }
                }
                else
                {
                    Service.Log.Error($"Fail to fetch component {component.GetType().Name}");
                }

                C.Save();
            }

            if (autoSaveEnabled)
            {
                ImGui.Separator();
                ImGui.AlignTextToFramePadding();
                ImGui.Text("");

                ImGui.SameLine(5f);
                AutoSaveRadioButton("BackupCurrentCharacter", 0);
                ImGui.SameLine();
                AutoSaveRadioButton("BackupAllCharacter", 1);

                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("Interval")}:");

                ImGui.SameLine();
                ImGui.SetNextItemWidth(140f);
                var autoSaveInterval = C.AutoSaveInterval;
                if (ImGui.InputInt(Service.Lang.GetText("Minutes"), ref autoSaveInterval, 5, 10))
                {
                    if (autoSaveInterval < 5) autoSaveInterval = 5;
                    C.AutoSaveInterval = autoSaveInterval;
                    C.Save();
                }

                var isNotification = C.AutoSaveMessage;
                if (ImGui.Checkbox(Service.Lang.GetText("BackupHelp5"), ref isNotification))
                {
                    C.AutoSaveMessage = !C.AutoSaveMessage;
                    C.Save();
                }
            }
        }

        // 自动保存界面单选按钮 Auto Backup Radio Button
        private void AutoSaveRadioButton(string textKey, int mode)
        {
            var isSelected = C.AutoSaveMode == mode;
            ImGui.RadioButton(Service.Lang.GetText(textKey), isSelected);
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                C.AutoSaveMode = mode;
                C.Save();
            }
        }
    }
}
