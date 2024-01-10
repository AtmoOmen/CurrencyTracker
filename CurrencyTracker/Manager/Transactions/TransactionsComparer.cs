namespace CurrencyTracker.Manager.Transactions;

public class TransactionComparer : IEqualityComparer<TransactionsConvertor>
{
    public bool Equals(TransactionsConvertor? x, TransactionsConvertor? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        return x.TimeStamp == y.TimeStamp && x.Amount == y.Amount && x.Change == y.Change &&
               x.LocationName == y.LocationName && x.Note == y.Note;
    }

    public int GetHashCode(TransactionsConvertor? obj)
    {
        return obj is null ? 0 :
                   HashCode.Combine(obj.TimeStamp, obj.Amount, obj.Change, obj.LocationName, obj.Note);
    }
}
