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

public class LetterAttachments : TrackerComponentBase
{
    private InventoryHandler? inventoryHandler;

    private static TaskHelper? TaskHelper;

    protected override void OnInit()
    {
        TaskHelper ??= new TaskHelper { TimeoutMS = 15_000 };

        DService.Instance().AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "LetterViewer", OnLetterViewer);
        DService.Instance().AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "LetterViewer", OnLetterViewer);
    }

    private unsafe void OnLetterViewer(AddonEvent type, AddonArgs args)
    {
        var addon = (AtkUnitBase*)args.Addon.Address;
        if (addon == null) return;

        switch (type)
        {
            case AddonEvent.PostSetup:
            {
                // 获取全部按钮 The Claim Button
                var buttonNode = addon->GetComponentButtonById(30);
                if (buttonNode == null || !buttonNode->IsEnabled) return;

                inventoryHandler ??= new InventoryHandler();
                HandlerManager.ChatHandler.IsBlocked = true;
                break;
            }
            case AddonEvent.PreFinalize:
                var atkValue     = addon->AtkValues[0];
                var letterSender = atkValue.Type == 0 ? string.Empty : atkValue.String.ExtractText();
                TaskHelper.DelayNext(1_500);
                TaskHelper.Enqueue(() => EndLetterAttachments(letterSender));
                break;
        }
    }

    private bool? EndLetterAttachments(string letterSender)
    {
        DService.Instance().Log.Debug("Letter Closed, Currency Change Check Starts.");
        var items = inventoryHandler?.Items ?? [];
        TrackerManager.CheckCurrencies(
            items, string.Empty, $"({Service.Lang.GetText("LetterAttachments-LetterFrom", letterSender)})", RecordChangeType.All,
            24);

        HandlerManager.Nullify(ref inventoryHandler);
        HandlerManager.ChatHandler.IsBlocked = false;
        DService.Instance().Log.Debug("Currency Change Check Completes.");

        return true;
    }

    protected override void OnUninit()
    {
        DService.Instance().AddonLifecycle.UnregisterListener(OnLetterViewer);
        HandlerManager.Nullify(ref inventoryHandler);

        TaskHelper?.Abort();
        TaskHelper = null;
    }
}
