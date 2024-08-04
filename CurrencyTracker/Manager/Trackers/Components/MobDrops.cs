using System.Collections.Generic;
using System.Linq;
using CurrencyTracker.Helpers.TaskHelper;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tools;
using CurrencyTracker.Manager.Trackers.Handlers;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Plugin.Services;
using GameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace CurrencyTracker.Manager.Trackers.Components;

public unsafe class MobDrops : ITrackerComponent
{
    public bool Initialized { get; set; }

    private static readonly Dictionary<ulong, string> EnemiesList = []; // Object ID - Name

    private static bool IsOnCombat;

    private InventoryHandler? inventoryHandler;
    private static TaskHelper? TaskHelper;

    public void Init()
    {
        TaskHelper ??= new TaskHelper { TimeLimitMS = 10000 };

        Service.Condition.ConditionChange += OnConditionChange;
        Service.Framework.Update += OnUpdate;
    }

    private void OnConditionChange(ConditionFlag flag, bool value)
    {
        if (flag != ConditionFlag.InCombat || Flags.IsBoundByDuty()) return;

        if (value)
        {
            if (TaskHelper.IsBusy || inventoryHandler != null) return;

            HandlerManager.ChatHandler.isBlocked = true;
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

        var currentTarget = Service.Target.Target;
        if (currentTarget == null) return;
        var targetName = currentTarget.Name.ExtractText();
        if (EnemiesList.ContainsKey(currentTarget.GameObjectId) || EnemiesList.ContainsValue(targetName)) return;
        if (currentTarget.ObjectKind != ObjectKind.BattleNpc) return;

        var gameObj = (GameObject*)currentTarget.Address;
        if (gameObj == null) return;
        if (gameObj->FateId != 0) return;
        
        EnemiesList[currentTarget.GameObjectId] = targetName;
        Service.Log.Debug($"Added {targetName} to the mob list");
    }

    private bool? EndMobDropsHandler()
    {
        if (Service.Condition[ConditionFlag.InCombat] || EnemiesList.Count <= 0) return true;

        Service.Log.Debug("Combat Ends, Currency Change Check Starts.");
        var items = inventoryHandler?.Items ?? [];
        Tracker.CheckCurrencies(
            items, "", $"({Service.Lang.GetText("MobDrops-MobDropsNote", string.Join(", ", EnemiesList.Values.TakeLast(3)))})",
            RecordChangeType.All, 8);

        EnemiesList.Clear();
        HandlerManager.ChatHandler.isBlocked = false;
        HandlerManager.Nullify(ref inventoryHandler);
        IsOnCombat = false;
        Service.Log.Debug("Currency Change Check Completes.");

        return true;
    }

    public void Uninit()
    {
        Service.Condition.ConditionChange -= OnConditionChange;
        Service.Framework.Update -= OnUpdate;
        IsOnCombat = false;

        HandlerManager.Nullify(ref inventoryHandler);
        TaskHelper?.Abort();
        TaskHelper = null;
    }
}
