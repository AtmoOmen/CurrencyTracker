using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Infos;
using CurrencyTracker.Manager.Langs;
using CurrencyTracker.Manager.Trackers;
using CurrencyTracker.Manager.Trackers.Components;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ImGuiNET;
using OmenTools.ImGuiOm;
using LanguageManager = CurrencyTracker.Manager.Langs.LanguageManager;

namespace CurrencyTracker.Windows;

public class Settings : Window, IDisposable
{
    private string devLangFilePath = string.Empty;

    public Settings(Plugin plugin) : base($"Settings##{Name}")
    {
        Flags |= ImGuiWindowFlags.NoScrollbar;
        Flags |= ImGuiWindowFlags.AlwaysAutoResize;
    }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("SettingsCT"))
        {
            if (ImGui.BeginTabItem(Service.Lang.GetText("Record")))
            {
                if (ImGui.BeginTabBar("RecordSettings"))
                {
                    // 一般 General
                    if (ImGui.BeginTabItem(Service.Lang.GetText("General")))
                    {
                        // 邮件附件 Letter Attachments
                        ModuleCheckbox(typeof(LetterAttachments),
                                       Service.Lang.GetText("LetterAttachments-RecordMailAttachments"));
                        if (Service.Config.ComponentEnabled["LetterAttachments"])
                            NoteContentInputText("LetterAttachments-LetterFrom",
                                                 new[] { Service.Lang.GetText("ParamEP-SenderName") });

                        // 传送 Teleport
                        ImGui.Separator();
                        ModuleCheckbox(typeof(TeleportCosts), Service.Lang.GetText("TeleportCosts-RecordTPCosts"));
                        if (Service.Config.ComponentEnabled["TeleportCosts"])
                            SecondaryRadioButtons(boolName1: "RecordDesAetheryteName", "RecordDesAreaName",
                                                  Service.Lang.GetText("TeleportCosts-RecordAetheryteName"),
                                                  Service.Lang.GetText("TeleportCosts-RecordAreaName"));
                        ModuleCheckbox(typeof(WarpCosts), Service.Lang.GetText("WarpCosts-RecordTPCosts"));
                        if (Service.Config.ComponentEnabled["TeleportCosts"] || Service.Config.ComponentEnabled["WarpCosts"])
                            NoteContentInputText("TeleportTo",
                                                 new[] { Service.Lang.GetText("ParamEP-DestinationName") });
                        if (Service.Config.ComponentEnabled["WarpCosts"]) NoteContentInputText("TeleportWithinArea", null);

                        // 任务 Quest
                        ImGui.Separator();
                        ModuleCheckbox(typeof(QuestRewards), Service.Lang.GetText("QuestRewards-RecordQuestRewards"));
                        if (Service.Config.ComponentEnabled["QuestRewards"])
                            NoteContentInputText("Quest", new[] { Service.Lang.GetText("ParamEP-QuestName") });

                        // 无人岛 Island Sanctuary
                        ImGui.Separator();
                        ModuleCheckbox(typeof(IslandSanctuary), Service.Lang.GetText("IslandSanctuary-RecordISResult"));
                        if (Service.Config.ComponentEnabled["IslandSanctuary"])
                        {
                            NoteContentInputText("IslandFarm", null);
                            NoteContentInputText("IslandPasture", null);
                            NoteContentInputText("IslandWorkshop", null);
                        }
                        ImGui.EndTabItem();
                    }

                    // 战斗 Combat
                    if (ImGui.BeginTabItem(Service.Lang.GetText("Combat")))
                    {
                        ModuleCheckbox(typeof(DutyRewards), Service.Lang.GetText("DutyRewards-RecordDutyRewards"));
                        if (Service.Config.ComponentEnabled["DutyRewards"])
                            SecondaryCheckbox("RecordContentName",
                                              Service.Lang.GetText("DutyRewards-RecordContentName"));

                        ImGui.Separator();
                        ModuleCheckbox(typeof(FateRewards), Service.Lang.GetText("FateRewards-RecordFateRewards"));
                        if (Service.Config.ComponentEnabled["FateRewards"])
                            NoteContentInputText("Fate", new[] { Service.Lang.GetText("ParamEP-FateName") });

                        ImGui.Separator();
                        ModuleCheckbox(typeof(MobDrops), Service.Lang.GetText("MobDrops-RecordMobDrops"));
                        if (Service.Config.ComponentEnabled["MobDrops"])
                            NoteContentInputText("MobDrops-MobDropsNote",
                                                 new[] { Service.Lang.GetText("ParamEP-MobNames") });
                        ImGui.EndTabItem();
                    }

                    // 交易 Trade
                    if (ImGui.BeginTabItem(Service.Lang.GetText("Trade")))
                    {
                        // 交换 Exchange
                        ModuleCheckbox(typeof(Exchange), Service.Lang.GetText("Exchange-RecordExchangeResult"));
                        ModuleCheckbox(typeof(SpecialExchange),
                                       Service.Lang.GetText("SpecialExchange-RecordSpecialExchangeResult"));
                        if (Service.Config.ComponentEnabled["Exchange"] || Service.Config.ComponentEnabled["SpecialExchange"])
                            NoteContentInputText("ExchangeWith", new[] { Service.Lang.GetText("ParamEP-TargetName") });

                        // 交易 Trade
                        ImGui.Separator();
                        ModuleCheckbox(typeof(Trade), Service.Lang.GetText("Trade-RecordTradeTarget"));
                        if (Service.Config.ComponentEnabled["Trade"])
                            NoteContentInputText("TradeWith", new[] { Service.Lang.GetText("ParamEP-TargetName") });
                        ImGui.EndTabItem();
                    }

                    // 金碟 Gold Saucer
                    if (ImGui.BeginTabItem(Service.Lang.GetText("GoldSaucer")))
                    {
                        ModuleCheckbox(typeof(GoldSaucer), Service.Lang.GetText("GoladSaucer-RecordMGPSource"));

                        ImGui.Separator();
                        ModuleCheckbox(typeof(TripleTriad), Service.Lang.GetText("TripleTriad-RecordTTResult"));
                        if (Service.Config.ComponentEnabled["TripleTriad"])
                            NoteContentInputText("TripleTriadWith", new[]
                                                 {
                                                     Service.Lang.GetText("ParamEP-TTOutcome"),
                                                     Service.Lang.GetText("ParamEP-TTRivalName")
                                                 });
                        ImGui.EndTabItem();
                    }
                    ImGui.EndTabBar();
                }
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Service.Lang.GetText("Features")))
            {
                ModuleCheckbox(typeof(CurrencyUIEdit), Service.Lang.GetText("CurrencyUIEdit-ShowTotalGilAmount"));
                ModuleCheckbox(typeof(ServerBar), Service.Lang.GetText("DisplayChangesInServerBar"));
                if (Service.Config.ComponentEnabled["ServerBar"])
                {
                    ImGui.Indent();

                    ImGui.AlignTextToFramePadding();
                    ImGui.Text($"{Service.Lang.GetText("DisplayedCurrency")}:");

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(150f * ImGuiHelpers.GlobalScale);
                    if (ImGui.BeginCombo("###ServerBarDisplayCurrency", CurrencyInfo.GetCurrencyName(Service.Config.ServerBarDisplayCurrency), ImGuiComboFlags.HeightLarge))
                    {
                        foreach (var currency in Service.Config.AllCurrencies)
                        {
                            if (ImGui.Selectable(currency.Value,
                                                 currency.Key == Service.Config.ServerBarDisplayCurrency))
                            {
                                Service.Config.ServerBarDisplayCurrency = currency.Key;
                                ServerBar.OnCurrencyChanged(currency.Key, TransactionFileCategory.Inventory, 0);

                                Service.Config.Save();
                            }
                        }
                        ImGui.EndCombo();
                    }

                    ImGui.AlignTextToFramePadding();
                    ImGui.Text($"{Service.Lang.GetText("CycleMode")}:");

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(150f * ImGuiHelpers.GlobalScale);
                    if (ImGui.BeginCombo("###ServerBarCycleMode", ServerBar.GetCycleModeLoc(Service.Config.ServerBarCycleMode)))
                    {
                        for (var i = 0; i < 4; i++)
                        {
                            if (ImGui.Selectable(ServerBar.GetCycleModeLoc(i), i == Service.Config.ServerBarCycleMode))
                            {
                                Service.Config.ServerBarCycleMode = i;
                                ServerBar.OnCurrencyChanged(Service.Config.ServerBarDisplayCurrency, TransactionFileCategory.Inventory, 0);

                                Service.Config.Save();
                            }
                        }
                        ImGui.EndCombo();
                    }

                    ImGui.Unindent();
                }

                ImGui.Separator();
                ModuleCheckbox(typeof(Retainer), Service.Lang.GetText("Retainer-RecordRetainerInventory"));
                ModuleCheckbox(typeof(SaddleBag), Service.Lang.GetText("SaddleBag-RecordSaddleBag"));
                ModuleCheckbox(typeof(PremiumSaddleBag),
                               Service.Lang.GetText("PremiumSaddleBag-RecordPremiumSaddleBag"));
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Service.Lang.GetText("Plugin")))
            {
                // 导出文件类型 Export File Type
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("ExportFileType")}:");
                ImGui.SameLine();

                var exportDataFileType = Service.Config.ExportDataFileType;
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

                // 备份 Backup
                ImGui.Separator();
                ModuleCheckbox(typeof(AutoSave), Service.Lang.GetText("AutoBackup"));
                if (Service.Config.ComponentEnabled["AutoSave"])
                {
                    SecondaryRadioButtons("AutoSaveMode", Service.Lang.GetText("BackupCurrentCharacter"),
                                          Service.Lang.GetText("BackupAllCharacter"));

                    ImGui.Indent();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text($"{Service.Lang.GetText("Interval")}:");

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(140f);
                    var autoSaveInterval = Service.Config.AutoSaveInterval;
                    if (ImGui.InputInt(Service.Lang.GetText("Minutes"), ref autoSaveInterval, 5, 10))
                    {
                        if (autoSaveInterval < 5) autoSaveInterval = 5;
                        Service.Config.AutoSaveInterval = autoSaveInterval;
                        Service.Config.Save();
                    }

                    var isNotification = Service.Config.AutoSaveMessage;
                    if (ImGui.Checkbox(Service.Lang.GetText("BackupHelp5"), ref isNotification))
                    {
                        Service.Config.AutoSaveMessage = !Service.Config.AutoSaveMessage;
                        Service.Config.Save();
                    }
                    ImGui.Unindent();
                }

                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("MaxBackupFiles")}:");

                ImGui.SameLine();
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                var maxBackupFilesCount = Service.Config.MaxBackupFilesCount;
                if (ImGui.InputInt("##MaxBackupFilesCount", ref maxBackupFilesCount))
                {
                    if (maxBackupFilesCount < 0) maxBackupFilesCount = 0;
                    Service.Config.MaxBackupFilesCount = maxBackupFilesCount;
                    Service.Config.Save();
                }

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Service.Lang.GetText("Info")))
            {
                ImGui.TextColored(ImGuiColors.DalamudYellow, "Author:");

                ImGui.SameLine();
                ImGui.Text("AtmoOmen");

                ImGui.Separator();
                ImGui.TextColored(ImGuiColors.DalamudYellow, "Contact:");

                ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.TankBlue);
                if (ImGui.Button("GitHub")) Util.OpenLink("https://github.com/AtmoOmen/CurrencyTracker/");
                ImGui.PopStyleColor();

                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.ParsedPurple);
                if (ImGui.Button("Discord Thread"))
                    Util.OpenLink(
                        "https://discord.com/channels/581875019861328007/1019646133305344090/threads/1163039624957010021");
                ImGui.PopStyleColor();

                ImGui.SameLine();
                if (ImGui.Button("QQ 频道")) Util.OpenLink("https://pd.qq.com/s/fttirpnql");

                ImGui.Separator();
                ImGui.TextColored(ImGuiColors.DalamudYellow, "Languages & Translators:");
                foreach (var languageInfo in LanguageManager.LanguageNames)
                {
                    if (ImGui.Button(languageInfo.DisplayName) && languageInfo.Language != Service.Config.SelectedLanguage)
                        Service.Lang.SwitchLanguage(languageInfo.Language);

                    ImGui.SameLine();
                    ImGui.Text($"{string.Join(", ", languageInfo.Translators)}");
                }

                ImGui.Separator();

                if (ImGuiOm.ButtonIconSelectable("UpdateTranslations",
                                                 P.Main.isLangDownloading
                                                     ? FontAwesomeIcon.Spinner
                                                     : FontAwesomeIcon.CloudDownloadAlt,
                                                 Service.Lang.GetText("UpdateTranslations")))
                {
                    if (!P.Main.isLangDownloading)
                    {
                        P.Main.isLangDownloading = true;
                        P.Main.isLangDownloaded = false;
                        Task.Run(async () =>
                        {
                            await LanguageUpdater.DownloadLanguageFilesAsync();
                            P.Main.isLangDownloading = false;
                            P.Main.isLangDownloaded = true;
                            Service.Lang.SwitchLanguage(Service.Config.SelectedLanguage);
                        });
                    }
                }

                if (ImGuiOm.ButtonIconSelectable("HelpTranslate", FontAwesomeIcon.Language,
                                                 Service.Lang.GetText("HelpTranslate")))
                    Util.OpenLink("https://crowdin.com/project/dalamud-currencytracker");
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Dev"))
            {
                if (ImGui.Button("Open Local Language Files Directory")) OpenDirectory(LanguageManager.LangsDirectory);

                ImGui.InputTextWithHint("##LoadDevLangFile", "Dev Lang File Path (.resx)", ref devLangFilePath, 1000);

                ImGui.SameLine();
                if (ImGui.Button("Load"))
                {
                    if (string.IsNullOrEmpty(devLangFilePath) || !File.Exists(devLangFilePath)) return;
                    Service.Lang = new LanguageManager("Dev", true, devLangFilePath);
                }
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    private static void ModuleCheckbox(Type type, string checkboxLabel, string help = "")
    {
        var boolName = type.Name;
        if (!Service.Config.ComponentEnabled.TryGetValue(boolName, out var tempBool)) return;

        if (!typeof(ITrackerComponent).IsAssignableFrom(type))
        {
            Service.Log.Error($"Fail to fetch component {type.Name}");
            return;
        }

        ImGui.PushID($"{boolName}-{type.Name}");
        if (ImGuiOm.CheckboxColored($"{checkboxLabel}", ref tempBool))
        {
            Service.Config.ComponentEnabled[boolName] ^= true;
            if (ComponentManager.Components.TryGetValue(type, out var component))
            {
                if (Service.Config.ComponentEnabled[boolName])
                    ComponentManager.Load(component);
                else
                    ComponentManager.Unload(component);
            }
            else
                Service.Log.Error($"Fail to fetch component {type.Name}");

            Service.Config.Save();
        }

        if (!string.IsNullOrEmpty(help))
        {
            ImGui.SameLine();
            ImGuiComponents.HelpMarker(help);
        }
        ImGui.PopID();
    }

    private static void SecondaryCheckbox(string boolName, string checkboxLabel, string help = "")
    {
        var cbool = Service.Config.ComponentProp[boolName];

        ImGui.PushID($"{checkboxLabel}-{boolName}");
        ImGui.Indent();

        if (ImGui.Checkbox(checkboxLabel, ref cbool))
        {
            Service.Config.ComponentProp[boolName] = !Service.Config.ComponentProp[boolName];
            Service.Config.Save();
        }

        if (!string.IsNullOrEmpty(help))
        {
            ImGui.SameLine();
            ImGuiComponents.HelpMarker(help);
        }

        ImGui.Unindent();
        ImGui.PopID();
    }

    private static void SecondaryRadioButtons(
        string boolName1, string boolName2, string buttonLabel1, string buttonLabel2, string help = "")
    {
        var cbool1 = Service.Config.ComponentProp[boolName1];
        var cbool2 = Service.Config.ComponentProp[boolName2];

        ImGui.PushID($"{buttonLabel1}-{buttonLabel2}-{buttonLabel1}-{buttonLabel2}");
        ImGui.Indent();

        if (ImGui.RadioButton(buttonLabel1, cbool1))
        {
            Service.Config.ComponentProp[boolName1] = true;
            Service.Config.ComponentProp[boolName2] = false;
            Service.Config.Save();
        }

        ImGui.SameLine();
        if (ImGui.RadioButton(buttonLabel2, cbool2))
        {
            Service.Config.ComponentProp[boolName1] = false;
            Service.Config.ComponentProp[boolName2] = true;
            Service.Config.Save();
        }

        if (!string.IsNullOrEmpty(help))
        {
            ImGui.SameLine();
            ImGuiComponents.HelpMarker(help);
        }
        ImGui.Unindent();
        ImGui.PopID();
    }

    private static void SecondaryRadioButtons(
        string propertyName, string buttonLabel1, string buttonLabel2, string help = "")
    {
        var propertyValue = typeof(Configuration).GetProperty(propertyName)?.GetValue(Service.Config);

        if (propertyValue is null) return;

        var cbool1 = (int)propertyValue == 0;
        var cbool2 = (int)propertyValue == 1;

        ImGui.PushID($"{buttonLabel1}-{buttonLabel2}-{buttonLabel1}-{buttonLabel2}");
        ImGui.Indent();

        if (ImGui.RadioButton(buttonLabel1, cbool1))
        {
            typeof(Configuration).GetProperty(propertyName)?.SetValue(Service.Config, 0);
            Service.Config.Save();
        }

        ImGui.SameLine();
        if (ImGui.RadioButton(buttonLabel2, cbool2))
        {
            typeof(Configuration).GetProperty(propertyName)?.SetValue(Service.Config, 1);
            Service.Config.Save();
        }

        if (!string.IsNullOrEmpty(help))
        {
            ImGui.SameLine();
            ImGuiComponents.HelpMarker(help);
        }

        ImGui.Unindent();
        ImGui.PopID();
    }

    private static void NoteContentInputText(string key, IReadOnlyList<string>? paramsEP)
    {
        var textToShow = Service.Config.CustomNoteContents.TryGetValue(key, out var value) ? value : Service.Lang.GetOrigText(key);
        ImGui.PushID(key);
        ImGui.Indent();

        ImGui.AlignTextToFramePadding();
        ImGui.Text($"{Service.Lang.GetText("Note")}:");

        ImGui.SameLine();
        ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
        if (ImGui.InputText("", ref textToShow, 50))
        {
            Service.Config.CustomNoteContents[key] = textToShow;
            Service.Config.Save();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            if (!string.IsNullOrEmpty(textToShow))
            {
                ImGui.Text(textToShow);
                if (paramsEP != null)
                {
                    ImGui.Separator();
                    for (var i = 0; i < paramsEP.Count; i++) ImGui.Text("{" + i + "}" + $" - {paramsEP[i]}");
                }
            }

            ImGui.EndTooltip();
        }

        ImGui.SameLine();
        if (ImGuiOm.ButtonIcon("", FontAwesomeIcon.Sync, Service.Lang.GetText("Reset")))
        {
            Service.Config.CustomNoteContents.Remove(key);
            Service.Config.Save();
        }

        ImGui.Unindent();
        ImGui.PopID();
    }

    public void Dispose() { }
}
