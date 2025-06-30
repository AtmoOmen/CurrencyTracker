using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tracker;
using CurrencyTracker.Manager.Trackers.Handlers;
using CurrencyTracker.Trackers;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Component.GUI;
using OmenTools.Helpers;

namespace CurrencyTracker.Manager.Trackers.Components;

public unsafe class Trade : TrackerComponentBase
{
    private string tradeTargetName = string.Empty;
    private InventoryHandler? inventoryHandler;
    private static TaskHelper? TaskHelper;

    protected override void OnInit()
    {
        TaskHelper ??= new TaskHelper { TimeLimitMS = 30000 };

        DService.Condition.ConditionChange += OnConditionChanged;
    }

    private void OnConditionChanged(ConditionFlag flag, bool value)
    {
        if (flag != ConditionFlag.TradeOpen) return;

        if (value)
        {
            TaskHelper.Enqueue(GetTradeTarget);
        }
        else
        {
            DService.Log.Debug("Trade Ends, Currency Change Check Starts.");

            var items = inventoryHandler?.Items ?? [];

            TrackerManager.CheckCurrencies(items, "", $"({Service.Lang.GetText("TradeWith", tradeTargetName)})",
                                            RecordChangeType.All, 13);
            tradeTargetName = string.Empty;
            HandlerManager.ChatHandler.IsBlocked = false;
            HandlerManager.Nullify(ref inventoryHandler);

            DService.Log.Debug("Currency Change Check Completes.");
        }
    }

    private bool? GetTradeTarget()
    {
        if (TryGetAddonByName<AtkUnitBase>("Trade", out var addon) && IsAddonAndNodesReady(addon))
        {
            var textNode = addon->GetTextNodeById(17);
            if (textNode == null) return false;

            tradeTargetName = textNode->NodeText.ExtractText();
            if (string.IsNullOrEmpty(tradeTargetName)) return false;

            inventoryHandler ??= new InventoryHandler();
            HandlerManager.ChatHandler.IsBlocked = true;

            DService.Log.Debug($"Trade Starts with {tradeTargetName}");

            return true;
        }

        return false;
    }

    protected override void OnUninit()
    {
        DService.Condition.ConditionChange -= OnConditionChanged;
        HandlerManager.Nullify(ref inventoryHandler);
        tradeTargetName = string.Empty;
        TaskHelper?.Abort();
    }
}
