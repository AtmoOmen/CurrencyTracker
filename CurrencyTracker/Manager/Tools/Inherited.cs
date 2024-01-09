namespace CurrencyTracker.Manager.Tools;

public class UpdateDictionary<TKey, TValue> : Dictionary<TKey, TValue> where TKey : notnull
{
    public event Action? Update;

    public UpdateDictionary() { }

    public UpdateDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary) { }

    public new void Add(TKey key, TValue value)
    {
        base.Add(key, value);
        Update?.Invoke();
    }

    public new bool Remove(TKey key)
    {
        var removed = base.Remove(key);
        if (removed) Update?.Invoke();
        return removed;
    }
}
