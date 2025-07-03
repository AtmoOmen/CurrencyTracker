using CurrencyTracker.Infos;
using CurrencyTracker.Manager;
using CurrencyTracker.Manager.Tracker;
using CurrencyTracker.Manager.Trackers.Handlers;
using Dalamud.Game.ClientState.Conditions;

namespace CurrencyTracker.Trackers.Components;

public unsafe class Trade : TrackerComponentBase
{
    private static InventoryHandler? InventoryHandler;
    private static TaskHelper? TaskHelper;
    
    private static string TradeTargetName = string.Empty;

    protected override void OnInit()
    {
        TaskHelper ??= new() { TimeLimitMS = 15_000 };

        DService.Condition.ConditionChange += OnConditionChanged;
        if (DService.Condition[ConditionFlag.TradeOpen])
            OnConditionChanged(ConditionFlag.TradeOpen, true);
    }

    private static void OnConditionChanged(ConditionFlag flag, bool value)
    {
        if (flag != ConditionFlag.TradeOpen) return;

        if (value)
        {
            TaskHelper.Enqueue(() => GetTradeTarget());
            TaskHelper.Enqueue(() => DService.Log.Debug($"Trade starts with {TradeTargetName}"));
        }
        else
        {
            if (string.IsNullOrWhiteSpace(TradeTargetName)) return;
            
            DService.Log.Debug($"Trade with {TradeTargetName} completed. Starts to check currency changes.");

            var items = InventoryHandler?.Items ?? [];

            TrackerManager.CheckCurrencies(items, string.Empty, $"({Service.Lang.GetText("TradeWith", TradeTargetName)})", RecordChangeType.All, 13);
            
            HandlerManager.ChatHandler.IsBlocked = false;
            HandlerManager.Nullify(ref InventoryHandler);
            
            TradeTargetName = string.Empty;

            DService.Log.Debug("Currency changes check completes.");
        }
    }

    private static bool GetTradeTarget()
    {
        if (!IsAddonAndNodesReady(InfosOm.Trade)) return false;
        
        var textNode = InfosOm.Trade->GetTextNodeById(17);
        if (textNode == null) return false;

        TradeTargetName = textNode->NodeText.ExtractText();
        if (string.IsNullOrEmpty(TradeTargetName)) return false;

        HandlerManager.ChatHandler.IsBlocked = true;
        InventoryHandler ??= new InventoryHandler();
        
        return true;

    }

    protected override void OnUninit()
    {
        DService.Condition.ConditionChange -= OnConditionChanged;
        
        HandlerManager.Nullify(ref InventoryHandler);
        
        TaskHelper?.Abort();
        TaskHelper = null;
        
        TradeTargetName = string.Empty;
    }
}
