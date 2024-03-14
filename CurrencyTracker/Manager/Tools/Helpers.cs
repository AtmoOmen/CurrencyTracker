using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using CurrencyTracker.Manager.Infos;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using IntervalUtility;
using OmenTools.ImGuiOm;

namespace CurrencyTracker.Manager.Tools;

public static class Helpers
{
    public static void OpenDirectory(string path)
    {
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = $"/c start \"\" \"{path}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = path
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = path
                });
            }
            else
                Service.Log.Error("Unsupported OS");
        }
        catch (Exception ex)
        {
            Service.Log.Error($"Error :{ex.Message}");
        }
    }

    public static void OpenAndSelectFile(string filePath)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start("open", $"-R \"{filePath}\"");
            else
                Service.Log.Error("Unsupported OS");
        }
        catch (Exception ex)
        {
            Service.Log.Error($"Error :{ex.Message}");
        }
    }

    public static bool IsFileLocked(FileInfo file)
    {
        FileStream? stream = null;

        try
        {
            stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException)
        {
            return true;
        } finally
        {
            stream?.Close();
        }

        return false;
    }

    public static string GetSelectedViewName(TransactionFileCategory category, ulong id)
    {
        var text = category switch
        {
            TransactionFileCategory.Inventory => Service.Lang.GetText("Inventory"),
            TransactionFileCategory.SaddleBag => Service.Lang.GetText("SaddleBag"),
            TransactionFileCategory.PremiumSaddleBag => Service.Lang.GetText("PSaddleBag"),
            TransactionFileCategory.Retainer => Service.Config.CharacterRetainers[
                P.CurrentCharacter.ContentID][id],
            _ => string.Empty
        };
        return text;
    }

    public static unsafe bool TryGetAddonByName<T>(string Addon, out T* AddonPtr) where T : unmanaged
    {
        var a = Service.GameGui.GetAddonByName(Addon, 1);
        if (a == IntPtr.Zero)
        {
            AddonPtr = null;
            return false;
        }

        AddonPtr = (T*)a;
        return true;
    }

    public static string GetTransactionViewKeyString(TransactionFileCategory view, ulong ID)
    {
        return view switch
        {
            TransactionFileCategory.Inventory => P.CurrentCharacter.ContentID.ToString(),
            TransactionFileCategory.SaddleBag => $"{P.CurrentCharacter.ContentID}_SB",
            TransactionFileCategory.PremiumSaddleBag => $"{P.CurrentCharacter.ContentID}_PSB",
            TransactionFileCategory.Retainer => ID.ToString(),
            _ => string.Empty
        };
    }

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

                var item = slot->ItemID;
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

    public static string ToIntervalString<T>(this Interval<T> interval) where T : struct, IComparable
    {
        return
            $"{(interval.Start == null ? "(-∞" : $"[{interval.Start}")},{(interval.End == null ? "+∞)" : $"{interval.End}]")}";
    }

    public static UpdateDictionary<TKey, TValue> ToUpdateDictionary<TKey, TValue>(
        this IEnumerable<KeyValuePair<TKey, TValue>> pairs, Func<KeyValuePair<TKey, TValue>, TKey> keySelector,
        Func<KeyValuePair<TKey, TValue>, TValue> valueSelector) where TKey : notnull
    {
        var updateDictionary = new UpdateDictionary<TKey, TValue>();
        foreach (var pair in pairs) updateDictionary.Add(keySelector(pair), valueSelector(pair));
        return updateDictionary;
    }

    public static void PagingComponent(Action firstPageAction, Action previousPageAction, Action nextPageAction, Action lastPageAction)
    {
        if (ImGuiOm.ButtonIcon("FirstPage", FontAwesomeIcon.Backward))
            firstPageAction.Invoke();

        ImGui.SameLine();
        if (ImGui.ArrowButton("PreviousPage", ImGuiDir.Left))
            previousPageAction.Invoke();

        ImGui.SameLine();
        if (ImGui.ArrowButton("NextPage", ImGuiDir.Right))
            nextPageAction.Invoke();

        ImGui.SameLine();
        if (ImGuiOm.ButtonIcon("LastPage", FontAwesomeIcon.Forward))
            lastPageAction.Invoke();

        if (ImGui.IsWindowFocused(ImGuiFocusedFlags.ChildWindows) && ImGui.GetIO().MouseWheel > 0)
            previousPageAction.Invoke();
        if (ImGui.IsWindowFocused(ImGuiFocusedFlags.ChildWindows) && ImGui.GetIO().MouseWheel < 0)
            nextPageAction.Invoke();
    }
}
