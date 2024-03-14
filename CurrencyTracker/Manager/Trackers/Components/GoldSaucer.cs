using CurrencyTracker.Manager.Infos;
using CurrencyTracker.Manager.Tools;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using OmenTools.Helpers;

namespace CurrencyTracker.Manager.Trackers.Components;

// 过时，需要重写 Outdated, Need Rewrite
public class GoldSaucer : ITrackerComponent
{
    public bool Initialized { get; set; }

    public void Init()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreSetup, "GoldSaucerReward", GoldSaucerHandler);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "GoldSaucerReward", GoldSaucerHandler);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "GoldSaucerReward", GoldSaucerHandler);
    }

    private static unsafe void GoldSaucerHandler(AddonEvent type, AddonArgs args)
    {
        switch (type)
        {
            case AddonEvent.PreSetup:
                HandlerManager.ChatHandler.isBlocked = true;
                break;
            case AddonEvent.PostSetup:
                var GSR = (AtkUnitBase*)args.Addon;
                if (!IsAddonAndNodesReady(GSR)) return;

                var textNode = GSR->GetTextNodeById(5);
                if (textNode == null) return;

                var gameName = textNode->NodeText.FetchText();
                Tracker.CheckCurrency(29, "", $"({gameName})", RecordChangeType.All, 23);
                break;
            case AddonEvent.PreFinalize:
                if (!Flags.OccupiedInEvent()) HandlerManager.ChatHandler.isBlocked = false;
                break;
        }
    }

    public void Uninit()
    {
        Service.AddonLifecycle.UnregisterListener(GoldSaucerHandler);
    }
}
