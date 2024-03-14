using System.Collections.Generic;
using CurrencyTracker.Manager.Infos;
using CurrencyTracker.Manager.Tasks;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

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

    private static TaskManager? TaskManager;

    public void Init()
    {
        TaskManager ??= new TaskManager { TimeLimitMS = int.MaxValue, ShowDebug = false };

        if (!Service.Config.CharacterRetainers.ContainsKey(P.CurrentCharacter.ContentID))
        {
            Service.Config.CharacterRetainers.Add(P.CurrentCharacter.ContentID, new());
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

        for (var i = 0U; i < retainerManager->GetRetainerCount(); i++)
        {
            var retainer = retainerManager->GetRetainerBySortedIndex(i);
            if (retainer == null) break;

            var retainerID = retainer->RetainerID;
            var retainerName = MemoryHelper.ReadSeStringNullTerminated((nint)retainer->Name).FetchText();
            var retainerGil = retainer->Gil;

            var characterRetainers = Service.Config.CharacterRetainers[P.CurrentCharacter.ContentID];

            characterRetainers[retainerID] = retainerName;

            if (!InventoryItemCount.TryGetValue(retainerID, out var itemCount))
            {
                itemCount = new();
                InventoryItemCount[retainerID] = itemCount;
            }

            itemCount[1] = retainerGil;

            retainerWindowName = GetWindowTitle(args.Addon, 28);
            Tracker.CheckCurrencies(retainerCurrencies, CurrentLocationName, "", RecordChangeType.All, 22,
                                            TransactionFileCategory.Retainer, retainerID);
            Tracker.CheckCurrencies(retainerCurrencies, CurrentLocationName,
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
            value = new();
            InventoryItemCount[currentRetainerID] = value;
        }

        switch (type)
        {
            case AddonEvent.PostSetup:
                TaskManager.Enqueue(RetainerInventoryScanner);
                break;
            case AddonEvent.PreFinalize:
                TaskManager.Abort();

                var retainerName =
                    MemoryHelper.ReadStringNullTerminated((nint)retainerManager->GetActiveRetainer()->Name);

                Tracker.CheckCurrencies(value.Keys, "", "", RecordChangeType.All,
                                                24, TransactionFileCategory.Retainer, currentRetainerID);
                Tracker.CheckCurrencies(value.Keys, "",
                                                $"({retainerWindowName} {retainerName})", RecordChangeType.All, 24,
                                                TransactionFileCategory.Inventory, currentRetainerID);
                break;
        }
    }

    private bool? RetainerInventoryScanner()
    {
        var tempDict = InventoryItemCount[currentRetainerID];
        InventoryScanner(RetainerInventories, ref tempDict);
        InventoryItemCount[currentRetainerID] = tempDict;

        return false;
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

        TaskManager?.Abort();
    }
}
