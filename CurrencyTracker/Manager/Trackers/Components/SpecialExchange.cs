using System.Collections.Generic;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tools;
using CurrencyTracker.Manager.Trackers.Handlers;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CurrencyTracker.Manager.Trackers.Components;

public class SpecialExchange : ITrackerComponent
{
    public bool Initialized { get; set; }

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

    public void Init()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, UI.Keys, BeginExchange);
    }

    private unsafe void BeginExchange(AddonEvent type, AddonArgs args)
    {
        if (isOnExchange || Exchange.isOnExchange) return;

        var addon = (AtkUnitBase*)args.Addon;
        if (addon == null) return;

        HandlerManager.ChatHandler.isBlocked = true;

        isOnExchange     =   true;
        windowName       =   args.AddonName == "SatisfactionSupply" ? addon->AtkValues[7].String.ExtractText() : GetWindowTitle(args, UI[args.AddonName]);
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
        Service.Framework.Update -= OnFrameworkUpdate;

        Service.Log.Debug("Exchange Completes, Currency Change Check Starts.");

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
