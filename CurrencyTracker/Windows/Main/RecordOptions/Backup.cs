namespace CurrencyTracker.Windows;

public partial class Main : Window, IDisposable
{
    private void BackupUI()
    {
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Save, Service.Lang.GetText("Backup")))
        {
            ImGui.OpenPopup("BackupUI");
        }

        using (var popup = ImRaii.Popup("BackupUI"))
        {
            if (popup)
            {
                ManualBackupUI();
                AutoBackupUI();
                MaxBackupFileUI();
            }
        }
    }

    private void ManualBackupUI()
    {
        ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("ManualBackup"));
        ImGui.Separator();

        if (ImGui.Button(Service.Lang.GetText("BackupCurrentCharacter")))
        {
            var filePath = TransactionsHandler.BackupTransactions(P.PlayerDataFolder, C.MaxBackupFilesCount);
            Service.Chat.Print(Service.Lang.GetText("BackupHelp4", filePath));
        }

        ImGui.SameLine();
        if (ImGui.Button(Service.Lang.GetText("BackupAllCharacter")))
        {
            var failCharacters = new List<string>();
            var successCount = 0;

            foreach (var character in C.CurrentActiveCharacter)
            {
                var backupPath = Path.Join(P.PluginInterface.ConfigDirectory.FullName, $"{character.Name}_{character.Server}");
                if (TransactionsHandler.BackupTransactions(backupPath, C.MaxBackupFilesCount).IsNullOrEmpty()) failCharacters.Add($"{character.Name}@{character.Server}");
                else successCount++;
            }

            Service.Chat.Print(Service.Lang.GetText("BackupHelp1", successCount) + (failCharacters.Any() ? Service.Lang.GetText("BackupHelp2", failCharacters.Count) : ""));

            if (failCharacters.Any())
            {
                Service.Chat.PrintError(Service.Lang.GetText("BackupHelp3"));
                foreach (var chara in failCharacters) Service.Chat.PrintError(chara);
            }
        }
    }

    private void AutoBackupUI()
    {
        var autoSaveEnabled = C.ComponentEnabled["AutoSave"];
        var autoSaveComponent = ComponentManager.Components.OfType<AutoSave>().FirstOrDefault();
        var nextAutoSaveTime = DateTime.Today.Add(autoSaveComponent?.Initialized == true
            ? (AutoSave.LastAutoSave + TimeSpan.FromMinutes(C.AutoSaveInterval) - DateTime.Now)
            : TimeSpan.Zero);
        var timeFormat = nextAutoSaveTime.Hour == 0 ? "mm:ss" : "HH:mm:ss";
        var autoBackupText = autoSaveEnabled
            ? $"{Service.Lang.GetText("AutoBackup")} ({nextAutoSaveTime.ToString(timeFormat)})"
            : Service.Lang.GetText("AutoBackup");

        ImGui.TextColored(autoSaveEnabled ? ImGuiColors.DalamudYellow : ImGuiColors.DalamudGrey, autoBackupText);
        ImGuiOm.TooltipHover(Service.Lang.GetText("BackupHelp7"));

        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            C.ComponentEnabled["AutoSave"] = !autoSaveEnabled;
            if (autoSaveComponent != null)
            {
                if (autoSaveEnabled) ComponentManager.Load(autoSaveComponent);
                else ComponentManager.Unload(autoSaveComponent);
            }
            else
            {
                Service.Log.Error("Fail to fetch AutoSave component");
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

            var autoSaveInterval = C.AutoSaveInterval;
            ImGui.SameLine();
            ImGui.SetNextItemWidth(140f);
            if (ImGui.InputInt(Service.Lang.GetText("Minutes"), ref autoSaveInterval, 5, 10))
            {
                C.AutoSaveInterval = Math.Max(autoSaveInterval, 5);
                C.Save();
            }

            var isNotification = C.AutoSaveMessage;
            if (ImGui.Checkbox(Service.Lang.GetText("BackupHelp5"), ref isNotification))
            {
                C.AutoSaveMessage = isNotification;
                C.Save();
            }
        }
    }

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

    private void MaxBackupFileUI()
    {
        ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("MaxBackupFiles")}:");
        ImGui.Separator();

        var maxBackupFilesCount = C.MaxBackupFilesCount;
        ImGui.SetNextItemWidth(210f);
        if (ImGui.InputInt("", ref maxBackupFilesCount))
        {
            maxBackupFilesCount = Math.Max(maxBackupFilesCount, 0);
            C.MaxBackupFilesCount = maxBackupFilesCount;
            C.Save();
        }

        ImGuiOm.HelpMarker(Service.Lang.GetText("BackupHelp6"));
    }
}
