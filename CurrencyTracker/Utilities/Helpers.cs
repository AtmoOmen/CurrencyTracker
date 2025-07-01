using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Transactions;
using CurrencyTracker.Windows;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.Game;
using IntervalUtility;

namespace CurrencyTracker.Utilities;

public static class Helpers
{
    public static string GetSelectedViewName(this TransactionFileCategory category, ulong ID) => category switch
    {
        TransactionFileCategory.Inventory        => Service.Lang.GetText("Inventory"),
        TransactionFileCategory.SaddleBag        => Service.Lang.GetText("SaddleBag"),
        TransactionFileCategory.PremiumSaddleBag => Service.Lang.GetText("PSaddleBag"),
        TransactionFileCategory.Retainer         => Service.Config.CharacterRetainers[P.CurrentCharacter.ContentID][ID],
        _                                        => string.Empty
    };

    public static string GetTransactionViewKeyString(this TransactionFileCategory view, ulong ID) => view switch
    {
        TransactionFileCategory.Inventory        => P.CurrentCharacter.ContentID.ToString(),
        TransactionFileCategory.SaddleBag        => $"{P.CurrentCharacter.ContentID}_SB",
        TransactionFileCategory.PremiumSaddleBag => $"{P.CurrentCharacter.ContentID}_PSB",
        TransactionFileCategory.Retainer         => ID.ToString(),
        _                                        => string.Empty
    };

    public static unsafe void InventoryScanner(
        IEnumerable<InventoryType> inventories, ref Dictionary<uint, long> inventoryItemCount)
    {
        var inventoryManager = InventoryManager.Instance();

        if (inventoryManager == null) return;

        var itemCountDict = new Dictionary<uint, long>();

        foreach (var inventory in inventories)
        {
            var container = inventoryManager->GetInventoryContainer(inventory);
            if (container == null) continue;

            for (var i = 0; i < container->Size; i++)
            {
                var slot = inventoryManager->GetInventorySlot(inventory, i);
                if (slot == null) continue;

                var item = slot->ItemId;
                if (item == 0) continue;

                long itemCount = inventoryManager->GetItemCountInContainer(item, inventory);
                itemCountDict[item] = itemCountDict.TryGetValue(item, out var value) ? value + itemCount : itemCount;
            }
        }

        foreach (var kvp in itemCountDict) inventoryItemCount[kvp.Key] = kvp.Value;

        foreach (var kvp in inventoryItemCount)
            if (!itemCountDict.ContainsKey(kvp.Key) && kvp.Key != 1)
                inventoryItemCount[kvp.Key] = 0;
    }
}
