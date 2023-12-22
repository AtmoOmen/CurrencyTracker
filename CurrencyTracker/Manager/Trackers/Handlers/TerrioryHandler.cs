using Lumina.Excel.GeneratedSheets;

namespace CurrencyTracker.Manager.Trackers
{
    public class TerrioryHandler : ITrackerHandler
    {
        public bool Initialized { get; set; }
        public bool isBlocked { get; set; }
        public static string CurrentLocationName { get; set; } = string.Empty;
        public static uint CurrentLocationID { get; set; } = 0;
        public static string PreviousLocationName { get; set; } = string.Empty;
        public static uint PreviousLocationID { get; set; } = 0;

        internal static Dictionary<uint, string> TerritoryNames = new();

        public void Init()
        {
            TerritoryNames = Service.DataManager.GetExcelSheet<TerritoryType>()
                .Where(x => !string.IsNullOrEmpty(x.PlaceName?.Value?.Name?.ToString()))
                .ToDictionary(
                    x => x.RowId,
                    x => Plugin.Instance.PluginInterface.Sanitizer.Sanitize(x.PlaceName?.Value?.Name?.ToString()));

            PreviousLocationID = CurrentLocationID = Service.ClientState.TerritoryType;
            PreviousLocationName = CurrentLocationName = TerritoryNames.TryGetValue(CurrentLocationID, out var currentLocation) ? currentLocation : Service.Lang.GetText("UnknownLocation");

            Service.ClientState.TerritoryChanged += OnZoneChange;

            Initialized = true;
        }

        private void OnZoneChange(ushort obj)
        {
            if (isBlocked) return;

            PreviousLocationID = CurrentLocationID;
            PreviousLocationName = CurrentLocationName;

            CurrentLocationID = Service.ClientState.TerritoryType;
            CurrentLocationName = TerritoryNames.TryGetValue(CurrentLocationID, out var currentLocation) ? currentLocation : Service.Lang.GetText("UnknownLocation");
        }

        public void Uninit()
        {
            Service.ClientState.TerritoryChanged -= OnZoneChange;

            TerritoryNames.Clear();
            PreviousLocationID = CurrentLocationID = 0;
            PreviousLocationName = CurrentLocationName = string.Empty;

            Initialized = false;
        }
    }
}
