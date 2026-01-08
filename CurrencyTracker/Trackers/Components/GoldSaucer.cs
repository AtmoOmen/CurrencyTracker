using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tracker;
using CurrencyTracker.Trackers;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using OmenTools.Helpers;

namespace CurrencyTracker.Manager.Trackers.Components;

// 过时，需要重写 Outdated, Need Rewrite
public class GoldSaucer : TrackerComponentBase
{
    protected override void OnInit()
    {
        DService.Instance().AddonLifecycle.RegisterListener(AddonEvent.PreSetup, "GoldSaucerReward", GoldSaucerHandler);
        DService.Instance().AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "GoldSaucerReward", GoldSaucerHandler);
        DService.Instance().AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "GoldSaucerReward", GoldSaucerHandler);
    }

    private static unsafe void GoldSaucerHandler(AddonEvent type, AddonArgs args)
    {
        switch (type)
        {
            case AddonEvent.PreSetup:
                HandlerManager.ChatHandler.IsBlocked = true;
                break;
            case AddonEvent.PostSetup:
                var addon = args.Addon.ToStruct();
                if (!addon->IsAddonAndNodesReady()) return;

                var textNode = addon->GetTextNodeById(5);
                if (textNode == null) return;

                var gameName = textNode->NodeText.StringPtr.ExtractText();
                TrackerManager.CheckCurrency(29, string.Empty, $"({gameName})", RecordChangeType.All, 23);
                break;
            case AddonEvent.PreFinalize:
                if (!OccupiedInEvent) HandlerManager.ChatHandler.IsBlocked = false;
                break;
        }
    }

    protected override void OnUninit() => 
        DService.Instance().AddonLifecycle.UnregisterListener(GoldSaucerHandler);
}
