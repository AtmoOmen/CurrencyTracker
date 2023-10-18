using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;

namespace CurrencyTracker.Windows
{
    public class RecordSettings : Window, IDisposable
    {
        private Configuration? C = Plugin.Instance.Configuration;
        private Plugin? P = Plugin.Instance;

        private bool isRecordContentName;
        private bool isRecordTeleportDes;
        private bool isRecordTeleport;
        private bool isTrackinDuty;
        private bool isWaitExComplete;

        public RecordSettings(Plugin plugin) : base("Currency Tracker - Record Settings")
        {
            Flags |= ImGuiWindowFlags.NoScrollbar;
            Flags |= ImGuiWindowFlags.AlwaysAutoResize;

            InitOptions();
        }

        public override void Draw()
        {
            if (ImGui.BeginTabBar("NoteSettings", ImGuiTabBarFlags.AutoSelectNewTabs))
            {
                if (ImGui.BeginTabItem("一般"))
                {
                    if (ImGui.Checkbox("记录传送费", ref isRecordTeleport))
                    {
                        C.RecordTeleport = isRecordTeleport;
                        C.Save();

                        if (isRecordTeleport)
                        {
                            Service.Tracker.UninitTeleportCosts();
                            Service.Tracker.InitTeleportCosts();
                        }
                        else
                        {
                            Service.Tracker.UninitTeleportCosts();
                        }
                    }

                    if (isRecordTeleport)
                    {
                        ImGui.BulletText("");
                        ImGui.SameLine();
                        if (ImGui.Checkbox("记录传送地点", ref isRecordTeleportDes))
                        {
                            C.RecordTeleportDes = isRecordTeleportDes;
                            C.Save();
                        }
                    }

                    if (ImGui.Checkbox("等待交易完成", ref isWaitExComplete))
                    {
                        C.WaitExComplete = isWaitExComplete;
                        C.Save();
                    }

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("副本"))
                {
                    if (ImGui.Checkbox(Service.Lang.GetText("TrackInDuty"), ref isTrackinDuty))
                    {
                        C.TrackedInDuty = isTrackinDuty;
                        C.Save();
                    }
                    ImGui.SameLine();
                    ImGuiComponents.HelpMarker(Service.Lang.GetText("TrackInDutyHelp"));

                    if (isTrackinDuty)
                    {
                        ImGui.BulletText("");
                        ImGui.SameLine();
                        if (ImGui.Checkbox("记录副本名称", ref isRecordContentName))
                        {
                            C.RecordContentName = isRecordContentName;
                            C.Save();
                        }
                    }

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("金碟"))
                {
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }

        private void InitOptions()
        {
            isRecordContentName = C.RecordContentName;
            isRecordTeleportDes = C.RecordTeleportDes;
            isRecordTeleport = C.RecordTeleport;
            isTrackinDuty = C.TrackedInDuty;
            isWaitExComplete = C.WaitExComplete;
        }

        public void Dispose()
        {
        }
    }
}
