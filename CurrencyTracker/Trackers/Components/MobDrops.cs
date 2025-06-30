using System.Collections.Generic;
using System.Linq;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tools;
using CurrencyTracker.Manager.Tracker;
using CurrencyTracker.Manager.Trackers.Handlers;
using CurrencyTracker.Trackers;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Plugin.Services;
using OmenTools.Helpers;
using GameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace CurrencyTracker.Manager.Trackers.Components;

public unsafe class MobDrops : TrackerComponentBase
{

    private static readonly Dictionary<ulong, string> EnemiesList = []; // Game Object ID - Name

    private static bool IsOnCombat;

    private InventoryHandler? inventoryHandler;
    private static TaskHelper? TaskHelper;

    protected override void OnInit()
    {
        TaskHelper ??= new TaskHelper { TimeLimitMS = 10_000 };

        DService.Condition.ConditionChange += OnConditionChange;
        DService.Framework.Update += OnUpdate;
    }

    private void OnConditionChange(ConditionFlag flag, bool value)
    {
        if (flag != ConditionFlag.InCombat || BoundByDuty) return;

        if (value)
        {
            if (TaskHelper.IsBusy || inventoryHandler != null) return;

            HandlerManager.ChatHandler.IsBlocked = true;
            inventoryHandler ??= new InventoryHandler();

            IsOnCombat = true;
            return;
        }

        TaskHelper.Abort();
        TaskHelper.DelayNext(5000);
        TaskHelper.Enqueue(EndMobDropsHandler);
    }

    private static void OnUpdate(IFramework framework)
    {
        if (!IsOnCombat) return;

        var currentTarget = DService.Targets.Target;
        if (currentTarget == null) return;
        var targetName = currentTarget.Name.ExtractText();
        if (EnemiesList.ContainsKey(currentTarget.GameObjectId) || EnemiesList.ContainsValue(targetName)) return;
        if (currentTarget.ObjectKind != ObjectKind.BattleNpc) return;

        var gameObj = (GameObject*)currentTarget.Address;
        if (gameObj == null) return;
        if (gameObj->FateId != 0) return;
        
        EnemiesList[currentTarget.GameObjectId] = targetName;
        DService.Log.Debug($"Added {targetName} to the mob list");
    }

    private bool? EndMobDropsHandler()
    {
        if (DService.Condition[ConditionFlag.InCombat] || EnemiesList.Count <= 0) return true;

        DService.Log.Debug("Combat Ends, Currency Change Check Starts.");
        var items = inventoryHandler?.Items ?? [];
        TrackerManager.CheckCurrencies(
            items, "", $"({Service.Lang.GetText("MobDrops-MobDropsNote", string.Join(", ", EnemiesList.Values.TakeLast(3)))})",
            RecordChangeType.All, 8);

        EnemiesList.Clear();
        HandlerManager.ChatHandler.IsBlocked = false;
        HandlerManager.Nullify(ref inventoryHandler);
        IsOnCombat = false;
        DService.Log.Debug("Currency Change Check Completes.");

        return true;
    }

    protected override void OnUninit()
    {
        DService.Condition.ConditionChange -= OnConditionChange;
        DService.Framework.Update -= OnUpdate;
        IsOnCombat = false;

        HandlerManager.Nullify(ref inventoryHandler);
        TaskHelper?.Abort();
        TaskHelper = null;
    }
}
