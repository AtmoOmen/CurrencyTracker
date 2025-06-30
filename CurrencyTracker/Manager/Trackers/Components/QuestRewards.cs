using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tools;
using CurrencyTracker.Manager.Trackers.Handlers;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Memory;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using OmenTools.Helpers;

namespace CurrencyTracker.Manager.Trackers.Components;

public class QuestRewards : ITrackerComponent
{
    public bool Initialized { get; set; }

    private InventoryHandler? inventoryHandler;
    private static TaskHelper? TaskHelper;

    public void Init()
    {
        TaskHelper ??= new TaskHelper { TimeLimitMS = int.MaxValue };

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
                inventoryHandler ??= new InventoryHandler();
                HandlerManager.ChatHandler.isBlocked = true;
                break;
            case AddonEvent.PreFinalize:
                var atkValue  = addon->AtkValues[1];
                var questName = atkValue.Type == 0 ? string.Empty : atkValue.String.ExtractText();
                Service.Log.Debug($"Quest {questName} Ready to Finish!");

                TaskHelper.Enqueue(() => CheckQuestRewards(questName));
                break;
        }
    }

    private bool? CheckQuestRewards(string questName)
    {
        if (Flags.OccupiedInEvent() || Flags.BetweenAreas()) return false;

        Service.Log.Debug($"Quest {questName} Finished, Currency Change Check Starts.");
        var items = inventoryHandler?.Items ?? [];
        Tracker.CheckCurrencies(items, string.Empty, $"({Service.Lang.GetText("Quest", questName)})",
                                        RecordChangeType.All, 9);
        Service.Log.Debug("Currency Change Check Completes.");

        HandlerManager.ChatHandler.isBlocked = false;
        HandlerManager.Nullify(ref inventoryHandler);
        return true;
    }

    public void Uninit()
    {
        Service.AddonLifecycle.UnregisterListener(OnQuestRewards);
        HandlerManager.Nullify(ref inventoryHandler);

        TaskHelper?.Abort();
        TaskHelper = null;
    }
}
