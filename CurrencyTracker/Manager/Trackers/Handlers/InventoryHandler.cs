using System.Collections.Generic;
using CurrencyTracker.Infos;
using Dalamud.Game.Inventory.InventoryEventArgTypes;

namespace CurrencyTracker.Manager.Trackers.Handlers;

public class InventoryHandler : ITrackerHandler
{
    public bool Initialized { get; set; }
    public bool isBlocked   { get; set; } = false;

    public HashSet<uint> Items { get; set; } = [];

    public InventoryHandler()
    {
        Init();
    }

    public void Init()
    {
        DService.Inventory.InventoryChangedRaw += OnInventoryChangedRaw;
        Initialized = true;
    }

    private void OnInventoryChangedRaw(IReadOnlyCollection<InventoryEventArgs> events)
    {
        foreach (var eventArgs in events)
        {
            Items.Add(eventArgs.Item.ItemId);
            if (P.PI.IsDev) DService.Log.Debug(eventArgs.ToString());
        }
    }

    public void Uninit()
    {
        DService.Inventory.InventoryChangedRaw -= OnInventoryChangedRaw;

        Items.Clear();
    }
}
