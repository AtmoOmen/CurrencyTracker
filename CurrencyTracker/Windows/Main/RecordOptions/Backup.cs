using System;
using System.Collections.Generic;
using System.IO;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Trackers;
using CurrencyTracker.Manager.Trackers.Components;
using CurrencyTracker.Manager.Transactions;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using ImGuiNET;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Windows;

public partial class Main
{
    private static void BackupUI()
    {
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Save, Service.Lang.GetText("Backup")))
            ImGui.OpenPopup("BackupUI");

        if (ImGui.BeginPopup("BackupUI"))
        {
            ManualBackupUI();
            AutoBackupUI();
            MaxBackupFileUI();

            ImGui.EndPopup();
        }
    }

    private static void ManualBackupUI()
    {
        ImGui.TextColored(ImGuiColors.DalamudYellow, Service.Lang.GetText("ManualBackup"));
        ImGui.Separator();

        if (ImGui.Button(Service.Lang.GetText("BackupCurrentCharacter")))
        {
            var filePath =
                TransactionsHandler.BackupTransactions(P.PlayerDataFolder, Service.Config.MaxBackupFilesCount);
            DService.Chat.Print(Service.Lang.GetText("BackupHelp4", filePath));
        }

        ImGui.SameLine();
        if (ImGui.Button(Service.Lang.GetText("BackupAllCharacter")))
        {
            var failCharacters = new List<string>();
            var successCount = 0;

            foreach (var character in Service.Config.CurrentActiveCharacter)
            {
                var backupPath = Path.Join(P.PI.ConfigDirectory.FullName,
                                           $"{character.Name}_{character.Server}");
                if (string.IsNullOrEmpty(
                        TransactionsHandler.BackupTransactions(backupPath, Service.Config.MaxBackupFilesCount)))
                    failCharacters.Add($"{character.Name}@{character.Server}");
                else successCount++;
            }

            DService.Chat.Print(Service.Lang.GetText("BackupHelp1", successCount) +
                               (failCharacters.Count != 0 ? Service.Lang.GetText("BackupHelp2", failCharacters.Count) : ""));

            if (failCharacters.Count != 0)
            {
                DService.Chat.PrintError(Service.Lang.GetText("BackupHelp3"));
                foreach (var chara in failCharacters) DService.Chat.PrintError(chara);
            }
        }
    }

    private static void AutoBackupUI()
    {
        var autoSaveEnabled = ComponentManager.TryGet<AutoSave>(out var component) && component.Initialized;

        var autoBackupText = Service.Lang.GetText("AutoBackup");
        if (autoSaveEnabled)
        {
            var countdown = AutoSave.NextAutoSaveTime - DateTime.Now;
            autoBackupText = $"{Service.Lang.GetText("AutoBackup")} ({(countdown.TotalHours < 1 ? countdown.ToString(@"mm\:ss") : countdown.ToString(@"hh\:mm\:ss"))})";
        }

        ImGui.TextColored(autoSaveEnabled ? ImGuiColors.DalamudYellow : ImGuiColors.DalamudGrey, autoBackupText);
        ImGuiOm.TooltipHover(Service.Lang.GetText("BackupHelp7"));

        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            if (component.Initialized)
                ComponentManager.Unload(component);
            else
                ComponentManager.Load(component);

            Service.Config.Save();
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

            var autoSaveInterval = Service.Config.AutoSaveInterval;
            ImGui.SameLine();
            ImGui.SetNextItemWidth(140f);
            if (ImGui.InputInt(Service.Lang.GetText("Minutes"), ref autoSaveInterval, 5, 10))
            {
                Service.Config.AutoSaveInterval = Math.Max(autoSaveInterval, 5);
                Service.Config.Save();

                AutoSave.LastAutoSaveTime = DateTime.Now;
                AutoSave.NextAutoSaveTime = AutoSave.LastAutoSaveTime + TimeSpan.FromMinutes(Service.Config.AutoSaveInterval);
            }

            var isNotification = Service.Config.AutoSaveMessage;
            if (ImGui.Checkbox(Service.Lang.GetText("BackupHelp5"), ref isNotification))
            {
                Service.Config.AutoSaveMessage = isNotification;
                Service.Config.Save();
            }
        }
    }

    private static void AutoSaveRadioButton(string textKey, int mode)
    {
        var isSelected = Service.Config.AutoSaveMode == mode;
        ImGui.RadioButton(Service.Lang.GetText(textKey), isSelected);
        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            Service.Config.AutoSaveMode = mode;
            Service.Config.Save();
        }
    }

    private static void MaxBackupFileUI()
    {
        ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("MaxBackupFiles")}:");
        ImGui.Separator();

        var maxBackupFilesCount = Service.Config.MaxBackupFilesCount;
        ImGui.SetNextItemWidth(210f);
        if (ImGui.InputInt("", ref maxBackupFilesCount))
        {
            maxBackupFilesCount = Math.Max(maxBackupFilesCount, 0);
            Service.Config.MaxBackupFilesCount = maxBackupFilesCount;
            Service.Config.Save();
        }

        ImGuiOm.HelpMarker(Service.Lang.GetText("BackupHelp6"));
    }
}
