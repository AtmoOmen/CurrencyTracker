using System.Collections.Generic;
using CurrencyTracker.Manager.Infos;
using CurrencyTracker.Manager.Tools;
using CurrencyTracker.Manager.Trackers.Handlers;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;

namespace CurrencyTracker.Manager.Trackers.Components;

public class SpecialExchange : ITrackerComponent
{
    public bool Initialized { get; set; }

    private static readonly Dictionary<string, uint> UI = new() // Addon Name - Window Node ID
    {
        { "GrandCompanySupplyList", 27 },
        { "WeeklyBingoResult", 99 },
        { "ReconstructionBox", 31 }
    };

    internal static bool isOnExchange;
    private string windowName = string.Empty;

    private InventoryHandler? inventoryHandler;

    public void Init()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, UI.Keys, BeginExchange);
    }

    private void BeginExchange(AddonEvent type, AddonArgs args)
    {
        if (isOnExchange || Exchange.isOnExchange) return;

        HandlerManager.ChatHandler.isBlocked = true;

        isOnExchange = true;
        windowName = GetWindowTitle(args, UI[args.AddonName]);
        inventoryHandler ??= new InventoryHandler();

        Service.Framework.Update += OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (Flags.OccupiedInEvent()) return;

        if (!isOnExchange && !Exchange.isOnExchange)
        {
            Service.Framework.Update -= OnFrameworkUpdate;
            return;
        }

        EndExchangeHandler();
    }

    private void EndExchangeHandler()
    {
        if (Exchange.isOnExchange) return;

        Service.Log.Debug("Exchange Completes, Currency Change Check Starts.");

        Service.Framework.Update -= OnFrameworkUpdate;
        isOnExchange = false;

        var items = inventoryHandler?.Items ?? [];
        Tracker.CheckCurrencies(items, "", $"({windowName})", RecordChangeType.All, 10);

        windowName = string.Empty;
        HandlerManager.ChatHandler.isBlocked = false;
        HandlerManager.Nullify(ref inventoryHandler);

        Service.Log.Debug("Currency Change Check Completes.");
    }

    public void Uninit()
    {
        Service.Framework.Update -= OnFrameworkUpdate;
        Service.AddonLifecycle.UnregisterListener(BeginExchange);
        HandlerManager.Nullify(ref inventoryHandler);

        isOnExchange = false;
        windowName = string.Empty;
    }
}
