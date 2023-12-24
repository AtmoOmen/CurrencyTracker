namespace CurrencyTracker.Manager.Trackers.Components
{
    public class Trade : ITrackerComponent
    {
        public bool Initialized { get; set; } = false;

        private string tradeTargetName = string.Empty;
        private InventoryHandler? inventoryHandler;

        public void Init()
        {
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "Trade", StartTrade);

            Initialized = true;
        }

        private unsafe void StartTrade(AddonEvent type, AddonArgs args)
        {
            var TGUI = (AtkUnitBase*)args.Addon;
            if (TGUI == null) return;

            var textNode = TGUI->GetTextNodeById(17);
            if (textNode == null) return;

            tradeTargetName = textNode->NodeText.ToString();
            inventoryHandler = new();
            Service.Framework.Update += OnFrameworkUpdate;
            HandlerManager.ChatHandler.isBlocked = true;

            Service.Log.Debug($"Trade Starts with {tradeTargetName}");
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            if (Service.Condition[ConditionFlag.TradeOpen]) return;

            Service.Framework.Update -= OnFrameworkUpdate;
            Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(t => EndTrade());
        }

        private void EndTrade()
        {
            if (Service.Condition[ConditionFlag.TradeOpen]) return;

            Service.Log.Debug("Trade Ends, Currency Change Check Starts.");

            var items = inventoryHandler?.Items ?? new();
            Service.Tracker.CheckCurrencies(items, "", $"({Service.Lang.GetText("TradeWith", tradeTargetName)})", RecordChangeType.All, 13);
            tradeTargetName = string.Empty;
            HandlerManager.ChatHandler.isBlocked = false;
            HandlerManager.Nullify(ref inventoryHandler);

            Service.Log.Debug("Currency Change Check Completes.");
        }

        public void Uninit()
        {
            Service.Framework.Update -= OnFrameworkUpdate;
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "Trade", StartTrade);
            HandlerManager.Nullify(ref inventoryHandler);
            tradeTargetName = string.Empty;

            Initialized = false;
        }
    }
}
