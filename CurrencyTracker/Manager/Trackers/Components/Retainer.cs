namespace CurrencyTracker.Manager.Trackers.Components;

public class Retainer : ITrackerComponent
{
    public bool Initialized { get; set; }

    public static readonly InventoryType[] RetainerInventories =
    {
        InventoryType.RetainerPage1, InventoryType.RetainerPage2, InventoryType.RetainerPage3,
        InventoryType.RetainerPage4,
        InventoryType.RetainerCrystals, InventoryType.RetainerPage5, InventoryType.RetainerPage6,
        InventoryType.RetainerPage7, InventoryType.RetainerMarket
    };

    private bool isOnRetainer;
    private ulong currentRetainerID;
    private string retainerWindowName = string.Empty;
    private static readonly uint[] retainerCurrencies = new uint[2] { 1, 21072 }; // Gil and Venture

    internal static Dictionary<ulong, Dictionary<uint, long>>
        InventoryItemCount = new(); // Retainer ID - Currency ID : Amount

    public void Init()
    {
        if (!Service.Config.CharacterRetainers.ContainsKey(P.CurrentCharacter.ContentID))
        {
            Service.Config.CharacterRetainers.Add(P.CurrentCharacter.ContentID, new Dictionary<ulong, string>());
            Service.Config.Save();
        }

        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerList", OnRetainerList);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerGrid0", OnRetainerInventory);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "RetainerGrid0", OnRetainerInventory);
    }

    private unsafe void OnRetainerList(AddonEvent type, AddonArgs args)
    {
        var inventoryManager = InventoryManager.Instance();
        var retainerManager = RetainerManager.Instance();

        if (inventoryManager == null || retainerManager == null) return;

        for (var i = 0U; i < 10; i++)
        {
            var retainer = retainerManager->GetRetainerBySortedIndex(i);
            if (retainer == null) continue;

            var retainerID = retainer->RetainerID;
            var retainerName = MemoryHelper.ReadStringNullTerminated((nint)retainer->Name);
            var retainerGil = retainer->Gil;
            Service.Log.Debug($"Successfully get retainer {retainerName} ({retainerID})");

            var characterRetainers = Service.Config.CharacterRetainers[P.CurrentCharacter.ContentID];

            characterRetainers[retainerID] = retainerName;

            if (!InventoryItemCount.TryGetValue(retainerID, out var itemCount))
            {
                itemCount = new Dictionary<uint, long>();
                InventoryItemCount[retainerID] = new Dictionary<uint, long>();
            }

            itemCount[1] = retainerGil;

            retainerWindowName = GetWindowTitle(args.Addon, 28);
            Service.Tracker.CheckCurrencies(retainerCurrencies, CurrentLocationName, "", RecordChangeType.All, 22,
                                            TransactionFileCategory.Retainer, retainerID);
            Service.Tracker.CheckCurrencies(retainerCurrencies, CurrentLocationName,
                                            $"({retainerWindowName} {retainerName})", RecordChangeType.All, 22,
                                            TransactionFileCategory.Inventory, retainerID);
        }

        Service.Config.Save();

        if (!isOnRetainer)
        {
            isOnRetainer = true;
            HandlerManager.ChatHandler.isBlocked = true;
            Service.Framework.Update += RetainerUIWatcher;
        }
    }

    private unsafe void OnRetainerInventory(AddonEvent type, AddonArgs args)
    {
        var retainerManager = RetainerManager.Instance();
        if (retainerManager == null) return;

        currentRetainerID = retainerManager->LastSelectedRetainerId;
        if (!InventoryItemCount.TryGetValue(currentRetainerID, out var value))
        {
            value = new Dictionary<uint, long>();
            InventoryItemCount[currentRetainerID] = value;
        }

        switch (type)
        {
            case AddonEvent.PostSetup:
                Service.Framework.Update += RetainerInventoryScanner;
                break;
            case AddonEvent.PreFinalize:

                Service.Framework.Update -= RetainerInventoryScanner;
                Service.Framework.Update -= RetainerInventoryScanner;

                var retainerName =
                    MemoryHelper.ReadStringNullTerminated((nint)retainerManager->GetActiveRetainer()->Name);

                Service.Tracker.CheckCurrencies(value.Keys, "", "", RecordChangeType.All,
                                                24, TransactionFileCategory.Retainer, currentRetainerID);
                Service.Tracker.CheckCurrencies(value.Keys, "",
                                                $"({retainerWindowName} {retainerName})", RecordChangeType.All, 24,
                                                TransactionFileCategory.Inventory, currentRetainerID);
                break;
        }
    }

    private void RetainerInventoryScanner(IFramework framework)
    {
        var tempDict = InventoryItemCount[currentRetainerID];
        InventoryScanner(RetainerInventories, ref tempDict);
        InventoryItemCount[currentRetainerID] = tempDict;
    }

    private void RetainerUIWatcher(IFramework framework)
    {
        if (!isOnRetainer)
        {
            Service.Framework.Update -= RetainerUIWatcher;
            HandlerManager.ChatHandler.isBlocked = false;
            return;
        }

        if (!Service.Condition[ConditionFlag.OccupiedSummoningBell])
        {
            Service.Framework.Update -= RetainerUIWatcher;
            isOnRetainer = false;
            currentRetainerID = 0;
            InventoryItemCount.Clear();
            HandlerManager.ChatHandler.isBlocked = false;
        }
    }

    public void Uninit()
    {
        Service.AddonLifecycle.UnregisterListener(OnRetainerList);
        Service.AddonLifecycle.UnregisterListener(OnRetainerInventory);

        isOnRetainer = false;
        retainerWindowName = string.Empty;
        currentRetainerID = 0;
        InventoryItemCount.Clear();

        Service.Framework.Update -= RetainerUIWatcher;
        Service.Framework.Update -= RetainerInventoryScanner;
    }
}
