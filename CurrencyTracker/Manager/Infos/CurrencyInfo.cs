using System.Threading;
using Lumina.Excel.GeneratedSheets;

namespace CurrencyTracker.Manager.Infos;

public static class CurrencyInfo
{
    public static readonly uint[] DefaultCustomCurrencies = new uint[15]
    {
        20, 21, 22, 25, 27, 28, 29, 10307, 25199, 25200, 26807, 28063, 33913, 33914, 36656
    };

    public static readonly uint[] PresetCurrencies = new uint[3]
    {
        1, GetSpecialTomestoneId(2), GetSpecialTomestoneId(3),
    };

    /// <summary>
    /// Try to get currency name from configuration, if not exists, get the local name from the game client.
    /// </summary>
    /// <param name="currencyID"></param>
    /// <returns></returns>
    public static string GetCurrencyName(uint currencyID)
    {
        return Plugin.Configuration.AllCurrencies.TryGetValue(currencyID, out var currencyName) ? currencyName : GetCurrencyLocalName(currencyID);
    }

    public static string GetCurrencyLocalName(uint currencyID)
    {
        if (LuminaCache<Item>.Instance.GetRow(currencyID) is { } currencyItem)
        {
            var CurrencyName = currencyItem.Name.ToDalamudString().TextValue;

            return CurrencyName;
        }
        else return "Unknown";
    }

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

        Service.Log.Warning($"Failed to get {currencyID} {GetCurrencyLocalName(currencyID)} icon");
        return null;
    }
}
