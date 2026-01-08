using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tracker;
using CurrencyTracker.Trackers;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Utility;

namespace CurrencyTracker.Manager.Trackers.Components;

// 过时，需要重写 Outdated, Need Rewrite
public class FateRewards : TrackerComponentBase
{
    protected override void OnInit()
    {
        DService.Instance().AddonLifecycle.RegisterListener(AddonEvent.PreSetup,    "FateReward", FateHandler);
        DService.Instance().AddonLifecycle.RegisterListener(AddonEvent.PostSetup,   "FateReward", FateHandler);
        DService.Instance().AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "FateReward", FateHandler);
    }

    private static unsafe void FateHandler(AddonEvent type, AddonArgs args)
    {
        switch (type)
        {
            case AddonEvent.PreSetup:
                HandlerManager.ChatHandler.IsBlocked = true;
                break;
            case AddonEvent.PostSetup:
                var addon = args.Addon.ToStruct();
                if (!addon->IsAddonAndNodesReady()) return;

                var textNode = addon->GetTextNodeById(6);
                if (textNode == null) return;

                var fateName = textNode->NodeText.StringPtr.ExtractText();
                TrackerManager.CheckAllCurrencies
                (
                    "",
                    $"({Service.Lang.GetText("Fate", fateName)})",
                    RecordChangeType.All,
                    23
                );
                break;
            case AddonEvent.PreFinalize:
                if (!OccupiedInEvent)
                    HandlerManager.ChatHandler.IsBlocked = false;
                break;
        }
    }

    protected override void OnUninit() =>
        DService.Instance().AddonLifecycle.UnregisterListener(FateHandler);
}
