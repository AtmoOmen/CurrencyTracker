namespace CurrencyTracker.Manager.Trackers.Components
{
    public class DutyRewards : ITrackerComponent
    {
        private bool _initialized = false;

        public bool Initialized
        {
            get { return _initialized; }
            set { _initialized = value; }
        }

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
            _initialized = true;
        }

        private void CheckDutyStart()
        {
            if (isDutyStarted) return;

            if (ContentNames.TryGetValue(TerrioryHandler.CurrentLocationID, out var dutyName))
            {
                HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = true;

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

            Service.Tracker.CheckAllCurrencies(PreviousLocationName, Plugin.Instance.Configuration.ComponentProp["RecordContentName"] ? $"({contentName})" : "", RecordChangeType.All, 2);

            isDutyStarted = false;
            contentName = string.Empty;

            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = false;
            Service.PluginLog.Debug("Currency Change Check Completes.");
        }

        public void Uninit()
        {
            isDutyStarted = false;
            contentName = string.Empty;

            Service.ClientState.TerritoryChanged -= OnZoneChange;
            _initialized = false;
        }
    }
}
