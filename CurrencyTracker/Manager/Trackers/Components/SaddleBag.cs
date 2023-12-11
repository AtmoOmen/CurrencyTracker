namespace CurrencyTracker.Manager.Trackers.Components
{
    public class SaddleBag : ITrackerComponent
    {
        public bool Initialized
        {
            get { return _initialized; }
            set { _initialized = value; }
        }

        public static readonly InventoryType[] SaddleBagInventories = new InventoryType[]
        {
            InventoryType.SaddleBag1, InventoryType.SaddleBag2
        };

        private string windowTitle = string.Empty;
        private bool _initialized = false;
        internal static Dictionary<uint, long> InventoryItemCount = new();

        public void Init()
        {
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "InventoryBuddy", BeginSaddleBag);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "InventoryBuddy", EndSaddleBag);

            _initialized = true;
        }

        private void BeginSaddleBag(AddonEvent type, AddonArgs args)
        {
            foreach(var currency in Plugin.Configuration.AllCurrencies)
            {
                InventoryItemCount.Add(currency.Key, 0);
            }

            windowTitle = GetWindowTitle(args.Addon, 86);

            Service.Framework.Update += SaddleBagScanner;
        }

        private void EndSaddleBag(AddonEvent type, AddonArgs args)
        {
            Service.Framework.Update -= SaddleBagScanner;

            SaddleBagHandler();
        }

        private unsafe void SaddleBagScanner(IFramework framework)
        {
            var inventoryManager = InventoryManager.Instance();

            if (inventoryManager != null)
            {
                Parallel.ForEach(Plugin.Configuration.AllCurrencies, currency =>
                {
                    long itemCount = 0;
                    Parallel.ForEach(SaddleBagInventories, inventory =>
                    {
                        itemCount += inventoryManager->GetItemCountInContainer(currency.Key, inventory);
                    });
                    InventoryItemCount[currency.Key] = itemCount;
                });
            }
        }

        private unsafe void SaddleBagHandler()
        {
            Service.Tracker.CheckAllCurrencies("", "", 0, 21, TransactionFileCategory.SaddleBag, 0);
            Service.Tracker.CheckAllCurrencies("", $"({windowTitle})", 0, 21, TransactionFileCategory.Inventory, 0);
            InventoryItemCount.Clear();
        }

        public void Uninit()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "InventoryBuddy", BeginSaddleBag);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "InventoryBuddy", EndSaddleBag);

            Service.Framework.Update -= SaddleBagScanner;

            windowTitle = string.Empty;
            InventoryItemCount.Clear();
            _initialized = false;
        }
    }
}
