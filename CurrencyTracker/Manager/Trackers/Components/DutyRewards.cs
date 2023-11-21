using CurrencyTracker.Manager.Libs;
using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CurrencyTracker.Manager.Trackers
{
    public class DutyRewards : ITrackerComponent
    {
        // Territory ID - ContentName
        private static Dictionary<uint, string> ContentNames = new();

        // Contents that Should be Ignored
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
        private string contentName = string.Empty;

        public DutyRewards() 
        {
            Init();
        }

        public void Init()
        {
            ContentNames = Service.DataManager.GetExcelSheet<ContentFinderCondition>()
                .Where(x => !x.Name.ToString().IsNullOrEmpty() && !IgnoredContents.Any(y => y == x.TerritoryType.Row))
                .GroupBy(x => x.TerritoryType.Row)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().Name.ToString()
                );

            if (Flags.IsBoundByDuty())
            {
                CheckDutyStart();
            }

            Service.ClientState.TerritoryChanged += OnZoneChange;
        }

        private void CheckDutyStart()
        {
            if (isDutyStarted) return;

            if (ContentNames.TryGetValue(TerrioryHandler.CurrentLocationID, out var dutyName))
            {
                Service.Tracker.ChatHandler.isBlocked = true;

                isDutyStarted = true;
                contentName = !dutyName.IsNullOrEmpty() ? dutyName : Service.Lang.GetText("UnknownContent");

                Service.PluginLog.Debug($"Duty {dutyName} Starts");
            }
        }

        private void OnZoneChange(ushort obj)
        {
            if (Flags.IsBoundByDuty())
            {
                CheckDutyStart();
            }
            else if (isDutyStarted)
            {
                CheckDutyEnd();
            }
        }

        private void CheckDutyEnd()
        {
            if (!isDutyStarted) return;

            Service.PluginLog.Debug($"Duty {contentName} Ends, Currency Change Check Starts.");

            Parallel.ForEach(Plugin.Instance.Configuration.AllCurrencies, currency =>
            {
                Service.Tracker.CheckCurrency(currency.Value, TerrioryHandler.PreviousLocationName, Plugin.Instance.Configuration.RecordContentName ? $"({contentName})" : "");
            });


            isDutyStarted = false;
            contentName = string.Empty;

            Service.Tracker.ChatHandler.isBlocked = false;
            Service.PluginLog.Debug("Currency Change Check Completes.");
        }


        public void Uninit()
        {
            isDutyStarted = false;
            contentName = string.Empty;

            Service.ClientState.TerritoryChanged -= OnZoneChange;
        }
    }
}
