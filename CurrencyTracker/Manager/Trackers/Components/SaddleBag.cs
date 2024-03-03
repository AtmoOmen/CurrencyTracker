namespace CurrencyTracker.Manager.Trackers.Components;

public class SaddleBag : ITrackerComponent
{
    public bool Initialized { get; set; }

    public static readonly InventoryType[] SaddleBagInventories =
    {
        InventoryType.SaddleBag1, InventoryType.SaddleBag2
    };

    internal static Dictionary<uint, long> InventoryItemCount = new();
    private string windowTitle = string.Empty;

    public void Init()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "InventoryBuddy", OnSaddleBag);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "InventoryBuddy", OnSaddleBag);
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
                Service.Framework.Update -= SaddleBagScanner;

                    Service.Tracker.CheckCurrencies(InventoryItemCount.Keys, "", "", 0, 21,
                                                TransactionFileCategory.SaddleBag);
                Service.Tracker.CheckCurrencies(InventoryItemCount.Keys, "", $"({windowTitle})", 0, 21);

                InventoryItemCount.Clear();

                break;
            }
        }
    }

    private static void SaddleBagScanner(IFramework framework)
    {
        InventoryScanner(SaddleBagInventories, ref InventoryItemCount);
    }

    public void Uninit()
    {
        Service.Framework.Update -= SaddleBagScanner;
        Service.AddonLifecycle.UnregisterListener(OnSaddleBag);

        windowTitle = string.Empty;
        InventoryItemCount.Clear();
    }
}
