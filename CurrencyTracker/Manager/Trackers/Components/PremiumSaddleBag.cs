namespace CurrencyTracker.Manager.Trackers.Components
{
    public class PremiumSaddleBag : ITrackerComponent
    {
        public bool Initialized { get; set; } = false;

        public static readonly InventoryType[] PSaddleBagInventories = new InventoryType[]
        {
            InventoryType.PremiumSaddleBag1, InventoryType.PremiumSaddleBag1
        };

        internal static Dictionary<uint, long> InventoryItemCount = new();
        private string windowTitle = string.Empty;

        public void Init()
        {
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "InventoryBuddy", OnPremiumSaddleBag);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "InventoryBuddy", OnPremiumSaddleBag);

            Initialized = true;
        }

        private void OnPremiumSaddleBag(AddonEvent type, AddonArgs args)
        {
            switch (type)
            {
                case AddonEvent.PostSetup:
                    {
                        windowTitle = GetWindowTitle(args.Addon, 86);
                        Service.Framework.Update += PSaddleBagScanner;

                        break;
                    }
                case AddonEvent.PreFinalize:
                    {
                        Service.Framework.Update -= PSaddleBagScanner;

                        Service.Tracker.CheckCurrencies(InventoryItemCount.Keys, "", "", 0, 21, TransactionFileCategory.SaddleBag, 0);
                        Service.Tracker.CheckCurrencies(InventoryItemCount.Keys, "", $"({windowTitle})", 0, 21, TransactionFileCategory.Inventory, 0);

                        InventoryItemCount.Clear();

                        break;
                    }
            }
        }

        private unsafe void PSaddleBagScanner(IFramework framework)
        {
            InventoryScanner(PSaddleBagInventories, ref InventoryItemCount);
        }

        public void Uninit()
        {
            Service.Framework.Update -= PSaddleBagScanner;
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "InventoryBuddy", OnPremiumSaddleBag);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "InventoryBuddy", OnPremiumSaddleBag);
            windowTitle = string.Empty;
            InventoryItemCount.Clear();

            Initialized = false;
        }
    }
}
