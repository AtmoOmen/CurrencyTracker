namespace CurrencyTracker.Manager.Tools
{
    public static class Helpers
    {
        public static bool IsTransactionEqual(TransactionsConvertor t1, TransactionsConvertor t2)
        {
            return t1.TimeStamp == t2.TimeStamp && t1.Amount == t2.Amount && t1.Change == t2.Change && t1.LocationName == t2.LocationName && t1.Note == t2.Note;
        }

        public static bool AreTransactionsEqual(List<TransactionsConvertor> list1, List<TransactionsConvertor> list2)
        {
            if (list1.Count != list2.Count)
            {
                return false;
            }

            for (var i = 0; i < list1.Count; i++)
            {
                if (!IsTransactionEqual(list1[i], list2[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static void OpenDirectory(string path)
        {
            if (path.IsNullOrEmpty())
            {
                return;
            }

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
                {
                    Service.Log.Error("Unsupported OS");
                }
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
            }
            finally
            {
                stream?.Close();
            }

            return false;
        }

        public static unsafe bool IsAddonNodesReady(AtkUnitBase* UI)
        {
            return UI != null && UI->RootNode != null && UI->RootNode->ChildNode != null && UI->UldManager.NodeList != null;
        }

        public static unsafe string GetWindowTitle(AddonArgs args, uint windowNodeID, uint[]? textNodeIDs = null)
        {
            textNodeIDs ??= new uint[] { 3, 4 };

            var UI = (AtkUnitBase*)args.Addon;

            if (UI == null || UI->RootNode == null || UI->RootNode->ChildNode == null || UI->UldManager.NodeList == null)
                return string.Empty;

            var windowNode = (AtkComponentBase*)UI->GetComponentNodeById(windowNodeID);
            if (windowNode == null)
                return string.Empty;

            // 国服和韩服特别处理逻辑 For CN and KR Client
            var bigTitle = windowNode->GetTextNodeById(textNodeIDs[0])->GetAsAtkTextNode()->NodeText.ToString();
            var smallTitle = windowNode->GetTextNodeById(textNodeIDs[1])->GetAsAtkTextNode()->NodeText.ToString();

            var windowTitle = !smallTitle.IsNullOrEmpty() ? smallTitle : bigTitle;

            return windowTitle;
        }

        public static unsafe string GetWindowTitle(nint addon, uint windowNodeID, uint[]? textNodeIDs = null)
        {
            textNodeIDs ??= new uint[] { 3, 4 };

            var UI = (AtkUnitBase*)addon;

            if (UI == null || UI->RootNode == null || UI->RootNode->ChildNode == null || UI->UldManager.NodeList == null)
                return string.Empty;

            var windowNode = (AtkComponentBase*)UI->GetComponentNodeById(windowNodeID);
            if (windowNode == null)
                return string.Empty;

            var textNode3 = windowNode->GetTextNodeById(textNodeIDs[0])->GetAsAtkTextNode()->NodeText.ToString();
            var textNode4 = windowNode->GetTextNodeById(textNodeIDs[1])->GetAsAtkTextNode()->NodeText.ToString();

            var windowTitle = !textNode4.IsNullOrEmpty() ? textNode4 : textNode3;

            return windowTitle;
        }

        public static unsafe void InventoryScanner(IEnumerable<InventoryType> Inventories, ref Dictionary<uint, long> InventoryItemCount)
        {
            var inventoryManager = InventoryManager.Instance();

            if (inventoryManager == null) return;

            var itemCountDict = new Dictionary<uint, long>();

            foreach (var inventory in Inventories)
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

            foreach (var kvp in itemCountDict)
            {
                InventoryItemCount[kvp.Key] = kvp.Value;
            }

            foreach (var kvp in InventoryItemCount)
            {
                if (!itemCountDict.ContainsKey(kvp.Key) && kvp.Key != 1)
                {
                    InventoryItemCount[kvp.Key] = 0;
                }
            }
        }

        public static void Restart(this Timer timer)
        {
            timer.Stop();
            timer.Start();
        }

        public static UpdateDictionary<TKey, TValue> ToUpdateDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> pairs, Func<KeyValuePair<TKey, TValue>, TKey> keySelector, Func<KeyValuePair<TKey, TValue>, TValue> valueSelector) where TKey : notnull
        {
            var updateDictionary = new UpdateDictionary<TKey, TValue>();
            foreach (var pair in pairs)
            {
                updateDictionary.Add(keySelector(pair), valueSelector(pair));
            }
            return updateDictionary;
        }
    }
}
