namespace CurrencyTracker.Manager.Trackers.Components;

public class PremiumSaddleBag : ITrackerComponent
{
    public bool Initialized { get; set; }

    public static readonly InventoryType[] PSaddleBagInventories =
    {
        InventoryType.PremiumSaddleBag1, InventoryType.PremiumSaddleBag1
    };

    internal static Dictionary<uint, long> InventoryItemCount = new();
    private string windowTitle = string.Empty;

    public void Init()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "InventoryBuddy", OnPremiumSaddleBag);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "InventoryBuddy", OnPremiumSaddleBag);
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

                Service.Tracker.CheckCurrencies(InventoryItemCount.Keys, "", "", 0, 21,
                                                TransactionFileCategory.SaddleBag);
                Service.Tracker.CheckCurrencies(InventoryItemCount.Keys, "", $"({windowTitle})", 0, 21);

                InventoryItemCount.Clear();

                break;
            }
        }
    }

    private void PSaddleBagScanner(IFramework framework)
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
    }
}
