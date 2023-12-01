namespace CurrencyTracker.Manager.Trackers.Components
{
    public class Trade : ITrackerComponent
    {
        private bool _initialized = false;

        public bool Initialized
        {
            get { return _initialized; }
            set { _initialized = value; }
        }

        private bool isOnTrade = false;
        private string tradeTargetName = string.Empty;

        public void Init()
        {
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "Trade", StartTrade);
            _initialized = true;
        }

        private unsafe void StartTrade(AddonEvent type, AddonArgs args)
        {
            if (isOnTrade) return;

            var TGUI = (AtkUnitBase*)args.Addon;

            if (TGUI != null && TGUI->RootNode != null && TGUI->RootNode->ChildNode != null && TGUI->UldManager.NodeList != null)
            {
                var textNode = TGUI->GetTextNodeById(17);
                if (textNode != null)
                {
                    tradeTargetName = textNode->NodeText.ToString();
                    HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = true;
                    isOnTrade = true;
                    Service.Framework.Update += OnFrameworkUpdate;
                    Service.Log.Debug("Trade Starts");
                }
            }
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            if (!isOnTrade)
            {
                Service.Framework.Update -= OnFrameworkUpdate;
                HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = false;
                return;
            }

            if (Service.Condition[ConditionFlag.TradeOpen]) return;

            EndTradeHandler();
        }

        private void EndTradeHandler()
        {
            Service.Framework.Update -= OnFrameworkUpdate;
            isOnTrade = false;
            Service.Tracker.CheckAllCurrencies("", $"({Service.Lang.GetText("TradeWith", tradeTargetName)})", RecordChangeType.All, 13);
            tradeTargetName = string.Empty;
            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = false;
            Service.Log.Debug("Trade Ends");
        }

        public void Uninit()
        {
            isOnTrade = false;
            tradeTargetName = string.Empty;

            Service.Framework.Update -= OnFrameworkUpdate;
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "Trade", StartTrade);

            _initialized = false;
        }
    }
}
