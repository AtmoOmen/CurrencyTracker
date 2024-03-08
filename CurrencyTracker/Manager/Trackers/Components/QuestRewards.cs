using CurrencyTracker.Manager.Infos;
using CurrencyTracker.Manager.Tools;
using CurrencyTracker.Manager.Trackers.Handlers;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Memory;
using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CurrencyTracker.Manager.Trackers.Components;

public class QuestRewards : ITrackerComponent
{
    public bool Initialized { get; set; }

    private string questName = string.Empty;

    private InventoryHandler? inventoryHandler;
    private static TaskManager? TaskManager;

    public void Init()
    {
        TaskManager ??= new TaskManager { TimeLimitMS = int.MaxValue, ShowDebug = false };

        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "JournalResult", OnQuestRewards);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "JournalResult", OnQuestRewards);
    }

    private unsafe void OnQuestRewards(AddonEvent type, AddonArgs? args)
    {
        var addon = (AtkUnitBase*)args.Addon;
        if (addon == null) return;

        switch (type)
        {
            case AddonEvent.PostSetup:
                questName = MemoryHelper.ReadStringNullTerminated((nint)addon->AtkValues[1].String);
                inventoryHandler ??= new InventoryHandler();
                HandlerManager.ChatHandler.isBlocked = true;
                Service.Log.Debug($"Quest {questName} Ready to Finish!");
                break;
            case AddonEvent.PreFinalize:
                TaskManager.Enqueue(CheckQuestRewards);
                break;
        }
    }

    private bool? CheckQuestRewards()
    {
        if (Flags.OccupiedInEvent() || Flags.BetweenAreas()) return false;

        Service.Log.Debug($"Quest {questName} Finished, Currency Change Check Starts.");
        var items = inventoryHandler?.Items ?? new();
        Service.Tracker.CheckCurrencies(items, "", $"({Service.Lang.GetText("Quest", questName)})",
                                        RecordChangeType.All, 9);
        Service.Log.Debug("Currency Change Check Completes.");

        HandlerManager.ChatHandler.isBlocked = false;
        ResetQuestState();
        return true;
    }

    private void ResetQuestState()
    {
        questName = string.Empty;
        HandlerManager.Nullify(ref inventoryHandler);
    }

    public void Uninit()
    {
        Service.AddonLifecycle.UnregisterListener(OnQuestRewards);
        ResetQuestState();
        TaskManager?.Abort();
    }
}
