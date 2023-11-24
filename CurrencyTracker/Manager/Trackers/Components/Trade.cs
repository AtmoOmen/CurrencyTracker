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

        private void StartTrade(AddonEvent type, AddonArgs args)
        {
            if (isOnTrade) return;

            var TGUI = Service.GameGui.GetAddonByName("Trade");

            if (TGUI != nint.Zero)
            {
                isOnTrade = true;
                if (Service.TargetManager.Target != null)
                {
                    tradeTargetName = Service.TargetManager.Target.Name.TextValue;
                }

                HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = true;
                Service.Framework.Update += OnFrameworkUpdate;
                Service.PluginLog.Debug("Trade Starts");
            }
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            if (!isOnTrade)
            {
                Service.Framework.Update -= OnFrameworkUpdate;
                return;
            }

            if (Flags.OccupiedInEvent()) return;

            EndTradeHandler();
        }

        private void EndTradeHandler()
        {
            isOnTrade = false;

            Parallel.ForEach(Plugin.Instance.Configuration.AllCurrencies, currency =>
            {
                Service.Tracker.CheckCurrency(currency.Key, "", $"({Service.Lang.GetText("TradeWith", tradeTargetName)})");
            });

            tradeTargetName = string.Empty;

            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = false;
            Service.Framework.Update -= OnFrameworkUpdate;
            Service.PluginLog.Debug("Trade Ends");
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
