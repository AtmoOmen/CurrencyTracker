namespace CurrencyTracker.Manager.Trackers.Components
{
    public class Exchange : ITrackerComponent
    {
        public bool Initialized { get; set; } = false;

        private static readonly string[] UI = new string[]
        {
            "InclusionShop", "CollectablesShop", "FreeCompanyExchange", "FreeCompanyCreditShop", "ShopExchangeCurrency", "Shop", "ItemSearch", "ShopExchangeItem", "SkyIslandExchange", "TripleTriadCoinExchange", "FreeCompanyChest", "MJIDisposeShop", "GrandCompanyExchange", "ReconstructionBuyback"
        };
        private static readonly Dictionary<string, uint> WindowUI = new()  // Addon Name - Window Node ID
        {
            { "Repair", 38 },
            { "PvpReward", 125 },
            { "Materialize", 16 },
            { "ColorantEquipment", 13 },
            { "MiragePrism", 28 },
            { "HWDSupply", 67 },
        };

        private string currentTargetName = string.Empty;
        internal static bool isOnExchange = false;
        private string windowName = string.Empty;

        private InventoryHandler? inventoryHandler = null;

        public void Init()
        {
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, UI.Concat(WindowUI.Keys), BeginExchange);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, UI.Concat(WindowUI.Keys), EndExchange);

            Initialized = true;
        }

        private void BeginExchange(AddonEvent type, AddonArgs? args)
        {
            if (isOnExchange || SpecialExchange.isOnExchange) return;

            isOnExchange = true;
            HandlerManager.ChatHandler.isBlocked = true;
            inventoryHandler = new();

            if (args != null && WindowUI.TryGetValue(args.AddonName, out var value))
            {
                windowName = GetWindowTitle(args, value, args.AddonName == "PvpReward" ? new uint[] { 4, 5 } : null) ?? string.Empty;
            }
            else
            {
                currentTargetName = Service.TargetManager.Target?.Name.TextValue ?? string.Empty;
            }
        }

        private void EndExchange(AddonEvent type, AddonArgs args)
        {
            if (SpecialExchange.isOnExchange) return;

            Service.Log.Debug("Exchange Completes, Currency Change Check Starts.");

            Service.Tracker.CheckCurrencies(inventoryHandler.Items, "", $"({(WindowUI.ContainsKey(args.AddonName) ? windowName : (Service.Lang.GetText("ExchangeWith", currentTargetName)))})", RecordChangeType.All, 3);

            windowName = currentTargetName = string.Empty;
            HandlerManager.ChatHandler.isBlocked = isOnExchange = false;
            HandlerManager.Nullify(ref inventoryHandler);

            Service.Log.Debug("Currency Change Check Completes.");
        }

        public void Uninit()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, UI.Concat(WindowUI.Keys), BeginExchange);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, UI.Concat(WindowUI.Keys), EndExchange);
            HandlerManager.Nullify(ref inventoryHandler);

            Initialized = false;
        }
    }
}
