using Dalamud.Game.ClientState.Conditions;
using Dalamud.Utility;
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

        // 应被忽略的副本 Contents should be ignored
        public static uint[] IgnoredContents = new uint[]
        {
            // 九宫幻卡相关 Triple Triad Related
            579, 940, 941,
            // 禁地优雷卡 Eureka
            732, 763, 795, 827,
            // 博兹雅 Bozja
            920, 975
        };

        private bool DutyStarted = false;

        private string dutyLocationName = string.Empty;

        private string dutyContentName = string.Empty;

        public void InitDutyRewards()
        {
            ContentNames = Service.DataManager.GetExcelSheet<ContentFinderCondition>()
                .Where(x => !x.Name.ToString().IsNullOrEmpty() && !IgnoredContents.Any(y => y == x.TerritoryType.Row))
                .GroupBy(x => x.TerritoryType.Row)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().Name.ToString()
                );

            // 上线就在副本里的情况 Player is already in Duty when logs in
            if (IsBoundByDuty())
            {
                DutyStartCheck();
            }
        }
        
        private void DutyStartCheck()
        {
            if (!C.TrackedInDuty) return;
            if (ContentNames.TryGetValue(Service.ClientState.TerritoryType, out var DutyName))
            {
                DutyStarted = true;
                dutyLocationName = TerritoryNames.TryGetValue(Service.ClientState.TerritoryType, out var currentLocation) ? currentLocation : Service.Lang.GetText("UnknownLocation");
                dutyContentName = !DutyName.IsNullOrEmpty() ? DutyName : Service.Lang.GetText("UnknownContent");

                DebindChatEvent();
                Service.PluginLog.Debug($"Duty {DutyName} Starts");
            }
        }

        private void DutyEndCheck(string ChatMessage)
        {
            if (!DutyStarted)
            {
                return;
            }
            if (DutyEndStrings.Any(ChatMessage.Contains))
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
        }
    }
}
