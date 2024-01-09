namespace CurrencyTracker.Manager.Trackers.Components;

public class QuestRewards : ITrackerComponent
{
    public bool Initialized { get; set; }

    private bool isReadyFinish;
    private string questName = string.Empty;

    private InventoryHandler? inventoryHandler;

    public void Init()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "JournalResult", OnQuestRewards);

        Initialized = true;
    }

    private void OnQuestRewards(AddonEvent type, AddonArgs? args)
    {
        if (isReadyFinish) return;

        BeginQuestHandler();
    }

    private unsafe void BeginQuestHandler()
    {
        ResetQuestState();

        var JR = (AtkUnitBase*)Service.GameGui.GetAddonByName("JournalResult");
        if (JR == null || !IsAddonNodesReady(JR)) return;

        questName = JR->GetTextNodeById(30)->NodeText.ToString();
        var buttonNode = JR->GetNodeById(37);
        if (questName.IsNullOrEmpty() || buttonNode == null) return;

        isReadyFinish = true;
        inventoryHandler = new InventoryHandler();
        HandlerManager.ChatHandler.isBlocked = true;
        Service.Framework.Update += OnFrameworkUpdate;

        Service.Log.Debug($"Quest {questName} Ready to Finish!");
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (Flags.OccupiedInEvent() || Flags.BetweenAreas()) return;

        Service.Framework.Update -= OnFrameworkUpdate;
        Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(t => EndQuestHandler());
    }

    private void EndQuestHandler()
    {
        if (Flags.OccupiedInEvent() || Flags.BetweenAreas())
        {
            Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(t => EndQuestHandler());
            return;
        }

        ;

        Service.Log.Debug($"Quest {questName} Finished, Currency Change Check Starts.");

        isReadyFinish = false;

        var items = inventoryHandler?.Items ?? new HashSet<uint>();
        Service.Tracker.CheckCurrencies(items, "", $"({Service.Lang.GetText("Quest", questName)})",
                                        RecordChangeType.All, 9);

        ResetQuestState();

        Service.Log.Debug("Currency Change Check Completes.");
    }

    private void ResetQuestState()
    {
        Service.Framework.Update -= OnFrameworkUpdate;
        HandlerManager.ChatHandler.isBlocked = false;
        isReadyFinish = false;
        questName = string.Empty;
        HandlerManager.Nullify(ref inventoryHandler);
    }

    public void Uninit()
    {
        Service.Framework.Update -= OnFrameworkUpdate;
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "JournalResult", OnQuestRewards);
        HandlerManager.Nullify(ref inventoryHandler);

        Initialized = false;
    }
}
