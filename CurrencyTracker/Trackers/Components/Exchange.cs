using System.Collections.Generic;
using System.Linq;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tracker;
using CurrencyTracker.Manager.Trackers.Handlers;
using CurrencyTracker.Trackers;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

namespace CurrencyTracker.Manager.Trackers.Components;

public class Exchange : TrackerComponentBase
{

    private static readonly string[] UI =
    [
        "InclusionShop", "CollectablesShop", "FreeCompanyExchange", "FreeCompanyCreditShop", "ShopExchangeCurrency",
        "Shop", "ItemSearch", "ShopExchangeItem", "SkyIslandExchange", "TripleTriadCoinExchange", "FreeCompanyChest",
        "MJIDisposeShop", "GrandCompanyExchange", "ReconstructionBuyback", "ShopExchangeCoin"
    ];

    private static readonly Dictionary<string, uint> WindowUI = new() // Addon Name - Window Node ID
    {
        { "Repair", 38 },
        { "PvpReward", 125 },
        { "Materialize", 16 },
        { "ColorantEquipment", 13 },
        { "MiragePrism", 28 },
        { "HWDSupply", 67 }
    };

    private string currentTargetName = string.Empty;
    internal static bool isOnExchange;
    private string windowName = string.Empty;

    private InventoryHandler? inventoryHandler;

    protected override void OnInit()
    {
        DService.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, UI.Concat(WindowUI.Keys), BeginExchange);
        DService.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, UI.Concat(WindowUI.Keys), EndExchange);
    }

    private void BeginExchange(AddonEvent type, AddonArgs? args)
    {
        if (isOnExchange || SpecialExchange.isOnExchange) return;

        if (args != null && WindowUI.TryGetValue(args.AddonName, out var windowNode))
            windowName = GetWindowTitle(args, windowNode, args.AddonName == "PvpReward" ? [4, 5] : null);
        else
            currentTargetName = DService.Targets.Target?.Name.TextValue ?? string.Empty;

        isOnExchange = true;
        inventoryHandler ??= new InventoryHandler();
        HandlerManager.ChatHandler.IsBlocked = true;
    }

    private void EndExchange(AddonEvent type, AddonArgs args)
    {
        if (SpecialExchange.isOnExchange) return;

        DService.Log.Debug("Exchange Completes, Currency Change Check Starts.");

        var items = inventoryHandler?.Items ?? [];
        TrackerManager.CheckCurrencies(
            items, "",
            $"({(WindowUI.ContainsKey(args.AddonName) ? windowName : Service.Lang.GetText("ExchangeWith", currentTargetName))})",
            RecordChangeType.All, 3);

        windowName = currentTargetName = string.Empty;
        HandlerManager.ChatHandler.IsBlocked = isOnExchange = false;
        HandlerManager.Nullify(ref inventoryHandler);

        DService.Log.Debug("Currency Change Check Completes.");
    }

    protected override void OnUninit()
    {
        DService.AddonLifecycle.UnregisterListener(BeginExchange);
        DService.AddonLifecycle.UnregisterListener(EndExchange);
        HandlerManager.Nullify(ref inventoryHandler);
    }
}
