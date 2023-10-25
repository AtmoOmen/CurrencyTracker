using Dalamud.Game.ClientState.Conditions;
using Dalamud.Utility;
using System;

namespace CurrencyTracker.Manager.Trackers
{
    public partial class Tracker : IDisposable
    {
        // 仙人微彩 Mini Cactpot
        private bool isMCOn = false;

        // 九宫幻卡 Triple Triad
        private bool isTTOn = false;


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
                    CheckCurrency(29, false, currentLocationName, $"({Service.Lang.GetText("TripleTriad")} {Service.Lang.GetText("With")} {currentTargetName})");
                }
                else
                {
                    CheckCurrency(29, false, currentLocationName, $"({Service.Lang.GetText("TripleTriad")})");
                }

                currentTargetName = string.Empty;
                isTTOn = false;

                Service.Chat.ChatMessage += OnChatMessage;
                Service.PluginLog.Debug("Triple Triad Ends");
            }
        }

        // 仙人微彩 Mini Cactpot
        private void MiniCactpot()
        {
            if (!C.RecordMiniCactpot) return;

            var MCGui = Service.GameGui.GetAddonByName("LotteryDaily");

            if (MCGui != nint.Zero)
            {
                isMCOn = true;
                Service.Chat.ChatMessage -= OnChatMessage;
            }
            else if (MCGui == nint.Zero && isMCOn)
            {
                if (!Service.Condition[ConditionFlag.OccupiedInQuestEvent])
                {
                    return;
                }
                Service.PluginLog.Debug("仙人微彩结束");
                isMCOn = false;

                CheckCurrency(29, true, previousLocationName, $"({Service.Lang.GetText("MiniCactpot")})");

                Service.Chat.ChatMessage += OnChatMessage;
            }
        }
    }
}
