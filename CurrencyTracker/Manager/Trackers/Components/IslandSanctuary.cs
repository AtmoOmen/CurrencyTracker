using System.Collections.Generic;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tools;
using CurrencyTracker.Manager.Trackers.Handlers;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;

namespace CurrencyTracker.Manager.Trackers.Components;

public class IslandSanctuary : ITrackerComponent
{
    public bool Initialized { get; set; }

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

    public void Init()
    {
        if (CurrentLocationID == 1055)
            OnZoneChanged(1055);

        Service.ClientState.TerritoryChanged += OnZoneChanged;
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, MJIWindowModules.Keys, BeginMJIWindow);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, MJIWindowModules.Keys, EndMJIWindow);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, MJIModules.Keys, BeginMJI);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, MJIModules.Keys, EndMJI);
    }

    private void OnZoneChanged(ushort zone)
    {
        if (zone == 1055)
            Service.Framework.Update += OnUpdate;
        else
            Service.Framework.Update -= OnUpdate;
    }

    private void OnUpdate(IFramework framework)
    {
        if (!Throttler.Throttle("IslandSanctuary-CheckWorkshop")) return;
        var currentTarget = Service.Target.Target;
        var prevTarget = Service.Target.PreviousTarget;

        if (currentTarget?.DataId == 1043078)
        {
            if (!isOnWorkshop)
            {
                isOnWorkshop = true;
                inventoryHandler ??= new InventoryHandler();
                HandlerManager.ChatHandler.isBlocked = true;
            }
        }
        else if (prevTarget?.DataId == 1043078 && isOnWorkshop)
        {
            if (currentTarget?.DataId != prevTarget.DataId)
            {
                isOnWorkshop = false;

                var items = inventoryHandler?.Items ?? [];
                Tracker.CheckCurrencies(items, "", $"({Service.Lang.GetText("IslandWorkshop")})",
                                        RecordChangeType.All, 7);

                HandlerManager.Nullify(ref inventoryHandler);
                HandlerManager.ChatHandler.isBlocked = false;
            }
        }
    }

    private void BeginMJI(AddonEvent type, AddonArgs args)
    {
        inventoryHandler ??= new InventoryHandler();
        HandlerManager.ChatHandler.isBlocked = true;
    }

    private void EndMJI(AddonEvent type, AddonArgs args)
    {
        if (Flags.OccupiedInEvent()) return;

        var items = inventoryHandler?.Items ?? [];
        Tracker.CheckCurrencies(items, "", $"({MJIModules[args.AddonName]})", RecordChangeType.All, 5);

        HandlerManager.ChatHandler.isBlocked = false;
        HandlerManager.Nullify(ref inventoryHandler);
    }

    private void BeginMJIWindow(AddonEvent type, AddonArgs args)
    {
        windowTitle = GetWindowTitle(args, MJIWindowModules[args.AddonName]);
        inventoryHandler ??= new InventoryHandler();
        HandlerManager.ChatHandler.isBlocked = true;
    }

    private void EndMJIWindow(AddonEvent type, AddonArgs args)
    {
        if (Flags.OccupiedInEvent()) return;

        var items = inventoryHandler?.Items ?? [];
        Tracker.CheckCurrencies(items, "", $"({windowTitle})", RecordChangeType.All, 6);

        HandlerManager.ChatHandler.isBlocked = false;
        HandlerManager.Nullify(ref inventoryHandler);
    }

    public void Uninit()
    {
        Service.Framework.Update -= OnUpdate;
        Service.ClientState.TerritoryChanged -= OnZoneChanged;

        Service.AddonLifecycle.UnregisterListener(BeginMJIWindow);
        Service.AddonLifecycle.UnregisterListener(EndMJIWindow);
        Service.AddonLifecycle.UnregisterListener(BeginMJI);
        Service.AddonLifecycle.UnregisterListener(EndMJI);

        HandlerManager.Nullify(ref inventoryHandler);

        isOnWorkshop = false;
        windowTitle = string.Empty;
    }
}
