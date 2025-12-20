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

    private static readonly string[] NormalAddons =
    [
        "InclusionShop", 
        "CollectablesShop",
        "FreeCompanyExchange",
        "FreeCompanyCreditShop", 
        "ShopExchangeCurrency",
        "Shop", 
        "ItemSearch", 
        "ShopExchangeItem",
        "SkyIslandExchange", 
        "TripleTriadCoinExchange",
        "FreeCompanyChest",
        "MJIDisposeShop", 
        "GrandCompanyExchange", 
        "ReconstructionBuyback", 
        "ShopExchangeCoin"
    ];

    private static readonly HashSet<string> WindowAddons =
    [
        "Repair",
        "PvpReward",
        "Materialize",
        "ColorantEquipment",
        "MiragePrism",
        "HWDSupply"
    ];

    private string currentTargetName = string.Empty;
    internal static bool IsOnExchange;
    private string windowName = string.Empty;

    private InventoryHandler? inventoryHandler;

    protected override void OnInit()
    {
        DService.AddonLifecycle.RegisterListener(AddonEvent.PostSetup,   NormalAddons.Concat(WindowAddons), BeginExchange);
        DService.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, NormalAddons.Concat(WindowAddons), EndExchange);
    }

    private unsafe void BeginExchange(AddonEvent type, AddonArgs? args)
    {
        if (IsOnExchange || SpecialExchange.isOnExchange) return;

        var addon = args.Addon.ToAtkUnitBase();
        if (args != null && addon != null && WindowAddons.TryGetValue(args.AddonName, out _))
            windowName = GetWindowTitle(args, addon->WindowNode->NodeId, args.AddonName == "PvpReward" ? [4, 5] : null);
        else
            currentTargetName = DService.Targets.Target?.Name.TextValue ?? string.Empty;

        IsOnExchange = true;
        inventoryHandler ??= new InventoryHandler();
        HandlerManager.ChatHandler.IsBlocked = true;
    }

    private void EndExchange(AddonEvent type, AddonArgs args)
    {
        if (SpecialExchange.isOnExchange) return;

        DService.Log.Debug("Exchange Completes, Currency Change Check Starts.");

        var items = inventoryHandler?.Items ?? [];
        TrackerManager.CheckCurrencies(
            items, 
            string.Empty,
            $"({(WindowAddons.Contains(args.AddonName) ? windowName : Service.Lang.GetText("ExchangeWith", currentTargetName))})",
            RecordChangeType.All,
            3);

        windowName = currentTargetName = string.Empty;
        HandlerManager.ChatHandler.IsBlocked = IsOnExchange = false;
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
