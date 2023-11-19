using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CurrencyTracker.Manager.Trackers
{
    public partial class Tracker : IDisposable
    {
        // Territory ID - ContentName
        private static Dictionary<uint, string> ContentNames = new();

        // Contents that should be ignored
        private static readonly uint[] IgnoredContents =
        {
            // Triple Triad Related
            579, 940, 941,
            // Eureka
            732, 763, 795, 827,
            // Bozja
            920, 975
        };

        private bool isDutyStarted;
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
            if (Flags.IsBoundByDuty())
            {
                CheckDutyStart();
            }
        }

        private void CheckDutyStart()
        {
            if (isDutyStarted) return;

            if (ContentNames.TryGetValue(Service.ClientState.TerritoryType, out var dutyName))
            {
                DebindChatEvent();

                isDutyStarted = true;
                dutyLocationName = TerritoryNames.TryGetValue(Service.ClientState.TerritoryType, out var currentLocation) ? currentLocation : Service.Lang.GetText("UnknownLocation");
                dutyContentName = !dutyName.IsNullOrEmpty() ? dutyName : Service.Lang.GetText("UnknownContent");

                Service.PluginLog.Debug($"Duty {dutyName} Starts");
            }
        }

        private void CheckDutyEnd()
        {
            if (!isDutyStarted) return;

            Service.PluginLog.Debug($"Duty {dutyContentName} Ends, Currency Change Check Starts.");

            foreach (var currency in C.AllCurrencies)
            {
                CheckCurrency(currency.Value, dutyLocationName, C.RecordContentName ? $"({dutyContentName})" : "");
            }

            isDutyStarted = false;
            dutyLocationName = string.Empty;
            dutyContentName = string.Empty;

            Service.Chat.ChatMessage += OnChatMessage;
            Service.PluginLog.Debug("Currency Change Check Completes.");
        }

        public void UninitDutyRewards()
        {
            isDutyStarted = false;
            dutyLocationName = string.Empty;
            dutyContentName = string.Empty;
        }
    }
}
