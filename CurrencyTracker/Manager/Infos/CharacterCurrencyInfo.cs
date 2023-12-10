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
            foreach (var currency in Plugin.Configuration.AllCurrencies)
            {
                long amount = 0;
                var categories = new[] { TransactionFileCategory.Inventory, TransactionFileCategory.SaddleBag, TransactionFileCategory.PremiumSaddleBag };

                foreach (var category in categories)
                {
                    var currencyAmount = CurrencyInfo.GetCurrencyAmountFromFile(currency.Key, Character, category, 0);
                    amount += currencyAmount == null ? 0 : (long)currencyAmount;
                }

                if (Plugin.Configuration.CharacterRetainers.TryGetValue(character.ContentID, out var value))
                {
                    foreach (var retainer in value)
                    {
                        var currencyAmount = CurrencyInfo.GetCurrencyAmountFromFile(currency.Key, Character, TransactionFileCategory.Retainer, retainer.Key);
                        amount += currencyAmount == null ? 0 : (long)currencyAmount;
                    }
                }
                currencyAmount[currency.Key] = amount;
            }
        }
    }
}
