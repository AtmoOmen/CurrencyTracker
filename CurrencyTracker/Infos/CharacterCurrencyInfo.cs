using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using CurrencyTracker.Internal;
using CurrencyTracker.Manager;

namespace CurrencyTracker.Infos;

public class CharacterCurrencyInfo
{
    public CharacterInfo Character { get; }

    public  ConcurrentDictionary<uint, long> CurrencyAmount { get; set; } = [];

    public ConcurrentDictionary<uint, ConcurrentDictionary<TransactionFileCategoryInfo, long>> SubCurrencyAmount { get; set; } = [];

    public CharacterCurrencyInfo(CharacterInfo character)
    {
        Character = character ?? throw new ArgumentNullException(nameof(character));
        UpdateAllCurrencies();
    }

    public void UpdateAllCurrencies() => (CurrencyAmount, SubCurrencyAmount) = InitCurrencies();

    private (ConcurrentDictionary<uint, long>, ConcurrentDictionary<uint, ConcurrentDictionary<TransactionFileCategoryInfo, long>>) InitCurrencies()
    {
        var currencyAmount = new ConcurrentDictionary<uint, long>();
        var subCurrencyAmount =
            new ConcurrentDictionary<uint, ConcurrentDictionary<TransactionFileCategoryInfo, long>>();

        foreach (var currencyKey in PluginConfig.Instance().AllCurrencies.Keys)
        {
            currencyAmount[currencyKey]    = CurrencyInfo.GetCharacterCurrencyAmount(currencyKey, Character);
            subCurrencyAmount[currencyKey] = new ConcurrentDictionary<TransactionFileCategoryInfo, long>(CurrencyInfo.GetCharacterCurrencyAmountDictionary(currencyKey, Character));
        }

        return (currencyAmount, subCurrencyAmount);
    }
}
