using System;
using System.Collections.Generic;

namespace CurrencyTracker.Infos;

public class UpdateDictionary<TKey, TValue> : Dictionary<TKey, TValue> where TKey : notnull
{
    public event Action? OnUpdate;

    public UpdateDictionary() { }

    public UpdateDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary) { }

    public new void Add(TKey key, TValue value)
    {
        base.Add(key, value);
        OnUpdate?.Invoke();
    }

    public new bool Remove(TKey key)
    {
        var removed = base.Remove(key);
        if (removed) OnUpdate?.Invoke();
        return removed;
    }
}
