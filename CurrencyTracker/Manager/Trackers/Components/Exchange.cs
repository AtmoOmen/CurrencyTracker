using System.Collections.Generic;
using System.Linq;
using CurrencyTracker.Manager.Infos;
using CurrencyTracker.Manager.Trackers.Handlers;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

namespace CurrencyTracker.Manager.Trackers.Components;

public class Exchange : ITrackerComponent
{
    public bool Initialized { get; set; }

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

    public void Init()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, UI.Concat(WindowUI.Keys), BeginExchange);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, UI.Concat(WindowUI.Keys), EndExchange);
    }

    private void BeginExchange(AddonEvent type, AddonArgs? args)
    {
        if (isOnExchange || SpecialExchange.isOnExchange) return;

        if (args != null && WindowUI.TryGetValue(args.AddonName, out var windowNode))
            windowName = GetWindowTitle(args, windowNode, args.AddonName == "PvpReward" ? [4, 5] : null);
        else
            currentTargetName = Service.TargetManager.Target?.Name.TextValue ?? string.Empty;

        isOnExchange = true;
        inventoryHandler ??= new InventoryHandler();
        HandlerManager.ChatHandler.isBlocked = true;
    }

    private void EndExchange(AddonEvent type, AddonArgs args)
    {
        if (SpecialExchange.isOnExchange) return;

        Service.Log.Debug("Exchange Completes, Currency Change Check Starts.");

        var items = inventoryHandler?.Items ?? [];
        Tracker.CheckCurrencies(
            items, "",
            $"({(WindowUI.ContainsKey(args.AddonName) ? windowName : Service.Lang.GetText("ExchangeWith", currentTargetName))})",
            RecordChangeType.All, 3);

        windowName = currentTargetName = string.Empty;
        HandlerManager.ChatHandler.isBlocked = isOnExchange = false;
        HandlerManager.Nullify(ref inventoryHandler);

        Service.Log.Debug("Currency Change Check Completes.");
    }

    public void Uninit()
    {
        Service.AddonLifecycle.UnregisterListener(BeginExchange);
        Service.AddonLifecycle.UnregisterListener(EndExchange);
        HandlerManager.Nullify(ref inventoryHandler);
    }
}
