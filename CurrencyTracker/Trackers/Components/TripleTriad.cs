using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tracker;
using CurrencyTracker.Manager.Trackers.Handlers;
using CurrencyTracker.Trackers;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using OmenTools.Helpers;

namespace CurrencyTracker.Manager.Trackers.Components;

public class TripleTriad : TrackerComponentBase
{

    private bool isTTOn;
    private string ttRivalName = string.Empty;
    private string ttResultText = string.Empty;
    private InventoryHandler? inventoryHandler;

    protected override void OnInit()
    {
        DService.Instance().AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "TripleTriad", StartTripleTriad);
        DService.Instance().AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "TripleTriadResult", EndTripleTriad);
    }

    private unsafe void StartTripleTriad(AddonEvent type, AddonArgs args)
    {
        isTTOn = true;
        HandlerManager.ChatHandler.IsBlocked = true;

        var addon = InfosOm.TripleTriad;
        if (addon != null) 
            ttRivalName = addon->GetTextNodeById(187)->NodeText.StringPtr.ExtractText();

        inventoryHandler ??= new InventoryHandler();

        DService.Instance().Log.Debug("Triple Triad Starts");
    }

    private unsafe void EndTripleTriad(AddonEvent type, AddonArgs args)
    {
        if (!isTTOn) return;

        isTTOn = false;

        var addon = TripleTriadResult;
        if (addon != null)
        {
            var draw = addon->GetTextNodeById(5)->AtkResNode.IsVisible();
            var lose = addon->GetTextNodeById(4)->AtkResNode.IsVisible();
            var win  = addon->GetTextNodeById(3)->AtkResNode.IsVisible();

            ttResultText = draw ? Service.Lang.GetText("TripleTriad-Draw") :
                           lose ? Service.Lang.GetText("TripleTriad-Loss") :
                           win ? Service.Lang.GetText("TripleTriad-Win") : string.Empty;
        }

        DService.Instance().Log.Debug("Triple Triad Match Ends, Currency Change Check Starts.");

        var items = inventoryHandler?.Items ?? [];
        TrackerManager.CheckCurrencies(
            items, "", $"({Service.Lang.GetText("TripleTriadWith", ttResultText, ttRivalName)})", RecordChangeType.All,
            14);

        ttRivalName = ttResultText = string.Empty;
        HandlerManager.Nullify(ref inventoryHandler);

        DService.Instance().Log.Debug("Currency Change Check Completes.");
    }

    protected override void OnUninit()
    {
        DService.Instance().AddonLifecycle.UnregisterListener(StartTripleTriad);
        DService.Instance().AddonLifecycle.UnregisterListener(EndTripleTriad);
        HandlerManager.Nullify(ref inventoryHandler);

        isTTOn = false;
        ttRivalName = ttResultText = string.Empty;
    }
}
