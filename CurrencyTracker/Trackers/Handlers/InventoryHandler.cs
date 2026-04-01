using System.Collections.Generic;
using Dalamud.Game.Inventory.InventoryEventArgTypes;

namespace CurrencyTracker.Trackers.Handlers;

public class InventoryHandler : TrackerHandlerBase
{
    public HashSet<uint> Items { get; set; } = [];

    public InventoryHandler() =>
        Init();

    protected override void OnInit() =>
        DService.Instance().GameInventory.InventoryChangedRaw += OnInventoryChangedRaw;

    private void OnInventoryChangedRaw(IReadOnlyCollection<InventoryEventArgs> events)
    {
        foreach (var eventArgs in events)
        {
            Items.Add(eventArgs.Item.ItemId);
            if (DService.Instance().PI.IsDev) DService.Instance().Log.Debug(eventArgs.ToString());
        }
    }

    protected override void OnUninit()
    {
        DService.Instance().GameInventory.InventoryChangedRaw -= OnInventoryChangedRaw;

        Items.Clear();
    }
}
