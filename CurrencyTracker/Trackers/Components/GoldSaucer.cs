using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tools;
using CurrencyTracker.Manager.Tracker;
using CurrencyTracker.Trackers;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using OmenTools.Helpers;

namespace CurrencyTracker.Manager.Trackers.Components;

// 过时，需要重写 Outdated, Need Rewrite
public class GoldSaucer : TrackerComponentBase
{
    protected override void OnInit()
    {
        DService.AddonLifecycle.RegisterListener(AddonEvent.PreSetup, "GoldSaucerReward", GoldSaucerHandler);
        DService.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "GoldSaucerReward", GoldSaucerHandler);
        DService.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "GoldSaucerReward", GoldSaucerHandler);
    }

    private static unsafe void GoldSaucerHandler(AddonEvent type, AddonArgs args)
    {
        switch (type)
        {
            case AddonEvent.PreSetup:
                HandlerManager.ChatHandler.IsBlocked = true;
                break;
            case AddonEvent.PostSetup:
                var GSR = (AtkUnitBase*)args.Addon;
                if (!IsAddonAndNodesReady(GSR)) return;

                var textNode = GSR->GetTextNodeById(5);
                if (textNode == null) return;

                var gameName = textNode->NodeText.ExtractText();
                TrackerManager.CheckCurrency(29, "", $"({gameName})", RecordChangeType.All, 23);
                break;
            case AddonEvent.PreFinalize:
                if (!OccupiedInEvent) HandlerManager.ChatHandler.IsBlocked = false;
                break;
        }
    }

    protected override void OnUninit() => 
        DService.AddonLifecycle.UnregisterListener(GoldSaucerHandler);
}
