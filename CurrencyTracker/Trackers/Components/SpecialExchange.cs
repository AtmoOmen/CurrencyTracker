using System.Collections.Generic;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tracker;
using CurrencyTracker.Manager.Trackers.Handlers;
using CurrencyTracker.Trackers;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CurrencyTracker.Manager.Trackers.Components;

public class SpecialExchange : TrackerComponentBase
{

    private static readonly Dictionary<string, uint> UI = new() // Addon Name - Window Node ID
    {
        { "GrandCompanySupplyList", 27 },
        { "WeeklyBingoResult", 99 },
        { "ReconstructionBox", 31 },
        { "SatisfactionSupply", 1 }
    };

    internal static bool isOnExchange;
    private static string windowName = string.Empty;

    private InventoryHandler? inventoryHandler;

    protected override void OnInit() => 
        DService.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, UI.Keys, BeginExchange);

    private unsafe void BeginExchange(AddonEvent type, AddonArgs args)
    {
        if (isOnExchange || Exchange.isOnExchange) return;

        var addon = (AtkUnitBase*)args.Addon.Address;
        if (addon == null) return;

        HandlerManager.ChatHandler.IsBlocked = true;

        isOnExchange     =   true;
        windowName       =   args.AddonName == "SatisfactionSupply" ? addon->AtkValues[7].String.ExtractText() : GetWindowTitle(args, UI[args.AddonName]);
        inventoryHandler ??= new InventoryHandler();

        DService.Framework.Update += OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (OccupiedInEvent) return;

        if (!isOnExchange && !Exchange.isOnExchange)
        {
            DService.Framework.Update -= OnFrameworkUpdate;
            return;
        }

        EndExchangeHandler();
    }

    private void EndExchangeHandler()
    {
        if (Exchange.isOnExchange) return;
        DService.Framework.Update -= OnFrameworkUpdate;

        DService.Log.Debug("Exchange Completes, Currency Change Check Starts.");

        isOnExchange = false;

        var items = inventoryHandler?.Items ?? [];
        TrackerManager.CheckCurrencies(items, "", $"({windowName})", RecordChangeType.All, 10);

        windowName = string.Empty;
        HandlerManager.ChatHandler.IsBlocked = false;
        HandlerManager.Nullify(ref inventoryHandler);

        DService.Log.Debug("Currency Change Check Completes.");
    }

    protected override void OnUninit()
    {
        DService.Framework.Update -= OnFrameworkUpdate;
        DService.AddonLifecycle.UnregisterListener(BeginExchange);
        HandlerManager.Nullify(ref inventoryHandler);

        isOnExchange = false;
        windowName = string.Empty;
    }
}
