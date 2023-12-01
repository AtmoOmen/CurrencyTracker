namespace CurrencyTracker.Windows
{
    public class RecordSettings : Window, IDisposable
    {
        private Configuration? C = Plugin.Instance.Configuration;
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
                    ModuleCheckbox(typeof(Exchange), "Exchange", Service.Lang.GetText("Exchange-RecordExchangeResult"), Service.Lang.GetText("Exchange-RecordExchangeResultHelp"));
                    ModuleCheckbox(typeof(SpecialExchange), "SpecialExchange", Service.Lang.GetText("SpecialExchange-RecordSpecialExchangeResult"), Service.Lang.GetText("SpecialExchange-RecordSpecialExchangeResultHelp"));
                    ModuleCheckbox(typeof(TeleportCosts), "TeleportCosts", Service.Lang.GetText("TeleportCosts-RecordTPCosts"), Service.Lang.GetText("TeleportCosts-RecordTPCostsHelp"));
                    if (C.ComponentEnabled["TeleportCosts"]) SecondaryRadioButtons("RecordDesAetheryteName", "RecordDesAreaName", Service.Lang.GetText("TeleportCosts-RecordAetheryteName"), Service.Lang.GetText("TeleportCosts-RecordAreaName"));
                    ModuleCheckbox(typeof(WarpCosts), "WarpCosts", Service.Lang.GetText("WarpCosts-RecordTPCosts"), Service.Lang.GetText("WarpCosts-RecordTPCostsHelp"));
                    ModuleCheckbox(typeof(QuestRewards), "QuestRewards", Service.Lang.GetText("QuestRewards-RecordQuestRewards"), Service.Lang.GetText("QuestRewards-RecordQuestRewardsHelp"));
                    ModuleCheckbox(typeof(Trade), "Trade", Service.Lang.GetText("Trade-RecordTradeTarget"), Service.Lang.GetText("Trade-RecordTradeTargetHelp"));
                    ModuleCheckbox(typeof(FateRewards), "FateRewards", Service.Lang.GetText("FateRewards-RecordFateRewards"), Service.Lang.GetText("FateRewards-RecordFateRewardsHelp"));
                    ModuleCheckbox(typeof(MobDrops), "MobDrops", Service.Lang.GetText("MobDrops-RecordMobDrops"), Service.Lang.GetText("MobDrops-RecordMobDropsHelp"));
                    ModuleCheckbox(typeof(IslandSanctuary), "IslandSanctuary", Service.Lang.GetText("IslandSanctuary-RecordISResult"), Service.Lang.GetText("IslandSanctuary-RecordISResultHelp"));

                    ImGui.EndTabItem();
                }

                // 副本 Content/Duty
                if (ImGui.BeginTabItem(Service.Lang.GetText("Content")))
                {
                    ModuleCheckbox(typeof(DutyRewards), "DutyRewards", Service.Lang.GetText("DutyRewards-RecordDutyRewards"), Service.Lang.GetText("DutyRewards-RecordDutyRewardsHelp"));
                    if (C.ComponentEnabled["DutyRewards"]) SecondaryCheckbox("RecordContentName", Service.Lang.GetText("DutyRewards-RecordContentName"), Service.Lang.GetText("DutyRewards-RecordContentNameHelp"));

                    ImGui.EndTabItem();
                }

                // 金碟 Gold Saucer
                if (ImGui.BeginTabItem(Service.Lang.GetText("GoldSaucer")))
                {
                    ModuleCheckbox(typeof(GoldSaucer), "GoldSaucer", Service.Lang.GetText("GoladSaucer-RecordMGPSource"), Service.Lang.GetText("GoladSaucer-RecordMGPSourceHelp"));
                    ModuleCheckbox(typeof(Manager.Trackers.Components.TripleTriad), "TripleTriad", Service.Lang.GetText("TripleTriad-RecordTTResult"), Service.Lang.GetText("TripleTriad-RecordTTResultHelp"));
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }

        private void ModuleCheckbox(Type type, string boolName, string checkboxLabel, string help = "")
        {
            var cbool = C.ComponentEnabled[boolName];

            if (typeof(ITrackerComponent).IsAssignableFrom(type))
            {
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
                        Service.Log.Error($"Fail to fetch component {component.GetType().Name}");
                    }

                    C.Save();
                }

                if (!help.IsNullOrEmpty())
                {
                    ImGui.SameLine();
                    ImGuiComponents.HelpMarker(help);
                }
            }
            else
            {
                Service.Log.Error($"Fail to fetch component {type.Name}");
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

        public void Dispose()
        {
        }
    }
}
