using OmenTools.Helpers;

namespace CurrencyTracker.Manager.Trackers.Components;

public class LetterAttachments : ITrackerComponent
{
    public bool Initialized { get; set; }

    private string LetterSender = string.Empty;
    private InventoryHandler? inventoryHandler;

    public void Init()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "LetterViewer", OnLetterViewer);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "LetterViewer", OnLetterViewer);
    }

    private unsafe void OnLetterViewer(AddonEvent type, AddonArgs args)
    {
        switch (type)
        {
            case AddonEvent.PostSetup:
            {
                var UI = (AtkUnitBase*)args.Addon;
                if (!HelpersOm.IsAddonAndNodesReady(UI)) return;

                var buttonNode = UI->GetButtonNodeById(30);
                if (buttonNode == null || !buttonNode->IsEnabled) return;

                var textNode = UI->GetTextNodeById(8);
                if (textNode == null) return;

                LetterSender = textNode->NodeText.ToString();
                inventoryHandler = new InventoryHandler();
                HandlerManager.ChatHandler.isBlocked = true;
                break;
            }
            case AddonEvent.PreFinalize:
                Task.Delay(TimeSpan.FromSeconds(1)).ContinueWith(_ => EndLetterAttachments());
                break;
        }
    }

    private void EndLetterAttachments()
    {
        Service.Log.Debug("Letter Closed, Currency Change Check Starts.");

        var items = inventoryHandler?.Items ?? new HashSet<uint>();
        Service.Tracker.CheckCurrencies(
            items, "", $"({Service.Lang.GetText("LetterAttachments-LetterFrom", LetterSender)})", RecordChangeType.All,
            24);

        HandlerManager.Nullify(ref inventoryHandler);
        HandlerManager.ChatHandler.isBlocked = false;
        LetterSender = string.Empty;

        Service.Log.Debug("Currency Change Check Completes.");
    }

    public void Uninit()
    {
        Service.AddonLifecycle.UnregisterListener(OnLetterViewer);
        HandlerManager.Nullify(ref inventoryHandler);
    }
}
