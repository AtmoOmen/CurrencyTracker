using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Trackers.Handlers;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using OmenTools.Helpers;

namespace CurrencyTracker.Manager.Trackers.Components;

public class TripleTriad : ITrackerComponent
{
    public bool Initialized { get; set; }

    private bool isTTOn;
    private string ttRivalName = string.Empty;
    private string ttResultText = string.Empty;
    private InventoryHandler? inventoryHandler;

    public void Init()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "TripleTriad", StartTripleTriad);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "TripleTriadResult", EndTripleTriad);
    }

    private unsafe void StartTripleTriad(AddonEvent type, AddonArgs args)
    {
        isTTOn = true;
        HandlerManager.ChatHandler.isBlocked = true;

        var TTGui = (AtkUnitBase*)Service.GameGui.GetAddonByName("TripleTriad");
        if (TTGui != null) ttRivalName = TTGui->GetTextNodeById(187)->NodeText.ExtractText();

        inventoryHandler ??= new InventoryHandler();

        Service.Log.Debug("Triple Triad Starts");
    }

    private unsafe void EndTripleTriad(AddonEvent type, AddonArgs args)
    {
        if (!isTTOn) return;

        isTTOn = false;

        var TTRGui = (AtkUnitBase*)Service.GameGui.GetAddonByName("TripleTriadResult");
        if (TTRGui != null)
        {
            var draw = TTRGui->GetTextNodeById(5)->AtkResNode.IsVisible();
            var lose = TTRGui->GetTextNodeById(4)->AtkResNode.IsVisible();
            var win = TTRGui->GetTextNodeById(3)->AtkResNode.IsVisible();

            ttResultText = draw ? Service.Lang.GetText("TripleTriad-Draw") :
                           lose ? Service.Lang.GetText("TripleTriad-Loss") :
                           win ? Service.Lang.GetText("TripleTriad-Win") : "";
        }

        Service.Log.Debug("Triple Triad Match Ends, Currency Change Check Starts.");

        var items = inventoryHandler?.Items ?? [];
        Tracker.CheckCurrencies(
            items, "", $"({Service.Lang.GetText("TripleTriadWith", ttResultText, ttRivalName)})", RecordChangeType.All,
            14);

        ttRivalName = ttResultText = string.Empty;
        HandlerManager.Nullify(ref inventoryHandler);

        Service.Log.Debug("Currency Change Check Completes.");
    }

    public void Uninit()
    {
        Service.AddonLifecycle.UnregisterListener(StartTripleTriad);
        Service.AddonLifecycle.UnregisterListener(EndTripleTriad);
        HandlerManager.Nullify(ref inventoryHandler);

        isTTOn = false;
        ttRivalName = ttResultText = string.Empty;
    }
}
