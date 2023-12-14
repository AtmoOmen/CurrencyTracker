namespace CurrencyTracker.Manager.Infos
{
    public class CharacterCurrencyInfo
    {
        private CharacterInfo? character;
        public CharacterInfo Character
        {
            get { return character; }
            set
            {
                character = value;
                GetCharacterCurrencyAmount();
            }
        }
        public ConcurrentDictionary<uint, long> CurrencyAmount => currencyAmount;

        private ConcurrentDictionary<uint, long> currencyAmount = new();
        private readonly List<uint> currencies = Plugin.Configuration.AllCurrencies.Keys.ToList();

        public void GetCharacterCurrencyAmount()
        {
            Parallel.ForEach(currencies, currencyKey =>
            {
                currencyAmount[currencyKey] = CurrencyInfo.GetCharacterCurrencyAmount(currencyKey, Character);
            });
        }
    }
}
