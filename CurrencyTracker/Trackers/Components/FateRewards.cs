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
public class FateRewards : TrackerComponentBase
{

    protected override void OnInit()
    {
        DService.AddonLifecycle.RegisterListener(AddonEvent.PreSetup, "FateReward", FateHandler);
        DService.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "FateReward", FateHandler);
        DService.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "FateReward", FateHandler);
    }

    private static unsafe void FateHandler(AddonEvent type, AddonArgs args)
    {
        switch (type)
        {
            case AddonEvent.PreSetup:
                HandlerManager.ChatHandler.IsBlocked = true;
                break;
            case AddonEvent.PostSetup:
                var FR = (AtkUnitBase*)args.Addon;
                if (!IsAddonAndNodesReady(FR)) return;

                var textNode = FR->GetTextNodeById(6);
                if (textNode == null) return;

                var fateName = textNode->NodeText.ExtractText();
                TrackerManager.CheckAllCurrencies("", $"({Service.Lang.GetText("Fate", fateName)})",
                                                   RecordChangeType.All, 23);
                break;
            case AddonEvent.PreFinalize:
                if (!OccupiedInEvent) 
                    HandlerManager.ChatHandler.IsBlocked = false;
                break;
        }
    }

    protected override void OnUninit()
    {
        DService.AddonLifecycle.UnregisterListener(FateHandler);
    }
}
