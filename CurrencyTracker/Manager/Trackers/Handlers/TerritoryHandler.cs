using System.Collections.Generic;
using System.Linq;
using CurrencyTracker.Infos;
using Lumina.Excel.GeneratedSheets2;

namespace CurrencyTracker.Manager.Trackers.Handlers;

public class TerritoryHandler : ITrackerHandler
{
    public bool Initialized { get; set; }
    public bool isBlocked { get; set; }
    public static string CurrentLocationName { get; set; } = string.Empty;
    public static uint CurrentLocationID { get; set; }
    public static string PreviousLocationName { get; set; } = string.Empty;
    public static uint PreviousLocationID { get; set; }

    internal static Dictionary<uint, string> TerritoryNames = new();

    public void Init()
    {
        TerritoryNames = Service.DataManager.GetExcelSheet<TerritoryType>()
                                .Where(x => !string.IsNullOrEmpty(x.PlaceName?.Value?.Name?.ToString()))
                                .ToDictionary(
                                    x => x.RowId,
                                    x => P.PluginInterface.Sanitizer.Sanitize(
                                        x.PlaceName?.Value?.Name?.ToString()));

        PreviousLocationID = CurrentLocationID = Service.ClientState.TerritoryType;
        PreviousLocationName = CurrentLocationName =
                                   TerritoryNames.TryGetValue(CurrentLocationID, out var currentLocation)
                                       ? currentLocation
                                       : Service.Lang.GetText("UnknownLocation");

        Service.ClientState.TerritoryChanged += OnZoneChange;
    }

    private void OnZoneChange(ushort zone)
    {
        if (isBlocked) return;

        PreviousLocationID = CurrentLocationID;
        PreviousLocationName = CurrentLocationName;

        CurrentLocationID = zone;
        CurrentLocationName = TerritoryNames.TryGetValue(CurrentLocationID, out var currentLocation)
                                  ? currentLocation
                                  : Service.Lang.GetText("UnknownLocation");
    }

    public void Uninit()
    {
        Service.ClientState.TerritoryChanged -= OnZoneChange;

        TerritoryNames.Clear();
        PreviousLocationID = CurrentLocationID = 0;
        PreviousLocationName = CurrentLocationName = string.Empty;
    }
}
