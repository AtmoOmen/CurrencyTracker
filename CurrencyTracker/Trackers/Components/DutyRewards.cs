using System.Collections.Generic;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Tracker;
using CurrencyTracker.Manager.Trackers.Handlers;

namespace CurrencyTracker.Trackers.Components;

public class DutyRewards : TrackerComponentBase
{
    private static readonly HashSet<uint> ContentUseToIgnore = [41, 48, 60, 61];

    private InventoryHandler? InventoryHandler;

    private static bool   IsDutyStarted;
    private static string DutyName = string.Empty;

    protected override void OnInit()
    {
        DService.ClientState.TerritoryChanged += OnZoneChange;
        OnZoneChange(0);
    }

    private void OnZoneChange(ushort zone)
    {
        if (IsDutyStarted)
        {
            if (GameState.ContentFinderCondition > 0 && !ContentUseToIgnore.Contains(GameState.TerritoryIntendedUse))
                return;

            DService.Log.Debug($"Duty {DutyName} completed. Starts to check currency changes.");

            var items = InventoryHandler?.Items ?? [];
            TrackerManager.CheckCurrencies(items, PreviousLocationName,
                                           Service.Config.ComponentProp["RecordContentName"]
                                               ? $"({DutyName})"
                                               : string.Empty, RecordChangeType.All, 2);

            IsDutyStarted = false;
            DutyName      = string.Empty;
        
            HandlerManager.ChatHandler.IsBlocked = false;
            HandlerManager.Nullify(ref InventoryHandler);

            DService.Log.Debug("Currency changes check completes.");
            return;
        }

        if (GameState.ContentFinderCondition == 0 || ContentUseToIgnore.Contains(GameState.TerritoryIntendedUse))
            return;

        IsDutyStarted = true;
        DutyName      = GameState.ContentFinderConditionData.Name.ExtractText();
        
        HandlerManager.ChatHandler.IsBlocked = true;
        InventoryHandler ??= new InventoryHandler();
        
        DService.Log.Debug($"Duty {DutyName} starts, recording all inventory changes.");
    }

    protected override void OnUninit()
    {
        DService.ClientState.TerritoryChanged -= OnZoneChange;
        
        HandlerManager.Nullify(ref InventoryHandler);

        IsDutyStarted = false;
        DutyName      = string.Empty;
    }
}
