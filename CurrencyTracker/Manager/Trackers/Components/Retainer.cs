namespace CurrencyTracker.Manager.Trackers.Components
{
    public class Retainer : ITrackerComponent
    {
        public bool Initialized
        {
            get { return _initialized; }
            set { _initialized = value; }
        }

        public static readonly InventoryType[] RetainerInventories = new InventoryType[]
        {
            InventoryType.RetainerPage1, InventoryType.RetainerPage2, InventoryType.RetainerPage3, InventoryType.RetainerPage4, InventoryType.RetainerGil,
            InventoryType.RetainerCrystals, InventoryType.RetainerPage5, InventoryType.RetainerPage6, InventoryType.RetainerPage7, InventoryType.RetainerMarket
        };

        private bool isOnRetainer = false; // 是否打开了雇员界面
        private string retainerWindowName = string.Empty;
        internal static Dictionary<ulong, Dictionary<uint, long>> InventoryItemCount = new(); // Retainer ID - Currency ID : Amount
        private bool _initialized = false;

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

            _initialized = true;
        }

        // 雇员背包打开后 -> 开始扫描物品栏 / 获取其中物品数量
        private unsafe void OnRetainerInventory(AddonEvent type, AddonArgs args)
        {
            var retainerManager = RetainerManager.Instance();
            if (retainerManager == null) return;

            var retainerID = retainerManager->LastSelectedRetainerId;
            var retainerName = MemoryHelper.ReadStringNullTerminated((IntPtr)retainerManager->GetActiveRetainer()->Name);

            if (type == AddonEvent.PostSetup)
            {
                Service.Framework.Update += RetainerInventoryScanner;

                Service.Tracker.CheckCurrencies(C.CustomCurrencies.Keys, CurrentLocationName, "", RecordChangeType.All, 23, TransactionFileCategory.Retainer, retainerID);
            }

            if (type == AddonEvent.PreFinalize)
            {
                Service.Framework.Update -= RetainerInventoryScanner;
                Service.Tracker.CheckCurrencies(C.CustomCurrencies.Keys, CurrentLocationName, "", RecordChangeType.All, 24, TransactionFileCategory.Retainer, retainerID);
                Service.Tracker.CheckCurrencies(C.CustomCurrencies.Keys, CurrentLocationName, $"({retainerWindowName} {retainerName})", RecordChangeType.All, 24, TransactionFileCategory.Inventory, retainerID);
            }
        }

        private unsafe void RetainerInventoryScanner(IFramework framework)
        {
            var inventoryManager = InventoryManager.Instance();
            var retainerManager = RetainerManager.Instance();

            if (inventoryManager == null || retainerManager == null) return;

            var retainerID = retainerManager->LastSelectedRetainerId;
            Parallel.ForEach(C.CustomCurrencies, currency =>
            {
                long itemCount = 0;
                Parallel.ForEach(RetainerInventories, inventory =>
                {
                    itemCount += inventoryManager->GetItemCountInContainer(currency.Key, inventory);
                    InventoryItemCount[retainerID][currency.Key] = itemCount;
                });
            });
        }

        // 雇员列表打开后 -> 获取雇员 ID 名称 / 获取雇员金币数量
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

                // 检查配置文件的雇员信息
                if (!C.CharacterRetainers[P.CurrentCharacter.ContentID].ContainsKey(retainerID))
                {
                    C.CharacterRetainers[P.CurrentCharacter.ContentID].Add(retainerID, retainerName);
                }
                else
                {
                    C.CharacterRetainers[P.CurrentCharacter.ContentID][retainerID] = retainerName;
                }

                // 添加货币到临时物品存储字典
                if (!InventoryItemCount.TryGetValue(retainerID, out var value))
                {
                    value = new();
                    InventoryItemCount.Add(retainerID, value);
                    foreach(var currency in C.AllCurrencies.Keys)
                    {
                        if (InventoryItemCount[retainerID].ContainsKey(currency)) continue;
                        InventoryItemCount[retainerID].Add(currency, 0);
                    }
                }

                value[1] = retainerGil;

                retainerWindowName = GetWindowTitle(args.Addon, 28);
                Service.Tracker.CheckCurrency(1, CurrentLocationName, "", RecordChangeType.All, 22, TransactionFileCategory.Retainer, retainerID);
                Service.Tracker.CheckCurrency(1, CurrentLocationName, $"({retainerWindowName} {retainerName})", RecordChangeType.All, 22, TransactionFileCategory.Inventory, retainerID);
            }
            C.Save();

            if (!isOnRetainer)
            {
                isOnRetainer = true;
                HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = true;
                Service.Framework.Update += RetainerUIWacther;
            }
        }

        private void RetainerUIWacther(IFramework framework)
        {
            if (!isOnRetainer)
            {
                Service.Framework.Update -= RetainerUIWacther;
                HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = false;
                return;
            }

            if (!Service.Condition[ConditionFlag.OccupiedSummoningBell])
            {
                isOnRetainer = false;
                Service.Framework.Update -= RetainerUIWacther;
                HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = false;
            }
        }


        public void Uninit()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerList", OnRetainerList);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerGrid0", OnRetainerInventory);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "RetainerGrid0", OnRetainerInventory);

            isOnRetainer = false;
            retainerWindowName = string.Empty;
            InventoryItemCount.Clear();

            Service.Framework.Update -= RetainerUIWacther;
            Service.Framework.Update -= RetainerInventoryScanner;

            _initialized = false;
        }
    }
}
