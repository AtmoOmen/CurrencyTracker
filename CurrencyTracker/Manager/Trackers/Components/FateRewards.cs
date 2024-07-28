using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tools;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
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

    private static unsafe void FateHandler(AddonEvent type, AddonArgs args)
    {
        switch (type)
        {
            case AddonEvent.PreSetup:
                HandlerManager.ChatHandler.isBlocked = true;
                break;
            case AddonEvent.PostSetup:
                var FR = (AtkUnitBase*)args.Addon;
                if (!IsAddonAndNodesReady(FR)) return;

                var textNode = FR->GetTextNodeById(6);
                if (textNode == null) return;

                var fateName = textNode->NodeText.ExtractText();
                Tracker.CheckAllCurrencies("", $"({Service.Lang.GetText("Fate", fateName)})",
                                                   RecordChangeType.All, 23);
                break;
            case AddonEvent.PreFinalize:
                if (!Flags.OccupiedInEvent()) HandlerManager.ChatHandler.isBlocked = false;
                break;
        }
    }

    public void Uninit()
    {
        Service.AddonLifecycle.UnregisterListener(FateHandler);
    }
}
