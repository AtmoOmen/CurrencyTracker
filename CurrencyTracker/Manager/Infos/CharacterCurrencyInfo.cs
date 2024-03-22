using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CurrencyTracker.Manager.Infos;

public class CharacterCurrencyInfo
{
    private CharacterInfo? character;

    public CharacterInfo Character
    {
        get => character;
        set
        {
            character = value;
            UpdateCharacterCurrencyAmount();
        }
    }

    public ConcurrentDictionary<uint, long> CurrencyAmount { get; } = new();
    public ConcurrentDictionary<uint, Dictionary<TransactionFileCategoryInfo, long>> SubCurrencyAmount { get; } = new();

    private void UpdateCharacterCurrencyAmount()
    {
        Parallel.ForEach(Service.Config.AllCurrencyID, currencyKey =>
        {
            var amount = CurrencyInfo.GetCharacterCurrencyAmount(currencyKey, Character);
            var subAmount = CurrencyInfo.GetCharacterCurrencyAmountDictionary(currencyKey, Character);

            CurrencyAmount.AddOrUpdate(currencyKey, amount, (_, _) => amount);
            SubCurrencyAmount.AddOrUpdate(currencyKey, subAmount, (_, _) => subAmount);
        });
    }
}
