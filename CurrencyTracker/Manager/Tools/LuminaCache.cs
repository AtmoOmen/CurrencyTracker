namespace CurrencyTracker.Manager;

// From KamiLib, used to extract some res
public class LuminaCache<T> : IEnumerable<T> where T : ExcelRow
{
    private readonly Func<uint, T?> searchAction;

    private static LuminaCache<T>? _instance;
    public static LuminaCache<T> Instance => _instance ??= new LuminaCache<T>();

    private LuminaCache(Func<uint, T?>? action = null)
    {
        searchAction = action ?? (row => Service.DataManager.GetExcelSheet<T>()!.GetRow(row));
    }

    private readonly Dictionary<uint, T> cache = new();
    private readonly Dictionary<Tuple<uint, uint>, T> subRowCache = new();

    public ExcelSheet<T> OfLanguage(ClientLanguage language)
    {
        return Service.DataManager.GetExcelSheet<T>(language)!;
    }

    public T? GetRow(uint id)
    {
        if (cache.TryGetValue(id, out var value))
        {
            return value;
        }
        else
        {
            if (searchAction(id) is not { } result) return null;

            return cache[id] = result;
        }
    }

    public T? GetRow(uint row, uint subRow)
    {
        var targetRow = new Tuple<uint, uint>(row, subRow);

        if (subRowCache.TryGetValue(targetRow, out var value))
        {
            return value;
        }
        else
        {
            if (Service.DataManager.GetExcelSheet<T>()!.GetRow(row, subRow) is not { } result) return null;

            return subRowCache[targetRow] = result;
        }
    }

    public IEnumerator<T> GetEnumerator() => Service.DataManager.GetExcelSheet<T>()!.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
