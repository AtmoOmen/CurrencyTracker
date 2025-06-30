using System.Collections.Generic;
using System.Linq;
using CurrencyTracker.Infos;
using CurrencyTracker.Trackers;
using Lumina.Excel.Sheets;

namespace CurrencyTracker.Manager.Trackers.Handlers;

public class ItemHandler : TrackerHandlerBase
{
    public static Dictionary<string, uint>? ItemNames   { get; set; }
    public static HashSet<uint>?            ItemIDs     { get; set; }

    protected override void OnInit()
    {
        ItemNames ??= LuminaGetter.Get<Item>()
                           .Where(x => x.ItemSortCategory.RowId != 5 && x.IsUnique == false &&
                                       !string.IsNullOrEmpty(x.Name.ToString()))
                           .DistinctBy(x => x.Name.ToString())
                           .ToDictionary(x => x.Name.ToString(), x => x.RowId);
        ItemIDs ??= [.. ItemNames.Values];
    }

    protected override void OnUninit() { }
}
