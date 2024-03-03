using OmenTools.Helpers;

namespace CurrencyTracker.Manager.Trackers.Components;

// 过时，需要重写 Outdated, Need Rewrite
public class FateRewards : ITrackerComponent
{
    public bool Initialized { get; set; }

    public void Init()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreSetup, "FateReward", FateHandler);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "FateReward", FateHandler);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "FateReward", FateHandler);
    }

    private static void FateHandler(AddonEvent type, AddonArgs args)
    {
        switch (type)
        {
            case AddonEvent.PreSetup:
                HandlerManager.ChatHandler.isBlocked = true;
                break;
            case AddonEvent.PostSetup:
                BeginFateHandler(args);
                break;
            case AddonEvent.PreFinalize:
                if (!Flags.OccupiedInEvent()) HandlerManager.ChatHandler.isBlocked = false;
                break;
        }
    }

    private static unsafe void BeginFateHandler(AddonArgs args)
    {
        var FR = (AtkUnitBase*)args.Addon;
        if (!HelpersOm.IsAddonAndNodesReady(FR)) return;
        var textNode = FR->GetTextNodeById(6);
        if (textNode != null)
        {
            var FateName = textNode->NodeText.ToString();
            Service.Tracker.CheckAllCurrencies("", $"({Service.Lang.GetText("Fate", FateName)})",
                                               RecordChangeType.All, 23);
        }
    }

    public void Uninit()
    {
        Service.AddonLifecycle.UnregisterListener(FateHandler);
    }
}
