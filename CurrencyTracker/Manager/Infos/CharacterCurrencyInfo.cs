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
            GetCharacterCurrencyAmount();
        }
    }

    public ConcurrentDictionary<uint, long> CurrencyAmount { get; } = new();
    public ConcurrentDictionary<uint, Dictionary<TransactionFileCategoryInfo, long>> SubCurrencyAmount { get; } = new();

    public void GetCharacterCurrencyAmount()
    {
        Parallel.ForEach(Service.Config.AllCurrencyID,
                         currencyKey =>
                         {
                             CurrencyAmount[currencyKey] =
                                 CurrencyInfo.GetCharacterCurrencyAmount(currencyKey, Character);
                             SubCurrencyAmount[currencyKey] =
                                 CurrencyInfo.GetCharacterCurrencyAmountDictionary(currencyKey, Character);
                         });
    }
}
