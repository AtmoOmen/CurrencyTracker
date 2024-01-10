namespace CurrencyTracker.Manager.Tools;

public static class Helpers
{
    public static void OpenDirectory(string path)
    {
        if (path.IsNullOrEmpty()) return;

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
            {
                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", $"-R \"{filePath}\"");
            }
            else
                Service.Log.Error("Unsupported OS");
        }
        catch(Exception ex)
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
            TransactionFileCategory.Retainer => Plugin.Configuration.CharacterRetainers[
                Plugin.Instance.CurrentCharacter.ContentID][id],
            _ => string.Empty
        };
        return text;
    }

    public static unsafe bool IsAddonNodesReady(AtkUnitBase* UI)
    {
        return UI != null && UI->RootNode != null && UI->RootNode->ChildNode != null && UI->UldManager.NodeList != null;
    }

    public static unsafe string GetWindowTitle(AddonArgs args, uint windowNodeID, uint[]? textNodeIDs = null)
    {
        textNodeIDs ??= [3, 4];

        var UI = (AtkUnitBase*)args.Addon;

        if (UI == null || UI->RootNode == null || UI->RootNode->ChildNode == null || UI->UldManager.NodeList == null)
            return string.Empty;

        var windowNode = (AtkComponentBase*)UI->GetComponentNodeById(windowNodeID);
        if (windowNode == null)
            return string.Empty;

        var bigTitle = windowNode->GetTextNodeById(textNodeIDs[0])->GetAsAtkTextNode()->NodeText.ToString();
        var smallTitle = windowNode->GetTextNodeById(textNodeIDs[1])->GetAsAtkTextNode()->NodeText.ToString();

        var windowTitle = !smallTitle.IsNullOrEmpty() ? smallTitle : bigTitle;

        return windowTitle;
    }

    public static unsafe string GetWindowTitle(nint addon, uint windowNodeID, uint[]? textNodeIDs = null)
    {
        textNodeIDs ??= [3, 4];

        var UI = (AtkUnitBase*)addon;

        if (UI == null || UI->RootNode == null || UI->RootNode->ChildNode == null || UI->UldManager.NodeList == null)
            return string.Empty;

        var windowNode = (AtkComponentBase*)UI->GetComponentNodeById(windowNodeID);
        if (windowNode == null)
            return string.Empty;

        var bigTitle = windowNode->GetTextNodeById(textNodeIDs[0])->GetAsAtkTextNode()->NodeText.ToString();
        var smallTitle = windowNode->GetTextNodeById(textNodeIDs[1])->GetAsAtkTextNode()->NodeText.ToString();

        var windowTitle = !smallTitle.IsNullOrEmpty() ? smallTitle : bigTitle;

        return windowTitle;
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

            for (var i = 0; i < 34; i++)
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

    public static void Restart(this Timer timer)
    {
        timer.Stop();
        timer.Start();
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
}
