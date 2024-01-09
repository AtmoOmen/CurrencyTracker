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

    private bool isInIsland;
    private bool isOnWorkshop;
    private string windowTitle = string.Empty;

    private InventoryHandler? inventoryHandler;

    public void Init()
    {
        if (CurrentLocationID == 1055)
        {
            isInIsland = true;
            Service.Framework.Update += OnFrameworkUpdate;
        }

        Service.ClientState.TerritoryChanged += OnZoneChanged;
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, MJIWindowModules.Keys, BeginMJIWindow);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, MJIWindowModules.Keys, EndMJIWindow);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, MJIModules.Keys, BeginMJI);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, MJIModules.Keys, EndMJI);

        Initialized = true;
    }

    private void OnZoneChanged(ushort obj)
    {
        var IntoMJI = !isInIsland && CurrentLocationID == 1055;

        if (IntoMJI) Service.Framework.Update += OnFrameworkUpdate;
        else Service.Framework.Update -= OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        WorkshopHandler();
    }

    private void BeginMJI(AddonEvent type, AddonArgs args)
    {
        inventoryHandler = new InventoryHandler();
        HandlerManager.ChatHandler.isBlocked = true;
    }

    private void EndMJI(AddonEvent type, AddonArgs args)
    {
        if (Flags.OccupiedInEvent()) return;

        var items = inventoryHandler?.Items ?? new HashSet<uint>();
        Service.Tracker.CheckCurrencies(items, "", $"({MJIModules[args.AddonName]})", RecordChangeType.All, 5);

        HandlerManager.ChatHandler.isBlocked = false;
        HandlerManager.Nullify(ref inventoryHandler);
    }

    private void BeginMJIWindow(AddonEvent type, AddonArgs args)
    {
        windowTitle = GetWindowTitle(args, MJIWindowModules[args.AddonName]);
        inventoryHandler = new InventoryHandler();
        HandlerManager.ChatHandler.isBlocked = true;
    }

    private void EndMJIWindow(AddonEvent type, AddonArgs args)
    {
        if (Flags.OccupiedInEvent()) return;

        var items = inventoryHandler?.Items ?? new HashSet<uint>();
        Service.Tracker.CheckCurrencies(items, "", $"({windowTitle})", RecordChangeType.All, 6);

        HandlerManager.ChatHandler.isBlocked = false;
        HandlerManager.Nullify(ref inventoryHandler);
    }

    private void WorkshopHandler()
    {
        var currentTarget = Service.TargetManager.Target;
        var prevTarget = Service.TargetManager.PreviousTarget;

        if (currentTarget?.DataId == 1043078)
        {
            if (!isOnWorkshop)
            {
                isOnWorkshop = true;
                inventoryHandler = new InventoryHandler();
                HandlerManager.ChatHandler.isBlocked = true;
            }
        }
        else if (prevTarget?.DataId == 1043078 && isOnWorkshop)
        {
            if (currentTarget?.DataId != prevTarget.DataId)
            {
                isOnWorkshop = false;

                var items = inventoryHandler?.Items ?? new HashSet<uint>();
                Service.Tracker.CheckCurrencies(items, "", $"({Service.Lang.GetText("IslandWorkshop")})",
                                                RecordChangeType.All, 7);

                HandlerManager.Nullify(ref inventoryHandler);
                HandlerManager.ChatHandler.isBlocked = false;
            }
        }
    }

    public void Uninit()
    {
        Service.Framework.Update -= OnFrameworkUpdate;
        Service.ClientState.TerritoryChanged -= OnZoneChanged;
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, MJIWindowModules.Keys, BeginMJIWindow);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, MJIWindowModules.Keys, EndMJIWindow);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, MJIModules.Keys, BeginMJI);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, MJIModules.Keys, EndMJI);
        HandlerManager.Nullify(ref inventoryHandler);
        isInIsland = false;
        isOnWorkshop = false;
        windowTitle = string.Empty;

        Initialized = false;
    }
}
