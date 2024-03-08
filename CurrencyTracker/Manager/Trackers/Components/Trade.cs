using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CurrencyTracker.Manager.Infos;
using CurrencyTracker.Manager.Trackers.Handlers;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using OmenTools.Helpers;

namespace CurrencyTracker.Manager.Trackers.Components;

public class Trade : ITrackerComponent
{
    public bool Initialized { get; set; }

    private bool isOnTrade;
    private string tradeTargetName = string.Empty;
    private InventoryHandler? inventoryHandler;

    public void Init()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "Trade", StartTrade);
    }

    private unsafe void StartTrade(AddonEvent type, AddonArgs args)
    {
        if (isOnTrade) return;

        isOnTrade = true;

        var TGUI = (AtkUnitBase*)args.Addon;
        if (HelpersOm.IsAddonAndNodesReady(TGUI)) return;

        var textNode = TGUI->GetTextNodeById(17);
        if (textNode == null) return;

        tradeTargetName = textNode->NodeText.FetchText();
        inventoryHandler ??= new InventoryHandler();
        Service.Framework.Update += OnFrameworkUpdate;
        HandlerManager.ChatHandler.isBlocked = true;

        Service.Log.Debug($"Trade Starts with {tradeTargetName}");
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (Service.Condition[ConditionFlag.TradeOpen]) return;

        Service.Framework.Update -= OnFrameworkUpdate;
        Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(_ => EndTrade());
    }

    private void EndTrade()
    {
        if (Service.Condition[ConditionFlag.TradeOpen])
        {
            Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(t => EndTrade());
            return;
        }

        Service.Log.Debug("Trade Ends, Currency Change Check Starts.");

        var items = inventoryHandler?.Items ?? new HashSet<uint>();

        Service.Tracker.CheckCurrencies(items, "", $"({Service.Lang.GetText("TradeWith", tradeTargetName)})",
                                        RecordChangeType.All, 13);
        tradeTargetName = string.Empty;
        HandlerManager.ChatHandler.isBlocked = false;
        HandlerManager.Nullify(ref inventoryHandler);
        isOnTrade = false;

        Service.Log.Debug("Currency Change Check Completes.");
    }

    public void Uninit()
    {
        Service.Framework.Update -= OnFrameworkUpdate;
        Service.AddonLifecycle.UnregisterListener(StartTrade);
        HandlerManager.Nullify(ref inventoryHandler);

        tradeTargetName = string.Empty;
    }
}
