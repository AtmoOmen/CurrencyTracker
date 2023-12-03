namespace CurrencyTracker.Manager.Infos;

public static class CurrencyInfo
{
    public static readonly List<uint> defaultCurrenciesToAdd = new()
    {
        20, 21, 22, 25, 27, 28, 29, 10307, 25199, 25200, 26807, 28063, 33913, 33914, 36656
    };

    public static readonly InventoryType[] RetainersInventory = new InventoryType[]
    {
        InventoryType.RetainerPage1, InventoryType.RetainerPage2, InventoryType.RetainerPage3, InventoryType.RetainerPage4, InventoryType.RetainerGil,
        InventoryType.RetainerCrystals, InventoryType.RetainerPage5, InventoryType.RetainerPage6, InventoryType.RetainerPage7,
    };

    // 存储一般货币的ID的字典（这里的string非货币名）
    public static readonly Dictionary<uint, string> PresetCurrencies = new()
    {
        { 1, "Gil" },
        { GetSpecialTomestoneId(2), "NonLimitedTomestone"},
        { GetSpecialTomestoneId(3), "LimitedTomestone"}
    };

    // 传入货币ID后，获取货币于当前语言环境的名称
    public static string CurrencyLocalName(uint currencyID)
    {
        if (LuminaCache<Item>.Instance.GetRow(currencyID) is { } currencyItem)
        {
            var CurrencyName = currencyItem.Name.ToDalamudString().TextValue;

            return CurrencyName;
        }
        else return "Unknown";
    }

    // 传入货币ID后，获取货币当前的数量
    public static unsafe long GetCurrencyAmount(uint currencyID)
    {
        return InventoryManager.Instance()->GetInventoryItemCount(currencyID);
    }

    private static uint GetSpecialTomestoneId(int row)
    {
        return LuminaCache<TomestonesItem>.Instance
            .Where(tomestone => tomestone.Tomestones.Row == row)
            .First()
            .Item.Row;
    }

    private static uint GetNonLimitedTomestoneId()
    {
        return LuminaCache<TomestonesItem>.Instance
            .Where(tomestone => tomestone.Tomestones.Row is 2)
            .First()
            .Item.Row;
    }

    private static uint GetLimitedTomestoneId()
    {
        return LuminaCache<TomestonesItem>.Instance
            .Where(tomestone => tomestone.Tomestones.Row is 3)
            .First()
            .Item.Row;
    }

    public static IDalamudTextureWrap? GetIcon(uint currencyID)
    {
        if (Service.DataManager.GetExcelSheet<Item>()!.GetRow(currencyID) is { Icon: var iconId })
        {
            var iconFlags = ITextureProvider.IconFlags.HiRes;

            return Service.TextureProvider.GetIcon(iconId, iconFlags);
        }

        Service.Log.Warning($"Failed to get {currencyID} {CurrencyLocalName(currencyID)} icon");
        return null;
    }

    public static long GetRetainerAmount(uint currencyID)
    {
        unsafe
        {
            InventoryManager* inventoryManagerPtr = InventoryManager.Instance();

            long itemCount = 0;
            foreach (var flag in RetainersInventory)
            {
                itemCount += inventoryManagerPtr->GetItemCountInContainer(currencyID, flag);
            }

            return itemCount;
        }
    }

    public static ulong GetRetainerID()
    {
        unsafe
        {
            uint SomeGil = 0;
            var retainerManager = RetainerManager.Instance();
            if (retainerManager != null)
            {
                for (uint i = 0; i < retainerManager->GetRetainerCount(); i++)
                {
                    var retainer = retainerManager->GetRetainerBySortedIndex(i);
                    if (retainer != null)
                    {
                        SomeGil += retainer->Gil;
                        Service.Log.Debug($"SomeGil:{SomeGil}");
                    }
                }
            }

            return SomeGil;
        }
    }
}
