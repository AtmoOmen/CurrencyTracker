using System.Collections.Generic;
using System.Linq;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tools;
using CurrencyTracker.Manager.Trackers.Handlers;
using Lumina.Excel.Sheets;

namespace CurrencyTracker.Manager.Trackers.Components;

public class DutyRewards : ITrackerComponent
{
    public bool Initialized { get; set; }

    // Territory ID - ContentName
    private static readonly Dictionary<uint, string>? ContentNames =
        LuminaGetter.Get<ContentFinderCondition>()
                    .Where(x => !string.IsNullOrEmpty(x.Name.ExtractText()) &&
                                !IgnoredContents.Contains(x.TerritoryType.Value.RowId))
                    .DistinctBy(x => x.TerritoryType.Value.RowId)
                    .ToDictionary(x => x.TerritoryType.Value.RowId, x => x.Name.ToString());

    private static readonly HashSet<uint> IgnoredContents =
    [
        579, 940, 941,
        // Eureka
        732, 763, 795, 827,
        // Bozja
        920, 975
    ];

    private bool isDutyStarted;
    private string contentName = string.Empty;

    private InventoryHandler? inventoryHandler;

    public void Init()
    {
        if (BoundByDuty) 
            CheckDutyStart();

        DService.ClientState.TerritoryChanged += OnZoneChange;
    }

    private void CheckDutyStart()
    {
        if (isDutyStarted) return;

        if (ContentNames.TryGetValue(CurrentLocationID, out var dutyName))
        {
            isDutyStarted = true;
            contentName = dutyName;
            HandlerManager.ChatHandler.isBlocked = true;
            inventoryHandler ??= new InventoryHandler();

            DService.Log.Debug($"Duty {dutyName} Starts");
        }
    }

    private void OnZoneChange(ushort obj)
    {
        if (BoundByDuty)
            CheckDutyStart();
        else if (isDutyStarted)
            CheckDutyEnd();
    }

    private void CheckDutyEnd()
    {
        if (!isDutyStarted) return;

        DService.Log.Debug($"Duty {contentName} Ends, Currency Change Check Starts.");

        var items = inventoryHandler?.Items ?? [];
        Tracker.CheckCurrencies(items, PreviousLocationName,
                                        Service.Config.ComponentProp["RecordContentName"]
                                            ? $"({contentName})"
                                            : "", RecordChangeType.All, 2);

        isDutyStarted = false;
        contentName = string.Empty;
        HandlerManager.ChatHandler.isBlocked = false;
        HandlerManager.Nullify(ref inventoryHandler);

        DService.Log.Debug("Currency Change Check Completes.");
    }

    public void Uninit()
    {
        DService.ClientState.TerritoryChanged -= OnZoneChange;
        HandlerManager.Nullify(ref inventoryHandler);

        isDutyStarted = false;
        contentName = string.Empty;
    }
}
