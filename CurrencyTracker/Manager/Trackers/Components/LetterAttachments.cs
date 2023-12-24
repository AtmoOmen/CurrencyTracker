namespace CurrencyTracker.Manager.Trackers.Components
{
    public class LetterAttachments : ITrackerComponent
    {
        public bool Initialized { get; set; } = false;

        private string LetterSender = string.Empty;
        private InventoryHandler? inventoryHandler;

        public void Init()
        {
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "LetterViewer", OnLetterViewer);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "LetterViewer", OnLetterViewer);

            Initialized = true;
        }

        private unsafe void OnLetterViewer(AddonEvent type, AddonArgs args)
        {
            if (type == AddonEvent.PostSetup)
            {
                var UI = (AtkUnitBase*)args.Addon;
                if (!IsAddonNodesReady(UI)) return;

                var buttonNode = UI->GetButtonNodeById(30);
                if (buttonNode == null || !buttonNode->IsEnabled) return;

                var textNode = UI->GetTextNodeById(8);
                if (textNode == null) return;

                LetterSender = textNode->NodeText.ToString();
                inventoryHandler = new();
                HandlerManager.ChatHandler.isBlocked = true;
            }
            else if (type == AddonEvent.PreFinalize)
            {
                Task.Delay(TimeSpan.FromSeconds(1)).ContinueWith(t => EndLetterAttachments());
            }
        }

        private void EndLetterAttachments()
        {
            Service.Log.Debug("Letter Closed, Currency Change Check Starts.");

            var items = inventoryHandler?.Items ?? new();
            Service.Tracker.CheckCurrencies(items, "", $"({Service.Lang.GetText("LetterAttachments-LetterFrom", LetterSender)})", RecordChangeType.All, 24, TransactionFileCategory.Inventory, 0);

            HandlerManager.Nullify(ref inventoryHandler);
            HandlerManager.ChatHandler.isBlocked = false;
            LetterSender = string.Empty;

            Service.Log.Debug("Currency Change Check Completes.");
        }

        public void Uninit()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "LetterViewer", OnLetterViewer);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "LetterViewer", OnLetterViewer);
            HandlerManager.Nullify(ref inventoryHandler);

            Initialized = false;
        }
    }
}
