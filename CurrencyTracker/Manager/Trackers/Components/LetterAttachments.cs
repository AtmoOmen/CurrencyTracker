using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Trackers.Handlers;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using OmenTools.Helpers;

namespace CurrencyTracker.Manager.Trackers.Components;

public class LetterAttachments : ITrackerComponent
{
    public bool Initialized { get; set; }
    private InventoryHandler? inventoryHandler;

    private static TaskHelper? TaskHelper;

    public void Init()
    {
        TaskHelper ??= new TaskHelper { TimeLimitMS = 15_000 };

        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "LetterViewer", OnLetterViewer);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "LetterViewer", OnLetterViewer);
    }

    private unsafe void OnLetterViewer(AddonEvent type, AddonArgs args)
    {
        var addon = (AtkUnitBase*)args.Addon;
        if (addon == null) return;

        switch (type)
        {
            case AddonEvent.PostSetup:
            {
                // 获取全部按钮 The Claim Button
                var buttonNode = addon->GetButtonNodeById(30);
                if (buttonNode == null || !buttonNode->IsEnabled) return;

                inventoryHandler ??= new InventoryHandler();
                HandlerManager.ChatHandler.isBlocked = true;
                break;
            }
            case AddonEvent.PreFinalize:
                var letterSender = MemoryHelper.ReadStringNullTerminated((nint)addon->AtkValues[0].String);
                TaskHelper.DelayNext(1_500);
                TaskHelper.Enqueue(() => EndLetterAttachments(letterSender));
                break;
        }
    }

    private bool? EndLetterAttachments(string letterSender)
    {
        Service.Log.Debug("Letter Closed, Currency Change Check Starts.");
        var items = inventoryHandler?.Items ?? [];
        Tracker.CheckCurrencies(
            items, string.Empty, $"({Service.Lang.GetText("LetterAttachments-LetterFrom", letterSender)})", RecordChangeType.All,
            24);

        HandlerManager.Nullify(ref inventoryHandler);
        HandlerManager.ChatHandler.isBlocked = false;
        Service.Log.Debug("Currency Change Check Completes.");

        return true;
    }

    public void Uninit()
    {
        Service.AddonLifecycle.UnregisterListener(OnLetterViewer);
        HandlerManager.Nullify(ref inventoryHandler);

        TaskHelper?.Abort();
        TaskHelper = null;
    }
}
