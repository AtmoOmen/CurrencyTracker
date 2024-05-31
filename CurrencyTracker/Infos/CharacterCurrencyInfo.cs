using System.Collections.Concurrent;
using System.Collections.Generic;
using CurrencyTracker.Manager;

namespace CurrencyTracker.Infos;

public class CharacterCurrencyInfo
{
    public CharacterCurrencyInfo(CharacterInfo character)
    {
        Character = character;
        InitializeCurrencies();
    }

    public CharacterInfo Character { get; set; }

    public ConcurrentDictionary<uint, long>                                          CurrencyAmount    { get; } = new();
    public ConcurrentDictionary<uint, Dictionary<TransactionFileCategoryInfo, long>> SubCurrencyAmount { get; } = new();

    private void InitializeCurrencies()
    {
        foreach (var currencyKey in Service.Config.AllCurrencyID)
        {
            CurrencyAmount[currencyKey] = CurrencyInfo.GetCharacterCurrencyAmount(currencyKey, Character);
            SubCurrencyAmount[currencyKey] = CurrencyInfo.GetCharacterCurrencyAmountDictionary(currencyKey, Character);
        }
    }
}
