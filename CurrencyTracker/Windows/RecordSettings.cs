namespace CurrencyTracker.Windows
{
    public class Settings : Window, IDisposable
    {
        private Configuration? C = Plugin.Configuration;
        private Plugin? P = Plugin.Instance;

        public Settings(Plugin plugin) : base($"Settings##{Plugin.Name}")
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
                    if (ImGui.BeginTabBar("RecordSettings", ImGuiTabBarFlags.Reorderable))
                    {
                        // 一般 General
                        if (ImGui.BeginTabItem(Service.Lang.GetText("General")))
                        {
                            // 交换 Exchange
                            ModuleCheckbox(typeof(Exchange), "Exchange", Service.Lang.GetText("Exchange-RecordExchangeResult"));
                            ModuleCheckbox(typeof(SpecialExchange), "SpecialExchange", Service.Lang.GetText("SpecialExchange-RecordSpecialExchangeResult"));
                            if (C.ComponentEnabled["Exchange"] || C.ComponentEnabled["SpecialExchange"]) NoteContentInputText("ExchangeWith", new string[1] { Service.Lang.GetText("ParamEP-TargetName") });

                            // 传送 Teleport
                            ImGui.Separator();
                            ModuleCheckbox(typeof(TeleportCosts), "TeleportCosts", Service.Lang.GetText("TeleportCosts-RecordTPCosts"));
                            if (C.ComponentEnabled["TeleportCosts"]) SecondaryRadioButtons(boolName1: "RecordDesAetheryteName", "RecordDesAreaName", Service.Lang.GetText("TeleportCosts-RecordAetheryteName"), Service.Lang.GetText("TeleportCosts-RecordAreaName"));
                            ModuleCheckbox(typeof(WarpCosts), "WarpCosts", Service.Lang.GetText("WarpCosts-RecordTPCosts"));
                            if (C.ComponentEnabled["TeleportCosts"] || C.ComponentEnabled["WarpCosts"]) NoteContentInputText("TeleportTo", new string[1] { Service.Lang.GetText("ParamEP-DestinationName") });
                            if (C.ComponentEnabled["WarpCosts"]) NoteContentInputText("TeleportWithinArea", null);

                            // 任务 Quest
                            ImGui.Separator();
                            ModuleCheckbox(typeof(QuestRewards), "QuestRewards", Service.Lang.GetText("QuestRewards-RecordQuestRewards"));
                            if (C.ComponentEnabled["QuestRewards"]) NoteContentInputText("Quest", new string[1] { Service.Lang.GetText("ParamEP-QuestName") });

                            // 交易 Trade
                            ImGui.Separator();
                            ModuleCheckbox(typeof(Trade), "Trade", Service.Lang.GetText("Trade-RecordTradeTarget"));
                            if (C.ComponentEnabled["Trade"]) NoteContentInputText("Quest", new string[1] { Service.Lang.GetText("ParamEP-QuestName") });

                            // 无人岛 Island Sanctuary
                            ImGui.Separator();
                            ModuleCheckbox(typeof(IslandSanctuary), "IslandSanctuary", Service.Lang.GetText("IslandSanctuary-RecordISResult"));
                            if (C.ComponentEnabled["IslandSanctuary"])
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
                            ModuleCheckbox(typeof(DutyRewards), "DutyRewards", Service.Lang.GetText("DutyRewards-RecordDutyRewards"));
                            if (C.ComponentEnabled["DutyRewards"]) SecondaryCheckbox("RecordContentName", Service.Lang.GetText("DutyRewards-RecordContentName"));

                            ImGui.Separator();
                            ModuleCheckbox(typeof(FateRewards), "FateRewards", Service.Lang.GetText("FateRewards-RecordFateRewards"));
                            if (C.ComponentEnabled["FateRewards"]) NoteContentInputText("Fate", new string[1] { Service.Lang.GetText("ParamEP-FateName") });

                            ImGui.Separator();
                            ModuleCheckbox(typeof(MobDrops), "MobDrops", Service.Lang.GetText("MobDrops-RecordMobDrops"));
                            if (C.ComponentEnabled["MobDrops"]) NoteContentInputText("MobDrops-MobDropsNote", new string[1] { Service.Lang.GetText("ParamEP-MobNames") });

                            ImGui.EndTabItem();
                        }

                        // 金碟 Gold Saucer
                        if (ImGui.BeginTabItem(Service.Lang.GetText("GoldSaucer")))
                        {
                            ModuleCheckbox(typeof(GoldSaucer), "GoldSaucer", Service.Lang.GetText("GoladSaucer-RecordMGPSource"));

                            ImGui.Separator();
                            ModuleCheckbox(typeof(Manager.Trackers.Components.TripleTriad), "TripleTriad", Service.Lang.GetText("TripleTriad-RecordTTResult"));
                            if (C.ComponentEnabled["TripleTriad"]) NoteContentInputText("TripleTriadWith", new string[2] { Service.Lang.GetText("ParamEP-TTOutcome"), Service.Lang.GetText("ParamEP-TTRivalName") });

                            ImGui.EndTabItem();
                        }

                        ImGui.EndTabBar();
                    }

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Features"))
                {
                    ModuleCheckbox(typeof(CurrencyUIEdit), "CurrencyUIEdit", Service.Lang.GetText("CurrencyUIEdit-ShowTotalGilAmount"));

                    ImGui.Separator();
                    ModuleCheckbox(typeof(Retainer), "Retainer", Service.Lang.GetText("Retainer-RecordRetainerInventory"));
                    ModuleCheckbox(typeof(SaddleBag), "SaddleBag", Service.Lang.GetText("SaddleBag-RecordSaddleBag"));
                    ModuleCheckbox(typeof(PremiumSaddleBag), "PremiumSaddleBag", Service.Lang.GetText("PremiumSaddleBag-RecordPremiumSaddleBag"));
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Plugin"))
                {
                    // 导出文件类型 Export File Type
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

                    // 备份 Backup
                    ImGui.Separator();
                    ModuleCheckbox(typeof(AutoSave), "AutoSave", Service.Lang.GetText("AutoBackup"));
                    if (C.ComponentEnabled["AutoSave"])
                    {
                        SecondaryRadioButtons("AutoSaveMode", Service.Lang.GetText("BackupCurrentCharacter"), Service.Lang.GetText("BackupAllCharacter"));

                        ImGui.AlignTextToFramePadding();
                        ImGui.BulletText("");

                        ImGui.SameLine();
                        ImGui.Text($"{Service.Lang.GetText("Interval")}:");

                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(140f);
                        if (ImGui.InputInt(Service.Lang.GetText("Minutes"), ref P.Main.autoSaveInterval, 5, 10))
                        {
                            if (P.Main.autoSaveInterval < 5) P.Main.autoSaveInterval = 5;
                            C.AutoSaveInterval = P.Main.autoSaveInterval;
                            C.Save();
                        }

                        var isNotification = C.AutoSaveMessage;

                        ImGui.AlignTextToFramePadding();
                        ImGui.BulletText("");

                        ImGui.SameLine();
                        if (ImGui.Checkbox(Service.Lang.GetText("BackupHelp5"), ref isNotification))
                        {
                            C.AutoSaveMessage = !C.AutoSaveMessage;
                            C.Save();
                        }
                    }

                    ImGui.AlignTextToFramePadding();
                    ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("MaxBackupFiles")}:");

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(150f);
                    if (ImGui.InputInt("", ref P.Main.maxBackupFilesCount))
                    {
                        if (P.Main.maxBackupFilesCount < 0) P.Main.maxBackupFilesCount = 0;
                        C.MaxBackupFilesCount = P.Main.maxBackupFilesCount;
                        C.Save();
                    }

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Information"))
                {
                    ImGui.TextColored(ImGuiColors.DalamudYellow, $"Author:");

                    ImGui.SameLine();
                    ImGui.Text("AtmoOmen");

                    ImGui.Separator();
                    ImGui.TextColored(ImGuiColors.DalamudYellow, $"Contact:");

                    ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.TankBlue);
                    if (ImGui.Button("GitHub"))
                    {
                        Util.OpenLink("https://github.com/AtmoOmen/CurrencyTracker/");
                    }
                    ImGui.PopStyleColor();

                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.ParsedPurple);
                    if (ImGui.Button("Discord Thread"))
                    {
                        Util.OpenLink("https://discord.com/channels/581875019861328007/1019646133305344090/threads/1163039624957010021");
                    }
                    ImGui.PopStyleColor();

                    ImGui.SameLine();
                    if (ImGui.Button("QQ 频道"))
                    {
                        Util.OpenLink("https://pd.qq.com/s/fttirpnql");
                    }

                    ImGui.Separator();
                    ImGui.TextColored(ImGuiColors.DalamudYellow, $"Languages & Translators:");
                    foreach (var languageInfo in LanguageManager.LanguageNames)
                    {
                        if (ImGui.Button(languageInfo.DisplayName) && languageInfo.Language != C.SelectedLanguage)
                        {
                            C.SelectedLanguage = languageInfo.Language;
                            C.Save();

                            Service.Lang = new LanguageManager(C.SelectedLanguage);
                            Service.CommandManager.RemoveHandler(Plugin.CommandName);
                            Service.CommandManager.AddHandler(Plugin.CommandName, new CommandInfo(P.OnCommand)
                            {
                                HelpMessage = Service.Lang.GetText("CommandHelp") + "\n" + Service.Lang.GetText("CommandHelp1")
                            });
                        }

                        ImGui.SameLine();
                        ImGui.Text($"{languageInfo.Translators}");
                    }

                    ImGui.AlignTextToFramePadding();
                    ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("HelpTranslate")}:");
                    ImGui.SameLine();
                    if (ImGui.Button("Crowdin"))
                    {
                        Util.OpenLink("https://crowdin.com/project/dalamud-currencytracker");
                    }

                    ImGui.EndTabItem();
                }


                ImGui.EndTabBar();
            }
        }

        private void ModuleCheckbox(Type type, string boolName, string checkboxLabel, string help = "")
        {
            var cbool = C.ComponentEnabled[boolName];

            if (!typeof(ITrackerComponent).IsAssignableFrom(type))
            {
                Service.Log.Error($"Fail to fetch component {type.Name}");
                return;
            }

            if (ColoredCheckbox($"{checkboxLabel}##{boolName}-{type.Name}", ref cbool))
            {
                C.ComponentEnabled[boolName] = !C.ComponentEnabled[boolName];
                var component = ComponentManager.Components.FirstOrDefault(c => c.GetType() == type);
                if (component != null)
                {
                    if (C.ComponentEnabled[boolName])
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
                    Service.Log.Error($"Fail to fetch component {type.Name}");
                }

                C.Save();
            }

            if (!help.IsNullOrEmpty())
            {
                ImGui.SameLine();
                ImGuiComponents.HelpMarker(help);
            }
        }

        private void SecondaryCheckbox(string boolName, string checkboxLabel, string help = "")
        {
            var cbool = C.ComponentProp[boolName];

            ImGui.AlignTextToFramePadding();
            ImGui.BulletText("");

            ImGui.SameLine();
            if (ImGui.Checkbox(checkboxLabel, ref cbool))
            {
                C.ComponentProp[boolName] = !C.ComponentProp[boolName];
                C.Save();
            }

            if (!help.IsNullOrEmpty())
            {
                ImGui.SameLine();
                ImGuiComponents.HelpMarker(help);
            }
        }

        private void SecondaryRadioButtons(string boolName1, string boolName2, string buttonLabel1, string buttonLabel2, string help = "")
        {
            var cbool1 = C.ComponentProp[boolName1];
            var cbool2 = C.ComponentProp[boolName2];

            ImGui.AlignTextToFramePadding();
            ImGui.BulletText("");

            ImGui.SameLine();
            if (ImGui.RadioButton($"{buttonLabel1}##{buttonLabel2}", cbool1))
            {
                C.ComponentProp[boolName1] = true;
                C.ComponentProp[boolName2] = false;
                C.Save();
            }

            ImGui.SameLine();
            if (ImGui.RadioButton($"{buttonLabel2}##{buttonLabel1}", cbool2))
            {
                C.ComponentProp[boolName1] = false;
                C.ComponentProp[boolName2] = true;
                C.Save();
            }

            if (!help.IsNullOrEmpty())
            {
                ImGui.SameLine();
                ImGuiComponents.HelpMarker(help);
            }
        }

        private void SecondaryRadioButtons(string propertyName, string buttonLabel1, string buttonLabel2, string help = "")
        {
            var propertyValue = typeof(Configuration).GetProperty(propertyName)?.GetValue(C);

            if (propertyValue is null) return;

            var cbool1 = (int)propertyValue == 0;
            var cbool2 = (int)propertyValue == 1;

            ImGui.AlignTextToFramePadding();
            ImGui.BulletText("");

            ImGui.SameLine();
            if (ImGui.RadioButton($"{buttonLabel1}##{buttonLabel2}", cbool1))
            {
                typeof(Configuration).GetProperty(propertyName)?.SetValue(C, 0);
                C.Save();
            }

            ImGui.SameLine();
            if (ImGui.RadioButton($"{buttonLabel2}##{buttonLabel1}", cbool2))
            {
                typeof(Configuration).GetProperty(propertyName)?.SetValue(C, 1);
                C.Save();
            }

            if (!help.IsNullOrEmpty())
            {
                ImGui.SameLine();
                ImGuiComponents.HelpMarker(help);
            }
        }

        private void NoteContentInputText(string key, string[]? paramsEP)
        {
            var textToShow = C.CustomNoteContents.TryGetValue(key, out var value) ? value : Service.Lang.GetOrigText(key);

            ImGui.AlignTextToFramePadding();
            ImGui.BulletText("");

            ImGui.SameLine();
            ImGui.Text($"{Service.Lang.GetText("Note")}:");

            ImGui.SameLine();
            ImGui.SetNextItemWidth(270f);
            if (ImGui.InputText($"##{key}", ref textToShow, 50))
            {
                C.CustomNoteContents[key] = textToShow;
                C.Save();
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                if (!textToShow.IsNullOrEmpty())
                {
                    ImGui.Text(textToShow);
                    if (paramsEP != null)
                    {
                        ImGui.Separator();
                        for (var i = 0; i < paramsEP.Length; i++)
                        {
                            ImGui.Text("{" + i + "}" + $" - {paramsEP[i]}");
                        }
                    }
                }
                ImGui.EndTooltip();
            }

            ImGui.SameLine();
            if (IconButton(FontAwesomeIcon.Sync, Service.Lang.GetText("Reset"), $"ResetContent_{key}"))
            {
                C.CustomNoteContents[key] = Service.Lang.GetOrigText(key);
                C.Save();
            }
        }

        public void Dispose()
        {
        }
    }
}
