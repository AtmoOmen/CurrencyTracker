namespace CurrencyTracker.Manager.Trackers.Components
{
    public class PremiumSaddleBag : ITrackerComponent
    {
        public bool Initialized
        {
            get { return _initialized; }
            set { _initialized = value; }
        }

        public static readonly InventoryType[] PSaddleBagInventories = new InventoryType[]
        {
            InventoryType.PremiumSaddleBag1, InventoryType.PremiumSaddleBag1
        };

        private string windowTitle = string.Empty;
        private bool _initialized = false;
        internal static Dictionary<uint, long> InventoryItemCount = new();

        public void Init()
        {
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "InventoryBuddy", BeginPSaddleBag);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "InventoryBuddy", EndPSaddleBag);

            _initialized = true;
        }

        private void BeginPSaddleBag(AddonEvent type, AddonArgs args)
        {
            foreach (var currency in Plugin.Configuration.AllCurrencies)
            {
                InventoryItemCount.Add(currency.Key, 0);
            }

            windowTitle = GetWindowTitle(args.Addon, 86);

            Service.Framework.Update += PSaddleBagScanner;
        }

        private void EndPSaddleBag(AddonEvent type, AddonArgs args)
        {
            Service.Framework.Update -= PSaddleBagScanner;

            PSaddleBagHandler();
        }

        private unsafe void PSaddleBagScanner(IFramework framework)
        {
            var inventoryManager = InventoryManager.Instance();

            if (inventoryManager != null)
            {
                Parallel.ForEach(Plugin.Configuration.AllCurrencies, currency =>
                {
                    long itemCount = 0;
                    Parallel.ForEach(PSaddleBagInventories, inventory =>
                    {
                        itemCount += inventoryManager->GetItemCountInContainer(currency.Key, inventory);
                    });
                    InventoryItemCount[currency.Key] = itemCount;
                });
            }
        }

        private unsafe void PSaddleBagHandler()
        {
            Service.Tracker.CheckAllCurrencies("", "", 0, 21, TransactionFileCategory.PremiumSaddleBag, 0);
            Service.Tracker.CheckAllCurrencies("", $"({windowTitle})", 0, 21, TransactionFileCategory.Inventory, 0);
            InventoryItemCount.Clear();
        }

        public void Uninit()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "InventoryBuddy", BeginPSaddleBag);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "InventoryBuddy", EndPSaddleBag);

            Service.Framework.Update -= PSaddleBagScanner;

            windowTitle = string.Empty;
            InventoryItemCount.Clear();
            _initialized = false;
        }
    }
}
