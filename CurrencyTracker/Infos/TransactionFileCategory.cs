namespace CurrencyTracker.Infos;

public enum TransactionFileCategory
{
    Inventory = 0,
    Retainer = 1,
    SaddleBag = 2,
    PremiumSaddleBag = 3
}

public class TransactionFileCategoryInfo(TransactionFileCategory category, ulong id)
{
    public TransactionFileCategory Category { get; set; } = category;
    public ulong ID { get; set; } = id;
}
