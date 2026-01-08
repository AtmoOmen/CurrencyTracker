using System.Collections.Generic;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tracker;
using CurrencyTracker.Manager.Trackers.Handlers;
using CurrencyTracker.Trackers;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;

namespace CurrencyTracker.Manager.Trackers.Components;

public class IslandSanctuary : TrackerComponentBase
{
    private static readonly Dictionary<string, string> MJIModules = new()
    {
        { "MJIFarmManagement", Service.Lang.GetText("IslandFarm") },
        { "MJIAnimalManagement", Service.Lang.GetText("IslandPasture") }
    };

    private static readonly Dictionary<string, uint> MJIWindowModules = new()
    {
        { "MJIGatheringHouse", 75 },
        { "MJIRecipeNoteBook", 37 },
        { "MJIBuilding", 25 }
    };

    private bool   isOnWorkshop;
    private string windowTitle = string.Empty;

    private InventoryHandler? inventoryHandler;

    protected override void OnInit()
    {
        if (CurrentLocationID == 1055)
            OnZoneChanged(1055);

        DService.Instance().ClientState.TerritoryChanged += OnZoneChanged;

        DService.Instance().AddonLifecycle.RegisterListener(AddonEvent.PostSetup,   MJIWindowModules.Keys, BeginMJIWindow);
        DService.Instance().AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, MJIWindowModules.Keys, EndMJIWindow);
        DService.Instance().AddonLifecycle.RegisterListener(AddonEvent.PostSetup,   MJIModules.Keys,       BeginMJI);
        DService.Instance().AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, MJIModules.Keys,       EndMJI);
    }

    private void OnZoneChanged(ushort zone)
    {
        if (zone == 1055)
            DService.Instance().Framework.Update += OnUpdate;
        else
            DService.Instance().Framework.Update -= OnUpdate;
    }

    private void OnUpdate(IFramework framework)
    {
        if (!Throttler.Throttle("IslandSanctuary-CheckWorkshop")) return;
        var currentTarget = TargetManager.Target;
        var prevTarget    = TargetManager.PreviousTarget;

        if (currentTarget?.DataID == 1043078)
        {
            if (!isOnWorkshop)
            {
                isOnWorkshop                         =   true;
                inventoryHandler                     ??= new InventoryHandler();
                HandlerManager.ChatHandler.IsBlocked =   true;
            }
        }
        else if (prevTarget?.DataID == 1043078 && isOnWorkshop)
        {
            if (currentTarget?.DataID != prevTarget.DataID)
            {
                isOnWorkshop = false;

                var items = inventoryHandler?.Items ?? [];
                TrackerManager.CheckCurrencies
                (
                    items,
                    string.Empty,
                    $"({Service.Lang.GetText("IslandWorkshop")})",
                    RecordChangeType.All,
                    7
                );

                HandlerManager.Nullify(ref inventoryHandler);
                HandlerManager.ChatHandler.IsBlocked = false;
            }
        }
    }

    private void BeginMJI(AddonEvent type, AddonArgs args)
    {
        inventoryHandler                     ??= new InventoryHandler();
        HandlerManager.ChatHandler.IsBlocked =   true;
    }

    private void EndMJI(AddonEvent type, AddonArgs args)
    {
        if (OccupiedInEvent) return;

        var items = inventoryHandler?.Items ?? [];
        TrackerManager.CheckCurrencies(items, "", $"({MJIModules[args.AddonName]})", RecordChangeType.All, 5);

        HandlerManager.ChatHandler.IsBlocked = false;
        HandlerManager.Nullify(ref inventoryHandler);
    }

    private unsafe void BeginMJIWindow(AddonEvent type, AddonArgs args)
    {
        windowTitle                          =   args.Addon.ToStruct()->GetWindowTitle();
        inventoryHandler                     ??= new InventoryHandler();
        HandlerManager.ChatHandler.IsBlocked =   true;
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
        DService.Instance().Framework.Update             -= OnUpdate;
        DService.Instance().ClientState.TerritoryChanged -= OnZoneChanged;

        DService.Instance().AddonLifecycle.UnregisterListener(BeginMJIWindow, EndMJIWindow, EndMJI);

        HandlerManager.Nullify(ref inventoryHandler);

        isOnWorkshop = false;
        windowTitle  = string.Empty;
    }
}
