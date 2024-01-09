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

    public void GetCharacterCurrencyAmount()
    {
        Parallel.ForEach(Plugin.Configuration.AllCurrencyID,
                         currencyKey =>
                         {
                             CurrencyAmount[currencyKey] =
                                 CurrencyInfo.GetCharacterCurrencyAmount(currencyKey, Character);
                         });
    }
}
