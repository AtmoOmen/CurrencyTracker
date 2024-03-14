using System.Collections.Generic;
using System.Linq;
using CurrencyTracker.Manager.Infos;
using CurrencyTracker.Manager.Tasks;
using CurrencyTracker.Manager.Tools;
using CurrencyTracker.Manager.Trackers.Handlers;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;

namespace CurrencyTracker.Manager.Trackers.Components;

public class MobDrops : ITrackerComponent
{
    public bool Initialized { get; set; }

    private readonly HashSet<string> enemiesList = new();
    private InventoryHandler? inventoryHandler;

    private static TaskManager? TaskManager;

    public void Init()
    {
        TaskManager ??= new TaskManager() { TimeLimitMS = int.MaxValue, ShowDebug = false };

        Service.Condition.ConditionChange += OnConditionChange;
    }

    private unsafe void OnConditionChange(ConditionFlag flag, bool value)
    {
        if (flag != ConditionFlag.InCombat || Flags.IsBoundByDuty()) return;

        if (value)
        {
            if (TaskManager.IsBusy || inventoryHandler != null || FateManager.Instance()->CurrentFate != null) return;

            HandlerManager.ChatHandler.isBlocked = true;
            inventoryHandler ??= new InventoryHandler();

            TaskManager.Enqueue(TryScanMobsName);
        }
        else
        {
            TaskManager.Abort();
            TaskManager.DelayNext(5000);
            TaskManager.Enqueue(EndMobDropsHandler);
        }
    }

    private bool? TryScanMobsName()
    {
        var target = Service.TargetManager.Target;
        if (target.ObjectKind == ObjectKind.BattleNpc && target is BattleNpc battleNpc &&
            battleNpc.StatusFlags.HasFlag(StatusFlags.Hostile | StatusFlags.InCombat | StatusFlags.WeaponOut))
        {
            enemiesList.Add(battleNpc.Name.FetchText());
        }

        return false;
    }

    private bool? EndMobDropsHandler()
    {
        if (Service.Condition[ConditionFlag.InCombat])
        {
            TaskManager.Abort();
            TaskManager.DelayNext(5000);
            TaskManager.Enqueue(EndMobDropsHandler);
            return true;
        }

        Service.Log.Debug("Combat Ends, Currency Change Check Starts.");
        var items = inventoryHandler?.Items ?? new();
        Service.Tracker.CheckCurrencies(
            items, "", $"({Service.Lang.GetText("MobDrops-MobDropsNote", string.Join(", ", enemiesList.TakeLast(3)))})",
            RecordChangeType.All, 8);

        enemiesList.Clear();
        HandlerManager.ChatHandler.isBlocked = false;
        HandlerManager.Nullify(ref inventoryHandler);
        Service.Log.Debug("Currency Change Check Completes.");

        return true;
    }

    public void Uninit()
    {
        Service.Condition.ConditionChange -= OnConditionChange;
        HandlerManager.Nullify(ref inventoryHandler);
        TaskManager?.Abort();
    }
}
