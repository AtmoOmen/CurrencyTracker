using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using System;

namespace CurrencyTracker.Manager.Trackers
{
    public partial class Tracker : IDisposable
    {

        // 仙人微彩 Mini Cactpot
        private bool isMCOn = false;

        // 九宫幻卡 Triple Triad
        private bool isTTOn = false;

        private void InitGoldSaucer()
        {
        }


        // 九宫幻卡 Triple Triad
        private void TripleTriad()
        {
            if (!C.RecordTripleTriad) return;

            var TTGui = Service.GameGui.GetAddonByName("TripleTriad");
            var TTRGui = Service.GameGui.GetAddonByName("TripleTriadResult");

            if (TTGui != nint.Zero && TTRGui == nint.Zero && !isTTOn)
            {
                isTTOn = true;
                Service.PluginLog.Debug("九宫幻卡开始");
            }

            if (TTRGui != nint.Zero && TTGui == nint.Zero && isTTOn)
            {
                Service.PluginLog.Debug("九宫幻卡结束");
                CheckCurrency(29, true, previousLocationName, $"({Service.Lang.GetText("From")} {Service.Lang.GetText("TripleTriad")})");

                isTTOn = false;
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

                CheckCurrency(29, true, previousLocationName, $"({Service.Lang.GetText("From")} {Service.Lang.GetText("MiniCactpot")})");

                Service.Chat.ChatMessage += OnChatMessage;
            }
        }

        private void UninitGoldSaucer()
        {
        }
    }
}
