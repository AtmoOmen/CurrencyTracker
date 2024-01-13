namespace CurrencyTracker.Manager.Infos;

public enum TransactionFileCategory
{
    Inventory = 0,
    Retainer = 1,
    SaddleBag = 2,
    PremiumSaddleBag = 3
}

public class TransactionFileCategoryInfo
{
    public TransactionFileCategory Category { get; set; }
    public ulong Id { get; set; }
}
