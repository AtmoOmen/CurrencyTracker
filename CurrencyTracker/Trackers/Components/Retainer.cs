using System.Collections.Generic;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tracker;
using CurrencyTracker.Trackers;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using OmenTools.Helpers;

namespace CurrencyTracker.Manager.Trackers.Components;

public class Retainer : TrackerComponentBase
{

    public static readonly InventoryType[] RetainerInventories =
    [
        InventoryType.RetainerPage1, InventoryType.RetainerPage2, InventoryType.RetainerPage3,
        InventoryType.RetainerPage4,
        InventoryType.RetainerCrystals, InventoryType.RetainerPage5, InventoryType.RetainerPage6,
        InventoryType.RetainerPage7, InventoryType.RetainerMarket
    ];

    private bool isOnRetainer;
    private ulong currentRetainerID;
    private string retainerWindowName = string.Empty;
    private static readonly uint[] retainerCurrencies = [1, 21072]; // Gil and Venture

    internal static Dictionary<ulong, Dictionary<uint, long>> InventoryItemCount = []; // Retainer ID - Currency ID : Amount

    private static TaskHelper? TaskHelper;

    protected override void OnInit()
    {
        TaskHelper ??= new TaskHelper { TimeoutMS = int.MaxValue };

        if (P.CurrentCharacter is { ContentID: var contentID } && !Service.Config.CharacterRetainers.ContainsKey(contentID))
        {
            Service.Config.CharacterRetainers.Add(P.CurrentCharacter.ContentID, []);
            Service.Config.Save();
        }

        DService.Instance().AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerList", OnRetainerList);
        DService.Instance().AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerGrid0", OnRetainerInventory);
        DService.Instance().AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "RetainerGrid0", OnRetainerInventory);
    }

    private unsafe void OnRetainerList(AddonEvent type, AddonArgs args)
    {
        var inventoryManager = InventoryManager.Instance();
        var retainerManager  = RetainerManager.Instance();

        if (inventoryManager == null || retainerManager == null) return;

        for (var i = 0U; i < retainerManager->GetRetainerCount(); i++)
        {
            var retainer = retainerManager->GetRetainerBySortedIndex(i);
            if (retainer == null) break;

            var retainerID = retainer->RetainerId;
            var retainerName = retainer->NameString;
            var retainerGil = retainer->Gil;

            var characterRetainers = Service.Config.CharacterRetainers[P.CurrentCharacter.ContentID];

            characterRetainers[retainerID] = retainerName;

            if (!InventoryItemCount.TryGetValue(retainerID, out var itemCount))
            {
                itemCount = [];
                InventoryItemCount[retainerID] = itemCount;
            }

            itemCount[1] = retainerGil;

            retainerWindowName = args.Addon.ToStruct()->GetWindowTitle();
            TrackerManager.CheckCurrencies(retainerCurrencies, CurrentLocationName, "", RecordChangeType.All, 22,
                                            TransactionFileCategory.Retainer, retainerID);
            TrackerManager.CheckCurrencies(retainerCurrencies, CurrentLocationName,
                                            $"({retainerWindowName} {retainerName})", RecordChangeType.All, 22,
                                            TransactionFileCategory.Inventory, retainerID);
        }

        Service.Config.Save();

        if (!isOnRetainer)
        {
            isOnRetainer = true;
            HandlerManager.ChatHandler.IsBlocked = true;
            DService.Instance().Framework.Update += RetainerUIWatcher;
        }
    }

    private unsafe void OnRetainerInventory(AddonEvent type, AddonArgs args)
    {
        var retainerManager = RetainerManager.Instance();
        if (retainerManager == null) return;

        currentRetainerID = retainerManager->LastSelectedRetainerId;
        if (!InventoryItemCount.TryGetValue(currentRetainerID, out var value))
        {
            value = [];
            InventoryItemCount[currentRetainerID] = value;
        }

        switch (type)
        {
            case AddonEvent.PostSetup:
                TaskHelper.Enqueue(RetainerInventoryScanner);
                break;
            case AddonEvent.PreFinalize:
                TaskHelper.Abort();

                var retainerName = retainerManager->GetActiveRetainer()->NameString;

                TrackerManager.CheckCurrencies(value.Keys, "", "", RecordChangeType.All,
                                                24, TransactionFileCategory.Retainer, currentRetainerID);
                TrackerManager.CheckCurrencies(value.Keys, "",
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
            DService.Instance().Framework.Update -= RetainerUIWatcher;
            HandlerManager.ChatHandler.IsBlocked = false;
            return;
        }

        if (!DService.Instance().Condition[ConditionFlag.OccupiedSummoningBell])
        {
            DService.Instance().Framework.Update -= RetainerUIWatcher;
            isOnRetainer = false;
            currentRetainerID = 0;
            InventoryItemCount.Clear();
            HandlerManager.ChatHandler.IsBlocked = false;
        }
    }

    protected override void OnUninit()
    {
        DService.Instance().AddonLifecycle.UnregisterListener(OnRetainerList);
        DService.Instance().AddonLifecycle.UnregisterListener(OnRetainerInventory);

        isOnRetainer = false;
        retainerWindowName = string.Empty;
        currentRetainerID = 0;
        InventoryItemCount.Clear();

        DService.Instance().Framework.Update -= RetainerUIWatcher;

        TaskHelper?.Abort();
        TaskHelper = null;
    }
}
