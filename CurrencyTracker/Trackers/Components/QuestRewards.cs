using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tracker;
using CurrencyTracker.Manager.Trackers.Handlers;
using CurrencyTracker.Trackers;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Memory;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using OmenTools.Helpers;

namespace CurrencyTracker.Manager.Trackers.Components;

public class QuestRewards : TrackerComponentBase
{

    private InventoryHandler? inventoryHandler;
    private static TaskHelper? TaskHelper;

    protected override void OnInit()
    {
        TaskHelper ??= new TaskHelper { TimeLimitMS = int.MaxValue };

        DService.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "JournalResult", OnQuestRewards);
        DService.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "JournalResult", OnQuestRewards);
    }

    private unsafe void OnQuestRewards(AddonEvent type, AddonArgs? args)
    {
        var addon = (AtkUnitBase*)args.Addon.Address;
        if (addon == null) return;

        switch (type)
        {
            case AddonEvent.PostSetup:
                inventoryHandler ??= new InventoryHandler();
                HandlerManager.ChatHandler.IsBlocked = true;
                break;
            case AddonEvent.PreFinalize:
                var atkValue  = addon->AtkValues[1];
                var questName = atkValue.Type == 0 ? string.Empty : atkValue.String.ExtractText();
                DService.Log.Debug($"Quest {questName} Ready to Finish!");

                TaskHelper.Enqueue(() => CheckQuestRewards(questName));
                break;
        }
    }

    private bool? CheckQuestRewards(string questName)
    {
        if (OccupiedInEvent || BetweenAreas) return false;

        DService.Log.Debug($"Quest {questName} Finished, Currency Change Check Starts.");
        var items = inventoryHandler?.Items ?? [];
        TrackerManager.CheckCurrencies(items, string.Empty, $"({Service.Lang.GetText("Quest", questName)})",
                                        RecordChangeType.All, 9);
        DService.Log.Debug("Currency Change Check Completes.");

        HandlerManager.ChatHandler.IsBlocked = false;
        HandlerManager.Nullify(ref inventoryHandler);
        return true;
    }

    protected override void OnUninit()
    {
        DService.AddonLifecycle.UnregisterListener(OnQuestRewards);
        HandlerManager.Nullify(ref inventoryHandler);

        TaskHelper?.Abort();
        TaskHelper = null;
    }
}
