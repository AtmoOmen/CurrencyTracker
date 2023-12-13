namespace CurrencyTracker.Windows
{
    public class RecordSettings : Window, IDisposable
    {
        private Configuration? C = Plugin.Configuration;
        private Plugin? P = Plugin.Instance;

        public RecordSettings(Plugin plugin) : base($"Record Settings##{Plugin.Name}")
        {
            Flags |= ImGuiWindowFlags.NoScrollbar;
            Flags |= ImGuiWindowFlags.AlwaysAutoResize;
        }

        public override void Draw()
        {
            if (!P.Main.IsOpen) IsOpen = false;

            if (ImGui.BeginTabBar("NoteSettings", ImGuiTabBarFlags.AutoSelectNewTabs))
            {
                // 一般 General
                if (ImGui.BeginTabItem(Service.Lang.GetText("General")))
                {
                    // 交换 Exchange
                    ModuleCheckbox(typeof(Exchange), "Exchange", Service.Lang.GetText("Exchange-RecordExchangeResult"));
                    ModuleCheckbox(typeof(SpecialExchange), "SpecialExchange", Service.Lang.GetText("SpecialExchange-RecordSpecialExchangeResult"));
                    if (C.ComponentEnabled["Exchange"] || C.ComponentEnabled["SpecialExchange"]) NoteContentInputText("ExchangeWith", new string[1] { Service.Lang.GetText("ParamEP-TargetName") });

                    // 传送 Teleport
                    ModuleCheckbox(typeof(TeleportCosts), "TeleportCosts", Service.Lang.GetText("TeleportCosts-RecordTPCosts"));
                    if (C.ComponentEnabled["TeleportCosts"]) SecondaryRadioButtons("RecordDesAetheryteName", "RecordDesAreaName", Service.Lang.GetText("TeleportCosts-RecordAetheryteName"), Service.Lang.GetText("TeleportCosts-RecordAreaName"));
                    ModuleCheckbox(typeof(WarpCosts), "WarpCosts", Service.Lang.GetText("WarpCosts-RecordTPCosts"));
                    if (C.ComponentEnabled["TeleportCosts"] || C.ComponentEnabled["WarpCosts"]) NoteContentInputText("TeleportTo", new string[1] { Service.Lang.GetText("ParamEP-DestinationName") });
                    if (C.ComponentEnabled["WarpCosts"]) NoteContentInputText("TeleportWithinArea", null);

                    // 任务 Quest
                    ModuleCheckbox(typeof(QuestRewards), "QuestRewards", Service.Lang.GetText("QuestRewards-RecordQuestRewards"));
                    if (C.ComponentEnabled["QuestRewards"]) NoteContentInputText("Quest", new string[1] { Service.Lang.GetText("ParamEP-QuestName") });

                    // 交易 Trade
                    ModuleCheckbox(typeof(Trade), "Trade", Service.Lang.GetText("Trade-RecordTradeTarget"));
                    if (C.ComponentEnabled["Trade"]) NoteContentInputText("Quest", new string[1] { Service.Lang.GetText("ParamEP-QuestName") });

                    // 无人岛 Island Sanctuary
                    ModuleCheckbox(typeof(IslandSanctuary), "IslandSanctuary", Service.Lang.GetText("IslandSanctuary-RecordISResult"));
                    if (C.ComponentEnabled["IslandSanctuary"])
                    {
                        NoteContentInputText("IslandFarm", null);
                        NoteContentInputText("IslandPasture", null);
                        NoteContentInputText("IslandWorkshop", null);
                    }

                    ImGui.EndTabItem();
                }

                // 存储 Inventory
                if (ImGui.BeginTabItem(Service.Lang.GetText("Inventory")))
                {
                    ModuleCheckbox(typeof(Retainer), "Retainer", Service.Lang.GetText("Retainer-RecordRetainerInventory"));
                    ModuleCheckbox(typeof(SaddleBag), "SaddleBag", Service.Lang.GetText("SaddleBag-RecordSaddleBag"));
                    ModuleCheckbox(typeof(PremiumSaddleBag), "PremiumSaddleBag", Service.Lang.GetText("PremiumSaddleBag-RecordPremiumSaddleBag"));
                    ImGui.EndTabItem();
                }

                // 战斗 Combat
                if (ImGui.BeginTabItem(Service.Lang.GetText("Combat")))
                {
                    ModuleCheckbox(typeof(DutyRewards), "DutyRewards", Service.Lang.GetText("DutyRewards-RecordDutyRewards"));
                    if (C.ComponentEnabled["DutyRewards"]) SecondaryCheckbox("RecordContentName", Service.Lang.GetText("DutyRewards-RecordContentName"));

                    ModuleCheckbox(typeof(FateRewards), "FateRewards", Service.Lang.GetText("FateRewards-RecordFateRewards"));
                    if (C.ComponentEnabled["FateRewards"]) NoteContentInputText("Fate", new string[1] { Service.Lang.GetText("ParamEP-FateName") });

                    ModuleCheckbox(typeof(MobDrops), "MobDrops", Service.Lang.GetText("MobDrops-RecordMobDrops"));
                    if (C.ComponentEnabled["MobDrops"]) NoteContentInputText("MobDrops-MobDropsNote", new string[1] { Service.Lang.GetText("ParamEP-MobNames") });

                    ImGui.EndTabItem();
                }

                // 金碟 Gold Saucer
                if (ImGui.BeginTabItem(Service.Lang.GetText("GoldSaucer")))
                {
                    ModuleCheckbox(typeof(GoldSaucer), "GoldSaucer", Service.Lang.GetText("GoladSaucer-RecordMGPSource"));
                    ModuleCheckbox(typeof(Manager.Trackers.Components.TripleTriad), "TripleTriad", Service.Lang.GetText("TripleTriad-RecordTTResult"));
                    if (C.ComponentEnabled["TripleTriad"]) NoteContentInputText("TripleTriadWith", new string[2] { Service.Lang.GetText("ParamEP-TTOutcome"), Service.Lang.GetText("ParamEP-TTRivalName") });

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

            if (ImGui.Checkbox($"{checkboxLabel}##{boolName}-{type.Name}", ref cbool))
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

        private void NoteContentInputText(string key, string[]? paramsEP)
        {
            var textToShow = C.CustomNoteContents.TryGetValue(key, out var value) ? value : Service.Lang.GetOrigText(key);

            ImGui.AlignTextToFramePadding();
            ImGui.BulletText("");

            ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.DalamudYellow, $"{Service.Lang.GetText("Note")}:");

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
            }
        }

        public void Dispose()
        {
        }
    }
}
