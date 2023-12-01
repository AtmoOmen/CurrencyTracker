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

        private Dictionary<uint, long> currencyAmount = new();

        public Dictionary<uint, long> CurrencyAmount => currencyAmount;

        public void GetCharacterCurrencyAmount()
        {
            foreach (var currency in Plugin.Instance.Configuration.AllCurrencies)
            {
                if (!currencyAmount.TryGetValue(currency.Key, out var value))
                {
                    currencyAmount[currency.Key] = 0;
                }
                var latestTransaction = Transactions.LoadLatestSingleTransaction(currency.Key, Character);
                currencyAmount[currency.Key] = latestTransaction == null ? 0 : latestTransaction.Amount;
            }
        }
    }
}
