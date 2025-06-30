using System.Collections.Generic;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tools;
using CurrencyTracker.Manager.Tracker;
using CurrencyTracker.Manager.Trackers.Handlers;
using CurrencyTracker.Trackers;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;

namespace CurrencyTracker.Manager.Trackers.Components;

public class IslandSanctuary : TrackerComponentBase
{

    private readonly Dictionary<string, string> MJIModules = new()
    {
        { "MJIFarmManagement", Service.Lang.GetText("IslandFarm") },
        { "MJIAnimalManagement", Service.Lang.GetText("IslandPasture") }
    };

    private readonly Dictionary<string, uint> MJIWindowModules = new()
    {
        { "MJIGatheringHouse", 73 },
        { "MJIRecipeNoteBook", 37 },
        { "MJIBuilding", 25 }
    };

    private bool isOnWorkshop;
    private string windowTitle = string.Empty;

    private InventoryHandler? inventoryHandler;

    protected override void OnInit()
    {
        if (CurrentLocationID == 1055)
            OnZoneChanged(1055);

        DService.ClientState.TerritoryChanged += OnZoneChanged;
        DService.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, MJIWindowModules.Keys, BeginMJIWindow);
        DService.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, MJIWindowModules.Keys, EndMJIWindow);
        DService.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, MJIModules.Keys, BeginMJI);
        DService.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, MJIModules.Keys, EndMJI);
    }

    private void OnZoneChanged(ushort zone)
    {
        if (zone == 1055)
            DService.Framework.Update += OnUpdate;
        else
            DService.Framework.Update -= OnUpdate;
    }

    private void OnUpdate(IFramework framework)
    {
        if (!Throttler.Throttle("IslandSanctuary-CheckWorkshop")) return;
        var currentTarget = DService.Targets.Target;
        var prevTarget = DService.Targets.PreviousTarget;

        if (currentTarget?.DataId == 1043078)
        {
            if (!isOnWorkshop)
            {
                isOnWorkshop = true;
                inventoryHandler ??= new InventoryHandler();
                HandlerManager.ChatHandler.IsBlocked = true;
            }
        }
        else if (prevTarget?.DataId == 1043078 && isOnWorkshop)
        {
            if (currentTarget?.DataId != prevTarget.DataId)
            {
                isOnWorkshop = false;

                var items = inventoryHandler?.Items ?? [];
                TrackerManager.CheckCurrencies(items, "", $"({Service.Lang.GetText("IslandWorkshop")})",
                                        RecordChangeType.All, 7);

                HandlerManager.Nullify(ref inventoryHandler);
                HandlerManager.ChatHandler.IsBlocked = false;
            }
        }
    }

    private void BeginMJI(AddonEvent type, AddonArgs args)
    {
        inventoryHandler ??= new InventoryHandler();
        HandlerManager.ChatHandler.IsBlocked = true;
    }

    private void EndMJI(AddonEvent type, AddonArgs args)
    {
        if (OccupiedInEvent) return;

        var items = inventoryHandler?.Items ?? [];
        TrackerManager.CheckCurrencies(items, "", $"({MJIModules[args.AddonName]})", RecordChangeType.All, 5);

        HandlerManager.ChatHandler.IsBlocked = false;
        HandlerManager.Nullify(ref inventoryHandler);
    }

    private void BeginMJIWindow(AddonEvent type, AddonArgs args)
    {
        windowTitle = GetWindowTitle(args, MJIWindowModules[args.AddonName]);
        inventoryHandler ??= new InventoryHandler();
        HandlerManager.ChatHandler.IsBlocked = true;
    }

    private void EndMJIWindow(AddonEvent type, AddonArgs args)
    {
        if (OccupiedInEvent) return;

        var items = inventoryHandler?.Items ?? [];
        TrackerManager.CheckCurrencies(items, "", $"({windowTitle})", RecordChangeType.All, 6);

        HandlerManager.ChatHandler.IsBlocked = false;
        HandlerManager.Nullify(ref inventoryHandler);
    }

    protected override void OnUninit()
    {
        DService.Framework.Update -= OnUpdate;
        DService.ClientState.TerritoryChanged -= OnZoneChanged;

        DService.AddonLifecycle.UnregisterListener(BeginMJIWindow);
        DService.AddonLifecycle.UnregisterListener(EndMJIWindow);
        DService.AddonLifecycle.UnregisterListener(BeginMJI);
        DService.AddonLifecycle.UnregisterListener(EndMJI);

        HandlerManager.Nullify(ref inventoryHandler);

        isOnWorkshop = false;
        windowTitle = string.Empty;
    }
}
