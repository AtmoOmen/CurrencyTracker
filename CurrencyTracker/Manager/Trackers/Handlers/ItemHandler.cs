using System.Collections.Generic;
using System.Linq;
using CurrencyTracker.Infos;
using Lumina.Excel.GeneratedSheets2;

namespace CurrencyTracker.Manager.Trackers.Handlers;

public class ItemHandler : ITrackerHandler
{
    public bool Initialized { get; set; }
    public bool isBlocked { get; set; } = false;
    public static Dictionary<string, uint>? ItemNames { get; set; } = [];
    public static HashSet<uint> ItemIDs { get; set; } = null!;

    public void Init()
    {
        ItemNames = Service.DataManager.GetExcelSheet<Item>()
                           .Where(x => x.ItemSortCategory.Row != 5 && x.IsUnique == false &&
                                       !string.IsNullOrEmpty(x.Name.RawString))
                           .ToDictionary(x => x.Name.RawString, x => x.RowId);
        ItemIDs = [.. ItemNames.Values];
    }

    public void Uninit() { }
}
