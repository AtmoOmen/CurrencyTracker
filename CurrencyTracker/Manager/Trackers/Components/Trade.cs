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
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "Trade", StartTrade);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "Trade", EndTrade);
            _initialized = true;
        }

        private void StartTrade(AddonEvent type, AddonArgs args)
        {
            if (isOnTrade) return;

            StartTradeHandler(args);
        }

        private unsafe void StartTradeHandler(AddonArgs args)
        {
            var TGUI = (AtkUnitBase*)args.Addon;

            if (TGUI != null)
            {
                var textNode = TGUI->GetTextNodeById(17);
                if (textNode != null)
                {
                    tradeTargetName = textNode->NodeText.ToString();
                    HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = true;
                    isOnTrade = true;
                    Service.Log.Debug($"Trade Starts {tradeTargetName}");
                }
            }
        }

        private void EndTrade(AddonEvent eventType, AddonArgs addonInfo)
        {
            if (!isOnTrade) return;

            EndTradeHandler();
        }

        private void EndTradeHandler()
        {
            Service.Framework.Update += OnFrameworkUpdate;
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

            isOnTrade = false;
            Service.Tracker.CheckAllCurrencies("", $"({Service.Lang.GetText("TradeWith", tradeTargetName)})", RecordChangeType.All, 13);
            tradeTargetName = string.Empty;
            Service.Framework.Update -= OnFrameworkUpdate;
            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = false;
            Service.Log.Debug("Trade Ends");

            EndTradeHandler();
        }

        public void Uninit()
        {
            isOnTrade = false;
            tradeTargetName = string.Empty;

            Service.Framework.Update -= OnFrameworkUpdate;
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "Trade", StartTrade);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "Trade", EndTrade);

            _initialized = false;
        }
    }
}
