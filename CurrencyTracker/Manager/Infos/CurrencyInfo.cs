using Lumina.Excel.GeneratedSheets2;

namespace CurrencyTracker.Manager.Infos;

public static class CurrencyInfo
{
    public static readonly uint[] DefaultCustomCurrencies = new uint[15]
    {
        20, 21, 22, 25, 27, 28, 29, 10307, 25199, 25200, 26807, 28063, 33913, 33914, 36656
    };

    public static readonly uint[] PresetCurrencies = new uint[3]
    {
        1, GetSpecialTomestoneId(2), GetSpecialTomestoneId(3)
    };

    /// <summary>
    ///     Try to get currency name from configuration, if not exists, get the local name from the game client.
    /// </summary>
    /// <param name="currencyID"></param>
    /// <returns></returns>
    public static string GetCurrencyName(uint currencyID)
    {
        return Service.Config.AllCurrencies.TryGetValue(currencyID, out var currencyName)
                   ? currencyName
                   : GetCurrencyLocalName(currencyID);
    }

    public static string GetCurrencyLocalName(uint currencyID)
    {
        if (LuminaCache<Item>.Instance.GetRow(currencyID) is { } currencyItem)
        {
            var currencyName = currencyItem.Name.ToDalamudString().TextValue;

            return currencyName;
        }

        return "Unknown";
    }

    public static unsafe long GetCurrencyAmount(uint currencyID, TransactionFileCategory category = 0, ulong ID = 0)
    {
        return category switch
        {
            TransactionFileCategory.Inventory => InventoryManager.Instance()->GetInventoryItemCount(currencyID),
            TransactionFileCategory.SaddleBag =>
                SaddleBag.InventoryItemCount.GetValueOrDefault(currencyID, 0),
            TransactionFileCategory.PremiumSaddleBag =>
                PremiumSaddleBag.InventoryItemCount.GetValueOrDefault(currencyID, 0),
            TransactionFileCategory.Retainer =>
                Retainer.InventoryItemCount.TryGetValue(ID, out var retainer) &&
                retainer.TryGetValue(currencyID, out var retainerAmount)
                    ? retainerAmount
                    : 0,

            _ => 0
        };
    }


    public static long GetCharacterCurrencyAmount(uint currencyID, CharacterInfo character)
    {
        var amount = 0L;
        var categories = new[]
        {
            TransactionFileCategory.Inventory, TransactionFileCategory.SaddleBag,
            TransactionFileCategory.PremiumSaddleBag
        };

        foreach (var category in categories)
        {
            var currencyAmount = GetCurrencyAmountFromFile(currencyID, character, category);
            amount += currencyAmount ?? 0;
        }

        if (Service.Config.CharacterRetainers.TryGetValue(character.ContentID, out var value))
        {
            foreach (var retainer in value)
            {
                var currencyAmount =
                    GetCurrencyAmountFromFile(currencyID, character, TransactionFileCategory.Retainer, retainer.Key);
                amount += currencyAmount ?? 0;
            }
        }

        return amount;
    }

    public static Dictionary<TransactionFileCategoryInfo, long> GetCharacterCurrencyAmountDictionary(
        uint currencyID, CharacterInfo character)
    {
        var amountDic = new Dictionary<TransactionFileCategoryInfo, long>();

        foreach (var category in new[]
                 {
                     TransactionFileCategory.Inventory, TransactionFileCategory.SaddleBag,
                     TransactionFileCategory.PremiumSaddleBag
                 }) AddCurrencyAmountToDictionary(currencyID, character, category, 0, amountDic);

        if (Service.Config.CharacterRetainers.TryGetValue(character.ContentID, out var retainers))
        {
            foreach (var retainer in retainers)
                AddCurrencyAmountToDictionary(currencyID, character, TransactionFileCategory.Retainer, retainer.Key,
                                              amountDic);
        }

        return amountDic;

        void AddCurrencyAmountToDictionary(
            uint currencyID, CharacterInfo character, TransactionFileCategory category, ulong ID,
            IDictionary<TransactionFileCategoryInfo, long> dictionary)
        {
            var currencyAmount = GetCurrencyAmountFromFile(currencyID, character, category, ID);
            var key = new TransactionFileCategoryInfo { Category = category, Id = ID };
            dictionary[key] = currencyAmount ?? 0;
        }
    }


    public static long? GetCurrencyAmountFromFile(
        uint currencyID, CharacterInfo character, TransactionFileCategory category = 0, ulong ID = 0)
    {
        var latestTransaction = TransactionsHandler.LoadLatestSingleTransaction(currencyID, character, category, ID);

        return latestTransaction?.Amount;
    }

    private static uint GetSpecialTomestoneId(int row)
    {
        return LuminaCache<TomestonesItem>.Instance
                                          .First(tomestone => tomestone.Tomestones.Row == row)
                                          .Item.Row;
    }

    public static IDalamudTextureWrap? GetIcon(uint currencyID)
    {
        if (Service.DataManager.GetExcelSheet<Item>()!.GetRow(currencyID) is { Icon: var iconId })
            return Service.TextureProvider.GetIcon(iconId);

        Service.Log.Warning($"Failed to get {currencyID} {GetCurrencyLocalName(currencyID)} icon");
        return null;
    }
}
