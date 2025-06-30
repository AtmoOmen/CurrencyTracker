using System.Collections.Generic;
using System.Linq;
using CurrencyTracker.Infos;
using CurrencyTracker.Trackers;
using Lumina.Excel.Sheets;

namespace CurrencyTracker.Manager.Trackers.Handlers;

public class TerritoryHandler : TrackerHandlerBase
{
    public static string CurrentLocationName  { get; private set; } = string.Empty;
    public static uint   CurrentLocationID    { get; private set; }
    public static string PreviousLocationName { get; private set; } = string.Empty;
    public static uint   PreviousLocationID   { get; private set; }

    public static Dictionary<uint, string>? TerritoryNames;

    protected override void OnInit()
    {
        LoadTerritoryNames();
        InitLocation();
        DService.ClientState.TerritoryChanged += OnZoneChange;
    }

    private static void LoadTerritoryNames()
    {
        TerritoryNames ??= LuminaGetter.Get<TerritoryType>()
                                  .Select(x => new
                                  {
                                      ZoneID = x.RowId,
                                      PlaceName = x.PlaceName.ValueNullable?.Name.ToString() ?? string.Empty,
                                  })
                                  .Where(x => !string.IsNullOrWhiteSpace(x.PlaceName))
                                  .ToDictionary(x => x.ZoneID, x => x.PlaceName);
    }

    private static void InitLocation()
    {
        CurrentLocationID = PreviousLocationID = DService.ClientState.TerritoryType;
        CurrentLocationName = PreviousLocationName = GetLocationName(CurrentLocationID);
    }

    private void OnZoneChange(ushort zone)
    {
        if (IsBlocked) return;

        PreviousLocationID = CurrentLocationID;
        PreviousLocationName = CurrentLocationName;
        CurrentLocationID = zone;
        CurrentLocationName = GetLocationName(CurrentLocationID);
    }

    private static string GetLocationName(uint locationId) =>
        TerritoryNames.TryGetValue(locationId, out var name) ? name : Service.Lang.GetText("UnknownLocation");

    private static void ResetLocations()
    {
        PreviousLocationID = CurrentLocationID = 0;
        PreviousLocationName = CurrentLocationName = string.Empty;
    }

    protected override void OnUninit()
    {
        DService.ClientState.TerritoryChanged -= OnZoneChange;
        TerritoryNames?.Clear();
        TerritoryNames = null;
        ResetLocations();
    }
}
