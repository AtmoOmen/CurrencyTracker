namespace CurrencyTracker.Manager.Trackers.Components
{
    public class Retainer : ITrackerComponent
    {
        public bool Initialized { get; set; } = false;

        public static readonly InventoryType[] RetainerInventories = new InventoryType[]
        {
            InventoryType.RetainerPage1, InventoryType.RetainerPage2, InventoryType.RetainerPage3, InventoryType.RetainerPage4,
            InventoryType.RetainerCrystals, InventoryType.RetainerPage5, InventoryType.RetainerPage6, InventoryType.RetainerPage7, InventoryType.RetainerMarket
        };

        private bool isOnRetainer = false;
        private ulong currentRetainerID = 0;
        private string retainerWindowName = string.Empty;

        internal static Dictionary<ulong, Dictionary<uint, long>> InventoryItemCount = new(); // Retainer ID - Currency ID : Amount

        private readonly Configuration? C = Plugin.Configuration;
        private readonly Plugin? P = Plugin.Instance;

        public void Init()
        {
            if (!C.CharacterRetainers.ContainsKey(P.CurrentCharacter.ContentID))
            {
                C.CharacterRetainers.Add(P.CurrentCharacter.ContentID, new());
                C.Save();
            }

            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerList", OnRetainerList);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerGrid0", OnRetainerInventory);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "RetainerGrid0", OnRetainerInventory);

            Initialized = true;
        }

        private unsafe void OnRetainerList(AddonEvent type, AddonArgs args)
        {
            var inventoryManager = InventoryManager.Instance();
            var retainerManager = RetainerManager.Instance();

            if (inventoryManager == null || retainerManager == null) return;

            for (uint i = 0; i < retainerManager->GetRetainerCount(); i++)
            {
                var retainer = retainerManager->GetRetainerBySortedIndex(i);
                if (retainer == null) continue;

                var retainerID = retainer->RetainerID;
                var retainerName = MemoryHelper.ReadStringNullTerminated((IntPtr)retainer->Name);
                var retainerGil = retainer->Gil;
                Service.Log.Debug($"Successfully get retainer {retainerName} ({retainerID})");

                var characterRetainers = C.CharacterRetainers[P.CurrentCharacter.ContentID];

                if (!characterRetainers.ContainsKey(retainerID))
                {
                    characterRetainers.Add(retainerID, retainerName);
                }
                else
                {
                    characterRetainers[retainerID] = retainerName;
                }

                if (!InventoryItemCount.TryGetValue(retainerID, out var itemCount))
                {
                    itemCount = new();
                    InventoryItemCount.Add(retainerID, itemCount);
                }

                itemCount[1] = retainerGil;

                retainerWindowName = GetWindowTitle(args.Addon, 28);
                Service.Tracker.CheckCurrency(1, CurrentLocationName, "", RecordChangeType.All, 22, TransactionFileCategory.Retainer, retainerID);
                Service.Tracker.CheckCurrency(1, CurrentLocationName, $"({retainerWindowName} {retainerName})", RecordChangeType.All, 22, TransactionFileCategory.Inventory, retainerID);
            }
            C.Save();

            if (!isOnRetainer)
            {
                isOnRetainer = true;
                HandlerManager.ChatHandler.isBlocked = true;
                Service.Framework.Update += RetainerUIWacther;
            }
        }

        private unsafe void OnRetainerInventory(AddonEvent type, AddonArgs args)
        {
            var retainerManager = RetainerManager.Instance();
            if (retainerManager == null) return;

            currentRetainerID = retainerManager->LastSelectedRetainerId;

            if (type == AddonEvent.PostSetup)
            {
                Service.Framework.Update += RetainerInventoryScanner;
            }

            if (type == AddonEvent.PreFinalize)
            {
                var retainerName = MemoryHelper.ReadStringNullTerminated((IntPtr)retainerManager->GetActiveRetainer()->Name);

                Service.Framework.Update -= RetainerInventoryScanner;

                Service.Tracker.CheckCurrencies(InventoryItemCount[currentRetainerID].Keys, "", "", RecordChangeType.All, 24, TransactionFileCategory.Retainer, currentRetainerID);
                Service.Tracker.CheckCurrencies(InventoryItemCount[currentRetainerID].Keys, "", $"({retainerWindowName} {retainerName})", RecordChangeType.All, 24, TransactionFileCategory.Inventory, currentRetainerID);
            }
        }

        private unsafe void RetainerInventoryScanner(IFramework framework)
        {
            var tempDict = InventoryItemCount[currentRetainerID];
            InventoryScanner(RetainerInventories, ref tempDict);
            InventoryItemCount[currentRetainerID] = tempDict;
        }        

        private void RetainerUIWacther(IFramework framework)
        {
            if (!isOnRetainer)
            {
                Service.Framework.Update -= RetainerUIWacther;
                HandlerManager.ChatHandler.isBlocked = false;
                return;
            }

            if (!Service.Condition[ConditionFlag.OccupiedSummoningBell])
            {
                Service.Framework.Update -= RetainerUIWacther;
                isOnRetainer = false;
                currentRetainerID = 0;
                InventoryItemCount.Clear();
                HandlerManager.ChatHandler.isBlocked = false;
            }
        }


        public void Uninit()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerList", OnRetainerList);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerGrid0", OnRetainerInventory);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "RetainerGrid0", OnRetainerInventory);

            isOnRetainer = false;
            retainerWindowName = string.Empty;
            currentRetainerID = 0;
            InventoryItemCount.Clear();

            Service.Framework.Update -= RetainerUIWacther;
            Service.Framework.Update -= RetainerInventoryScanner;

            Initialized = false;
        }
    }
}
