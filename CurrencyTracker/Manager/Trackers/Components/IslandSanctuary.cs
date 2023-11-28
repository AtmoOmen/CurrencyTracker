namespace CurrencyTracker.Manager.Trackers.Components
{
    public class IslandSanctuary : ITrackerComponent
    {
        private bool _initialized = false;

        public bool Initialized
        {
            get { return _initialized; }
            set { _initialized = value; }
        }

        private bool isInIsland = false;
        private bool isOnWorkshop = false;
        private string windowTitle = string.Empty;

        private readonly Dictionary<string, string> MJIModules = new()
        {
            { "MJIFarmManagement",  Service.Lang.GetText("IslandFarm") },
            { "MJIAnimalManagement",  Service.Lang.GetText("IslandPasture") }
        };

        private readonly Dictionary<string, uint> MJIWindowModules = new()
        {
            { "MJIGatheringHouse", 73 },
            { "MJIRecipeNoteBook", 37 },
            { "MJIBuilding", 25 }
        };

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

            _initialized = true;
        }

        private void OnZoneChanged(ushort obj)
        {
            if (!isInIsland && CurrentLocationID == 1055)
            {
                Service.Framework.Update += OnFrameworkUpdate;
            }

            if (isInIsland && CurrentLocationID != 1055)
            {
                Service.Framework.Update -= OnFrameworkUpdate;
            }
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            WorkshopHandler();
        }

        private void BeginMJI(AddonEvent type, AddonArgs args)
        {
            BeginMJIHandler();
        }

        private void BeginMJIHandler()
        {
            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = true;
        }

        private void EndMJI(AddonEvent type, AddonArgs args)
        {
            EndMJIHandler(args);
        }

        private void EndMJIHandler(AddonArgs args)
        {
            if (Flags.OccupiedInEvent()) return;

            Service.Tracker.CheckAllCurrencies("", $"({MJIModules[args.AddonName]})", RecordChangeType.All, 5);
        }

        private void BeginMJIWindow(AddonEvent type, AddonArgs args)
        {
            BeginMJIWindowHandler(args);
        }

        private void BeginMJIWindowHandler(AddonArgs args)
        {
            windowTitle = Service.Tracker.GetWindowTitle(args, MJIWindowModules[args.AddonName]);
            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = true;
        }

        private void EndMJIWindow(AddonEvent type, AddonArgs args)
        {
            EndMJIWindowHandler();
        }

        private void EndMJIWindowHandler()
        {
            if (Flags.OccupiedInEvent()) return;

            Service.Tracker.CheckAllCurrencies("", $"({windowTitle})", RecordChangeType.All, 6);

            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = false;
        }

        // 无人岛工房
        private void WorkshopHandler()
        {
            if (Service.TargetManager.Target != null && Service.TargetManager.Target.DataId == 1043078 && !isOnWorkshop)
            {
                isOnWorkshop = true;
                HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = true;
            }

            if (Service.TargetManager.PreviousTarget != null && Service.TargetManager.PreviousTarget.DataId == 1043078 && isOnWorkshop)
            {
                if (Service.TargetManager.Target != null && Service.TargetManager.Target.DataId == Service.TargetManager.PreviousTarget.DataId)
                {
                    return;
                }
                isOnWorkshop = false;

                Service.Tracker.CheckAllCurrencies("", $"({Service.Lang.GetText("IslandWorkshop")})", RecordChangeType.All, 7);

                HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = false;
            }
        }

        public void Uninit()
        {
            isInIsland = false;
            isOnWorkshop = false;
            windowTitle = string.Empty;

            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, MJIWindowModules.Keys, BeginMJIWindow);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, MJIWindowModules.Keys, EndMJIWindow);

            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, MJIModules.Keys, BeginMJI);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, MJIModules.Keys, EndMJI);

            Service.Framework.Update -= OnFrameworkUpdate;
            Service.ClientState.TerritoryChanged -= OnZoneChanged;
            _initialized = false;
        }
    }
}
