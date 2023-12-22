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

        public void GetCharacterCurrencyAmount()
        {
            Parallel.ForEach(Plugin.Configuration.AllCurrencyID, currencyKey =>
            {
                currencyAmount[currencyKey] = CurrencyInfo.GetCharacterCurrencyAmount(currencyKey, Character);
            });
        }
    }
}
