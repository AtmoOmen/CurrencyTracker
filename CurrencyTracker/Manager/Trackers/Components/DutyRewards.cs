using Dalamud.Game.ClientState.Conditions;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CurrencyTracker.Manager.Trackers
{
    public partial class Tracker : IDisposable
    {
        public static bool IsBoundByDuty()
        {
            return Service.Condition[ConditionFlag.BoundByDuty] ||
                   Service.Condition[ConditionFlag.BoundByDuty56] ||
                   Service.Condition[ConditionFlag.BoundByDuty95];
        }

        private static readonly string[] DutyEndStrings = new[] { "任务结束了", "has ended", "の攻略を終了した", "wurde beendet", "prend fin" };

        // Terriory ID - ContentName
        public static Dictionary<uint, string> ContentNames = new();

        // Terroiry ID - PVPNames
        public static Dictionary<uint, string> PVPNames = new();

        private bool DutyStarted = false;

        private string dutyLocationName = string.Empty;

        private string dutyContentName = string.Empty;

        public void InitDutyRewards()
        {
            PVPNames = Service.DataManager.GetExcelSheet<ContentFinderCondition>()
                .Where(x => !string.IsNullOrEmpty(x.Name.ToString()) && (x.AcceptClassJobCategory.Row == 146) && x.PvP)
                .GroupBy(x => x.TerritoryType.Row)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().Name.ToString()
                );

            ContentNames = Service.DataManager.GetExcelSheet<ContentFinderCondition>()
                .Where(x => !string.IsNullOrEmpty(x.Name.ToString()) && ((x.AcceptClassJobCategory.Row == 146 && x.PvP) || (x.AcceptClassJobCategory.Row == 108 || x.AcceptClassJobCategory.Row == 142) && !x.PvP))
                .GroupBy(x => x.TerritoryType.Row)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().Name.ToString()
                );

            // 上线就在副本里的情况 Player is already in Duty when logs in
            if (IsBoundByDuty())
            {
                dutyLocationName = TerritoryNames.TryGetValue(Service.ClientState.TerritoryType, out var currentLocation) ? currentLocation : Service.Lang.GetText("UnknownLocation");
                dutyContentName = ContentNames.TryGetValue(Service.ClientState.TerritoryType, out var currentContent) ? currentContent : Service.Lang.GetText("UnknownContent");
                DutyStarted = true;
                DebindChatEvent();
            }

            Service.DutyState.DutyStarted += isDutyStarted;
        }

        private void DutyEndCheck(string ChatMessage)
        {
            if (!DutyStarted)
            {
                return;
            }
            if (DutyEndStrings.Any(ChatMessage.Contains) || ChatMessage == "PVPEnds")
            {
                Service.PluginLog.Debug("Duty Ends, Currency Change Check Starts.");
                foreach (var currency in C.PresetCurrencies.Values.Concat(C.CustomCurrencies.Values))
                {
                    CheckCurrency(currency, true, dutyLocationName, C.RecordContentName ? $"({dutyContentName})" : "-1");
                }

                DutyStarted = false;
                dutyLocationName = string.Empty;
                dutyContentName = string.Empty;
                Service.Chat.ChatMessage += OnChatMessage;

                Service.PluginLog.Debug("Currency Change Check Completes.");
            }
        }

        public void UninitDutyRewards()
        {
            DutyStarted = false;
            dutyLocationName = string.Empty;
            dutyContentName = string.Empty;

            Service.DutyState.DutyStarted -= isDutyStarted;
        }
    }
}
