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

        public void Init()
        {
            if (TerrioryHandler.CurrentLocationID == 1055)
            {
                isInIsland = true;
                Service.Framework.Update += OnFrameworkUpdate;
            }

            Service.ClientState.TerritoryChanged += OnZoneChanged;

            // 囤货仓库 Gathering House
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "MJIGatheringHouse", MGHStart);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "MJIGatheringHouse", MGHEnd);
            // 无人岛制作 Island Crafting
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "MJIRecipeNoteBook", MRNBStart);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "MJIRecipeNoteBook", MRNBEnd);
            // 无人岛建造 Island Building
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "MJIBuilding", MBStart);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "MJIBuilding", MBEnd);
            // 无人岛耕地 Island Farm
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "MJIFarmManagement", MFMStart);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "MJIFarmManagement", MFMEnd);
            // 无人岛牧场 Island Pasture
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "MJIAnimalManagement", MAMStart);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "MJIAnimalManagement", MAMEnd);

            _initialized = true;
        }

        private void OnZoneChanged(ushort obj)
        {
            if (!isInIsland && TerrioryHandler.CurrentLocationID == 1055)
            {
                Service.Framework.Update += OnFrameworkUpdate;
            }

            if (isInIsland && TerrioryHandler.CurrentLocationID != 1055)
            {
                Service.Framework.Update -= OnFrameworkUpdate;
            }
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            WorkshopHandler();
        }

        // 无人岛牧场
        private void MAMStart(AddonEvent type, AddonArgs args)
        {
            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = true;
        }

        private void MAMEnd(AddonEvent type, AddonArgs args)
        {
            if (Flags.OccupiedInEvent())
                return;

            Parallel.ForEach(Plugin.Instance.Configuration.AllCurrencies, currency =>
            {
                Service.Tracker.CheckCurrency(currency.Key, "", $"({Service.Lang.GetText("IslandPasture")})");
            });
        }

        // 无人岛耕地
        private void MFMStart(AddonEvent type, AddonArgs args)
        {
            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = true;
        }

        private void MFMEnd(AddonEvent type, AddonArgs args)
        {
            if (Flags.OccupiedInEvent())
                return;

            Parallel.ForEach(Plugin.Instance.Configuration.AllCurrencies, currency =>
            {
                Service.Tracker.CheckCurrency(currency.Key, "", $"({Service.Lang.GetText("IslandFarm")})");
            });
        }

        // 无人岛建造 Island Building
        private unsafe void MBStart(AddonEvent type, AddonArgs args)
        {
            windowTitle = Service.Tracker.GetWindowTitle(args, 25);
            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = true;
        }

        private void MBEnd(AddonEvent type, AddonArgs args)
        {
            if (Flags.OccupiedInEvent())
                return;

            Parallel.ForEach(Plugin.Instance.Configuration.AllCurrencies, currency =>
            {
                Service.Tracker.CheckCurrency(currency.Key, "", $"({windowTitle})");
            });

            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = false;
        }

        // 无人岛制作
        private unsafe void MRNBStart(AddonEvent type, AddonArgs args)
        {
            windowTitle = Service.Tracker.GetWindowTitle(args, 37);
            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = true;
        }

        private void MRNBEnd(AddonEvent type, AddonArgs args)
        {
            if (Flags.OccupiedInEvent())
                return;

            Parallel.ForEach(Plugin.Instance.Configuration.AllCurrencies, currency =>
            {
                Service.Tracker.CheckCurrency(currency.Key, "", $"({windowTitle})");
            });

            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = false;
        }

        // 无人岛屯货仓库
        private unsafe void MGHStart(AddonEvent type, AddonArgs args)
        {
            windowTitle = Service.Tracker.GetWindowTitle(args, 73);
            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = true;
        }

        private void MGHEnd(AddonEvent type, AddonArgs args)
        {
            if (Flags.OccupiedInEvent())
                return;

            Parallel.ForEach(Plugin.Instance.Configuration.AllCurrencies, currency =>
            {
                Service.Tracker.CheckCurrency(currency.Key, "", $"({windowTitle})");
            });

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

                Parallel.ForEach(Plugin.Instance.Configuration.AllCurrencies, currency =>
                {
                    Service.Tracker.CheckCurrency(currency.Key, "", $"({Service.Lang.GetText("IslandWorkshop")})");
                });

                HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = false;
            }
        }

        public void Uninit()
        {
            isInIsland = false;
            isOnWorkshop = false;
            windowTitle = string.Empty;

            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "MJIGatheringHouse", MGHStart);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "MJIGatheringHouse", MGHEnd);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "MJIRecipeNotebook", MRNBStart);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "MJIRecipeNotebook", MRNBEnd);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "MJIBuilding", MBStart);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "MJIBuilding", MBEnd);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "MJIFarmManagement", MFMStart);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "MJIFarmManagement", MFMEnd);

            Service.Framework.Update -= OnFrameworkUpdate;
            Service.ClientState.TerritoryChanged -= OnZoneChanged;
            _initialized = false;
        }
    }
}
