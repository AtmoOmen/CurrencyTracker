namespace CurrencyTracker.Manager.Trackers.Components
{
    public class SpecialExchange : ITrackerComponent
    {
        public bool Initialized { get; set; } = false;

        private static readonly Dictionary<string, uint> UI = new()  // Addon Name - Window Node ID
        {
            { "GrandCompanySupplyList", 27 },
            { "WeeklyBingoResult", 99 },
            { "ReconstructionBox", 31 },
        };

        internal static bool isOnExchange = false;
        private string windowName = string.Empty;

        private InventoryHandler? inventoryHandler = null;

        public void Init()
        {
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, UI.Keys, BeginExchange);

            Initialized = true;
        }

        private void BeginExchange(AddonEvent type, AddonArgs args)
        {
            if (isOnExchange || Exchange.isOnExchange) return;

            HandlerManager.ChatHandler.isBlocked = true;

            isOnExchange = true;
            windowName = GetWindowTitle(args, UI[args.AddonName]) ?? string.Empty;
            inventoryHandler = new();

            Service.Framework.Update += OnFrameworkUpdate;
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            if (Flags.OccupiedInEvent()) return;

            if (!isOnExchange && !Exchange.isOnExchange)
            {
                Service.Framework.Update -= OnFrameworkUpdate;
                return;
            }

            EndExchangeHandler();
        }

        private void EndExchangeHandler()
        {
            if (Exchange.isOnExchange) return;

            Service.Log.Debug("Exchange Completes, Currency Change Check Starts.");

            Service.Framework.Update -= OnFrameworkUpdate;
            isOnExchange = false;

            Service.Tracker.CheckCurrencies(inventoryHandler.Items, "", $"({windowName})", RecordChangeType.All, 10);

            windowName = string.Empty;
            HandlerManager.ChatHandler.isBlocked = false;
            HandlerManager.Nullify(ref inventoryHandler);

            Service.Log.Debug("Currency Change Check Completes.");
        }

        public void Uninit()
        {
            Service.Framework.Update -= OnFrameworkUpdate;
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, UI.Keys, BeginExchange);
            HandlerManager.Nullify(ref inventoryHandler);
            isOnExchange = false;
            windowName = string.Empty;

            Initialized = false;
        }

    }
}
