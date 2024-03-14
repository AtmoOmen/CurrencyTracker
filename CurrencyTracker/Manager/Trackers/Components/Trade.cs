using CurrencyTracker.Manager.Infos;
using CurrencyTracker.Manager.Tasks;
using CurrencyTracker.Manager.Trackers.Handlers;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Component.GUI;
using OmenTools.Helpers;

namespace CurrencyTracker.Manager.Trackers.Components;

public unsafe class Trade : ITrackerComponent
{
    public bool Initialized { get; set; }

    private string tradeTargetName = string.Empty;
    private InventoryHandler? inventoryHandler;
    private static TaskManager? TaskManager;

    public void Init()
    {
        TaskManager ??= new TaskManager { AbortOnTimeout = true, TimeLimitMS = 30000, ShowDebug = false };

        Service.Condition.ConditionChange += OnConditionChanged;
    }

    private void OnConditionChanged(ConditionFlag flag, bool value)
    {
        if (flag != ConditionFlag.TradeOpen) return;

        if (value)
        {
            TaskManager.Enqueue(GetTradeTarget);
        }
        else
        {
            Service.Log.Debug("Trade Ends, Currency Change Check Starts.");

            var items = inventoryHandler?.Items ?? new();

            Service.Tracker.CheckCurrencies(items, "", $"({Service.Lang.GetText("TradeWith", tradeTargetName)})",
                                            RecordChangeType.All, 13);
            tradeTargetName = string.Empty;
            HandlerManager.ChatHandler.isBlocked = false;
            HandlerManager.Nullify(ref inventoryHandler);

            Service.Log.Debug("Currency Change Check Completes.");
        }
    }

    private bool? GetTradeTarget()
    {
        if (TryGetAddonByName<AtkUnitBase>("Trade", out var addon) && HelpersOm.IsAddonAndNodesReady(addon))
        {
            var textNode = addon->GetTextNodeById(17);
            if (textNode == null) return false;

            tradeTargetName = textNode->NodeText.FetchText();
            if (string.IsNullOrEmpty(tradeTargetName)) return false;

            inventoryHandler ??= new InventoryHandler();
            HandlerManager.ChatHandler.isBlocked = true;

            Service.Log.Debug($"Trade Starts with {tradeTargetName}");

            return true;
        }

        return false;
    }

    public void Uninit()
    {
        Service.Condition.ConditionChange -= OnConditionChanged;
        HandlerManager.Nullify(ref inventoryHandler);
        tradeTargetName = string.Empty;
        TaskManager?.Abort();
    }
}
