
namespace CurrencyTracker.Manager.Trackers.Components
{
    public class SaddleBag : ITrackerComponent
    {
        public bool Initialized { get; set; } = false;

        public static readonly InventoryType[] SaddleBagInventories = new InventoryType[]
        {
            InventoryType.SaddleBag1, InventoryType.SaddleBag2
        };

        internal static Dictionary<uint, long> InventoryItemCount = new();
        private string windowTitle = string.Empty;

        public void Init()
        {
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "InventoryBuddy", OnSaddleBag);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "InventoryBuddy", OnSaddleBag);

            Initialized = true;
        }

        private void OnSaddleBag(AddonEvent type, AddonArgs args)
        {
            switch (type)
            {
                case AddonEvent.PostSetup:
                    {
                        windowTitle = GetWindowTitle(args.Addon, 86);
                        Service.Framework.Update += SaddleBagScanner;

                        break;
                    }
                case AddonEvent.PreFinalize:
                    {
                        Service.Framework.Update -= SaddleBagScanner;

                        Service.Tracker.CheckCurrencies(InventoryItemCount.Keys, "", "", 0, 21, TransactionFileCategory.SaddleBag, 0);
                        Service.Tracker.CheckCurrencies(InventoryItemCount.Keys, "", $"({windowTitle})", 0, 21, TransactionFileCategory.Inventory, 0);

                        InventoryItemCount.Clear();

                        break;
                    }
            }
        }

        private unsafe void SaddleBagScanner(IFramework framework)
        {
            InventoryScanner(SaddleBagInventories, ref InventoryItemCount);
        }

        public void Uninit()
        {
            Service.Framework.Update -= SaddleBagScanner;
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "InventoryBuddy", OnSaddleBag);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "InventoryBuddy", OnSaddleBag);
            windowTitle = string.Empty;
            InventoryItemCount.Clear();

            Initialized = false;
        }
    }
}
