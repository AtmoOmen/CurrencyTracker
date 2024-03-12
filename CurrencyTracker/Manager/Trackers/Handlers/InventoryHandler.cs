using System.Collections.Generic;
using CurrencyTracker.Manager.Infos;
using Dalamud.Game.Inventory.InventoryEventArgTypes;

namespace CurrencyTracker.Manager.Trackers.Handlers;

public class InventoryHandler : ITrackerHandler
{
    public bool Initialized { get; set; }
    public bool isBlocked { get; set; } = false;

    public HashSet<uint> Items { get; set; } = new();

    public InventoryHandler()
    {
        Init();
    }

    public void Init()
    {
        Service.GameInventory.InventoryChangedRaw += OnInventoryChangedRaw;
        Initialized = true;
    }

    private void OnInventoryChangedRaw(IReadOnlyCollection<InventoryEventArgs> events)
    {
        foreach (var eventArgs in events)
        {
            Items.Add(eventArgs.Item.ItemId);
            if (P.PluginInterface.IsDev) Service.Log.Debug(eventArgs.ToString());
        }
    }

    public void Uninit()
    {
        Service.GameInventory.InventoryChangedRaw -= OnInventoryChangedRaw;

        Items.Clear();
    }
}
