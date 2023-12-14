using System.Threading;

namespace CurrencyTracker.Manager.Infos;

public static class CurrencyInfo
{
    public static readonly List<uint> defaultCurrenciesToAdd = new()
    {
        20, 21, 22, 25, 27, 28, 29, 10307, 25199, 25200, 26807, 28063, 33913, 33914, 36656
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
    public static unsafe long GetCurrencyAmount(uint currencyID, TransactionFileCategory category = 0, ulong ID = 0)
    {
        return category switch
        {
            TransactionFileCategory.Inventory => InventoryManager.Instance()->GetInventoryItemCount(currencyID),
            TransactionFileCategory.SaddleBag => SaddleBag.InventoryItemCount[currencyID],
            TransactionFileCategory.PremiumSaddleBag => PremiumSaddleBag.InventoryItemCount[currencyID],
            TransactionFileCategory.Retainer => Retainer.InventoryItemCount[ID][currencyID],
            _ => 0,
        };
    }

    // 获取指定角色指定货币总数量
    public static long GetCharacterCurrencyAmount(uint currencyID, CharacterInfo character)
    {
        var amount = 0L;
        var categories = new[] { TransactionFileCategory.Inventory, TransactionFileCategory.SaddleBag, TransactionFileCategory.PremiumSaddleBag };

        Parallel.ForEach(categories, category =>
        {
            var currencyAmount = CurrencyInfo.GetCurrencyAmountFromFile(currencyID, character, category, 0);

            Interlocked.Add(ref amount, currencyAmount == null ? 0 : (long)currencyAmount);
        });

        if (Plugin.Configuration.CharacterRetainers.TryGetValue(character.ContentID, out var value))
        {
            Parallel.ForEach(value, retainer =>
            {
                var currencyAmount = CurrencyInfo.GetCurrencyAmountFromFile(currencyID, character, TransactionFileCategory.Retainer, retainer.Key);

                Interlocked.Add(ref amount, currencyAmount == null ? 0 : (long)currencyAmount);
            });
        }

        return amount;
    }

    // 获取数据文件中最新一条数据的货币数量
    public static long? GetCurrencyAmountFromFile(uint currencyID, CharacterInfo character, TransactionFileCategory category = 0, ulong ID = 0)
    {
        var latestTransaction = LoadLatestSingleTransaction(currencyID, character, category, ID);

        return latestTransaction?.Amount;
    }

    private static uint GetSpecialTomestoneId(int row)
    {
        return LuminaCache<TomestonesItem>.Instance
            .Where(tomestone => tomestone.Tomestones.Row == row)
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
}
