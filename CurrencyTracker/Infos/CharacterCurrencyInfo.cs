using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using CurrencyTracker.Manager;

namespace CurrencyTracker.Infos;

public class CharacterCurrencyInfo
{
    public CharacterInfo Character { get; }

    private ConcurrentDictionary<uint, long> _currencyAmount = [];
    public IReadOnlyDictionary<uint, long> CurrencyAmount => _currencyAmount;

    private ConcurrentDictionary<uint, IReadOnlyDictionary<TransactionFileCategoryInfo, long>> _subCurrencyAmount = [];
    public IReadOnlyDictionary<uint, IReadOnlyDictionary<TransactionFileCategoryInfo, long>> SubCurrencyAmount => _subCurrencyAmount;

    public CharacterCurrencyInfo(CharacterInfo character)
    {
        Character = character ?? throw new ArgumentNullException(nameof(character));
        UpdateAllCurrencies();
    }

    public void UpdateAllCurrencies() => (_currencyAmount, _subCurrencyAmount) = InitCurrencies();

    private (ConcurrentDictionary<uint, long>,
        ConcurrentDictionary<uint, IReadOnlyDictionary<TransactionFileCategoryInfo, long>>) InitCurrencies()
    {
        var currencyAmount = new ConcurrentDictionary<uint, long>();
        var subCurrencyAmount =
            new ConcurrentDictionary<uint, IReadOnlyDictionary<TransactionFileCategoryInfo, long>>();

        foreach (var currencyKey in Service.Config.AllCurrencyID)
        {
            currencyAmount[currencyKey] = CurrencyInfo.GetCharacterCurrencyAmount(currencyKey, Character);
            subCurrencyAmount[currencyKey] = CurrencyInfo.GetCharacterCurrencyAmountDictionary(currencyKey, Character);
        }

        return (currencyAmount, subCurrencyAmount);
    }
}
