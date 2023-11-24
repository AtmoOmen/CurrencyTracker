namespace CurrencyTracker.Manager.Trackers
{
    public class TerrioryHandler : ITrackerHandler
    {
        public bool Initialized
        {
            get { return _initialized; }
            set { _initialized = value; }
        }

        public bool isBlocked
        {
            get { return _isBlocked; }
            set { _isBlocked = value; }
        }

        public static string CurrentLocationName
        {
            get { return _currentLocationName; }
            set { _currentLocationName = value; }
        }

        public static uint CurrentLocationID
        {
            get { return _currentLocationID; }
            set { _currentLocationID = value; }
        }

        public static string PreviousLocationName
        {
            get { return _previousLocationName; }
            set { _previousLocationName = value; }
        }

        public static uint PreviousLocationID
        {
            get { return _previousLocationID; }
            set { _previousLocationID = value; }
        }

        private bool _initialized = false;
        private bool _isBlocked = false;
        private static string _currentLocationName = string.Empty;
        private static uint _currentLocationID = 0;
        private static string _previousLocationName = string.Empty;
        private static uint _previousLocationID = 0;

        public void Init()
        {
            PreviousLocationID = CurrentLocationID = Service.ClientState.TerritoryType;
            PreviousLocationName = CurrentLocationName = Tracker.TerritoryNames.TryGetValue(CurrentLocationID, out var currentLocation) ? currentLocation : Service.Lang.GetText("UnknownLocation");

            Service.ClientState.TerritoryChanged += OnZoneChange;

            _initialized = true;
        }

        private void OnZoneChange(ushort obj)
        {
            if (_isBlocked) return;

            PreviousLocationID = CurrentLocationID;
            PreviousLocationName = CurrentLocationName;

            CurrentLocationID = Service.ClientState.TerritoryType;
            CurrentLocationName = Tracker.TerritoryNames.TryGetValue(CurrentLocationID, out var currentLocation) ? currentLocation : Service.Lang.GetText("UnknownLocation");
        }

        public void Uninit()
        {
            Service.ClientState.TerritoryChanged -= OnZoneChange;

            PreviousLocationID = CurrentLocationID = 0;
            PreviousLocationName = CurrentLocationName = string.Empty;

            _initialized = false;
        }
    }
}
