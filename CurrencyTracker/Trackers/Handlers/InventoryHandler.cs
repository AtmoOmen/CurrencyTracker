using System.Collections.Generic;
using CurrencyTracker.Infos;
using CurrencyTracker.Trackers;
using Dalamud.Game.Inventory.InventoryEventArgTypes;

namespace CurrencyTracker.Manager.Trackers.Handlers;

public class InventoryHandler : TrackerHandlerBase
{

    public HashSet<uint> Items { get; set; } = [];

    public InventoryHandler()
    {
        Init();
    }

    protected override void OnInit()
    {
        DService.Instance().Inventory.InventoryChangedRaw += OnInventoryChangedRaw;
    }

    private void OnInventoryChangedRaw(IReadOnlyCollection<InventoryEventArgs> events)
    {
        foreach (var eventArgs in events)
        {
            Items.Add(eventArgs.Item.ItemId);
            if (P.PI.IsDev) DService.Instance().Log.Debug(eventArgs.ToString());
        }
    }

    protected override void OnUninit()
    {
        DService.Instance().Inventory.InventoryChangedRaw -= OnInventoryChangedRaw;

        Items.Clear();
    }
}
