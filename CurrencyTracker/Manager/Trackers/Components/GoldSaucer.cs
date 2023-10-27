using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Memory;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;

namespace CurrencyTracker.Manager.Trackers
{
    public partial class Tracker : IDisposable
    {
        // 九宫幻卡 Triple Triad
        private bool isTTOn = false;

        // 当前正在玩的游戏 Currently Playing Minigame
        private string GameName = string.Empty;

        internal void InitGoldSacuer()
        {
            GameName = string.Empty;
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "GoldSaucerReward", GoldSaucerMain);
        }

        // 九宫幻卡 Triple Triad
        private void TripleTriad()
        {
            if (!C.RecordTripleTriad) return;

            var TTGui = Service.GameGui.GetAddonByName("TripleTriad");
            var TTRGui = Service.GameGui.GetAddonByName("TripleTriadResult");
            
            // 九宫幻卡开始 Triple Triad Starts
            if (TTGui != nint.Zero && TTRGui == nint.Zero && !isTTOn)
            {
                isTTOn = true;
                if (Service.TargetManager.Target != null)
                {
                    currentTargetName = Service.TargetManager.Target.Name.TextValue;
                }
                Service.Chat.ChatMessage -= OnChatMessage;
                Service.PluginLog.Debug("Triple Triad Starts");
            }

            // 九宫幻卡结束 Triple Triad Ends
            if ((C.WaitExComplete && TTRGui == nint.Zero && TTGui == nint.Zero && isTTOn) || (!C.WaitExComplete && TTRGui != nint.Zero && TTGui == nint.Zero && isTTOn))
            {
                if (!currentTargetName.IsNullOrEmpty())
                {
                    CheckCurrency(29, false, "-1", $"({Service.Lang.GetText("TripleTriad")} {Service.Lang.GetText("With")} {currentTargetName})");
                }
                else
                {
                    CheckCurrency(29, false, "-1", $"({Service.Lang.GetText("TripleTriad")})");
                }

                currentTargetName = string.Empty;
                isTTOn = false;

                Service.Chat.ChatMessage += OnChatMessage;
                Service.PluginLog.Debug("Triple Triad Ends");
            }
        }

        // 金碟内的大部分处理逻辑 
        public void GoldSaucerMain(AddonEvent eventtype, AddonArgs args)
        {
            unsafe
            {
                var GSR = (AtkUnitBase*)Service.GameGui.GetAddonByName("GoldSaucerReward");
                if (GSR != null && GSR->RootNode != null && GSR->RootNode->ChildNode != null && GSR->UldManager.NodeList != null)
                {
                    var textNode = GSR->GetTextNodeById(5);
                    if (textNode != null)
                    {
                        GameName = textNode -> NodeText.ToString();
                        if (!GameName.IsNullOrEmpty())
                        {
                            Service.PluginLog.Debug(GameName);
                            CheckCurrency(29, false, "-1", $"({GameName})");

                            GameName = string.Empty;
                        }
                    }
                }
            }
        }

        internal void UninitGoldSacuer()
        {
            GameName = string.Empty;

            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostDraw, "GoldSaucerReward", GoldSaucerMain);
        }
    }
}
