using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;

namespace CurrencyTracker.Manager.Trackers
{
    public partial class Tracker : IDisposable
    {
        // 是否正在玩九宫幻卡 Is Playing Triple Triad
        private bool isTTOn = false;

        // 九宫幻卡结果 Triple Triad Result
        private string ttResultText = string.Empty;

        public void InitTripleTriad()
        {
            // 一启用插件就在玩九宫幻卡的情况
            var TTGui = Service.GameGui.GetAddonByName("TripleTriad");
            if (TTGui != nint.Zero)
            {
                isTTOn = true;
                if (Service.TargetManager.Target != null)
                {
                    currentTargetName = Service.TargetManager.Target.Name.TextValue;
                }
                DebindChatEvent();
                Service.PluginLog.Debug("Triple Triad Starts");
            }

            Service.AddonLifecycle.RegisterListener(AddonEvent.PreSetup, "TripleTriad", TripleTriadCheck);
        }

        private void TripleTriadCheck(AddonEvent type, AddonArgs args)
        {
            isTTOn = true;
            if (Service.TargetManager.Target != null)
            {
                currentTargetName = Service.TargetManager.Target.Name.TextValue;
            }
            DebindChatEvent();
            Service.PluginLog.Debug("Triple Triad Starts");
        }

        // 九宫幻卡 Triple Triad
        private void TripleTriad()
        {
            if (!C.RecordTripleTriad) return;

            var TTGui = Service.GameGui.GetAddonByName("TripleTriad");
            var TTRGui = Service.GameGui.GetAddonByName("TripleTriadResult");

            if (TTRGui != nint.Zero)
            {
                unsafe
                {
                    var TTR = (AtkUnitBase*)TTRGui;
                    if (TTR != null && TTR->RootNode != null && TTR->RootNode->ChildNode != null && TTR->UldManager.NodeList != null && ttResultText.IsNullOrEmpty())
                    {
                        var draw = (TTR->GetTextNodeById(5))->AtkResNode.NodeFlags.HasFlag(NodeFlags.Visible);
                        var lose = (TTR->GetTextNodeById(4))->AtkResNode.IsVisible;
                        var win = (TTR->GetTextNodeById(3))->AtkResNode.IsVisible;

                        ttResultText = draw ? TTR->GetTextNodeById(5)->NodeText.ToString() :
                                     lose ? TTR->GetTextNodeById(4)->NodeText.ToString() :
                                     win ? TTR->GetTextNodeById(3)->NodeText.ToString() : "";
                        Service.PluginLog.Debug(ttResultText);
                    }
                }
            }

            // 九宫幻卡结束 Triple Triad Ends
            if ((TTRGui == nint.Zero && TTGui == nint.Zero && isTTOn) || (TTRGui != nint.Zero && TTGui == nint.Zero && isTTOn))
            {
                isTTOn = false;

                foreach (var currency in C.AllCurrencies)
                {
                    CheckCurrency(currency.Value, "", $"({(!ttResultText.IsNullOrEmpty() ? $"[{ttResultText}]" : "")}{Service.Lang.GetText("TripleTriadWith", currentTargetName)})");
                }

                currentTargetName = string.Empty;
                ttResultText = string.Empty;

                Service.Chat.ChatMessage += OnChatMessage;
                Service.PluginLog.Debug("Triple Triad Ends");
            }
        }

        public void UninitTripleTriad()
        {
            isTTOn = false;
            ttResultText = string.Empty;

            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreSetup, "TripleTriad", TripleTriadCheck);
        }
    }
}
