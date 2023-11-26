namespace CurrencyTracker.Manager.Trackers.Components
{
    public class SpecialExchange : ITrackerComponent
    {
        private bool _initialized = false;

        public bool Initialized
        {
            get { return _initialized; }
            set { _initialized = value; }
        }

        // Addon Name - Window Node ID
        private static readonly Dictionary<string, uint> UI = new()
        {
            { "RetainerList", 28 },
            { "GrandCompanySupplyList", 27 },
            { "WeeklyBingoResult", 99 },
            { "ReconstructionBox", 31 },
        };

        internal static bool isOnExchange = false;
        private string windowName = string.Empty;

        public void Init()
        {
            if (!isOnExchange || !Exchange.isOnExchange)
            {
                var exchange = UI.Keys.FirstOrDefault(ex => Service.GameGui.GetAddonByName(ex) != nint.Zero);
                if (exchange != null)
                {
                    BeginExchangeHandler(exchange);
                }
            }

            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, UI.Keys, BeginExchange);

            _initialized = true;
        }

        private void BeginExchange(AddonEvent type, AddonArgs args)
        {
            if (isOnExchange || Exchange.isOnExchange) return;

            BeginExchangeHandler(args);
        }

        private void BeginExchangeHandler(AddonArgs args)
        {
            if (isOnExchange || Exchange.isOnExchange) return;

            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = true;
            isOnExchange = true;
            windowName = Service.Tracker.GetWindowTitle(args, UI[args.AddonName]) ?? string.Empty;

            Service.Framework.Update += OnFrameworkUpdate;
            Service.PluginLog.Debug("Exchange Starts");
        }

        private void BeginExchangeHandler(string addonName)
        {
            if (isOnExchange || Exchange.isOnExchange) return;

            if (!addonName.IsNullOrEmpty()) return;

            var addon = Service.GameGui.GetAddonByName(addonName);
            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = true;
            isOnExchange = true;
            windowName = Service.Tracker.GetWindowTitle(addon, UI[addonName]) ?? string.Empty;

            Service.Framework.Update += OnFrameworkUpdate;
            Service.PluginLog.Debug("Exchange Starts");
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            if (!isOnExchange && !Exchange.isOnExchange)
            {
                Service.Framework.Update -= OnFrameworkUpdate;
                return;
            }

            if (Flags.OccupiedInEvent()) return;

            EndExchangeHandler();
        }

        private void EndExchangeHandler()
        {
            if (Exchange.isOnExchange) return;

            isOnExchange = false;

            Parallel.ForEach(Plugin.Instance.Configuration.AllCurrencies, currency =>
            {
                Service.Tracker.CheckCurrency(currency.Key, "", $"({windowName})", RecordChangeType.All, 2);
            });

            windowName = string.Empty;

            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = false;
            Service.Framework.Update -= OnFrameworkUpdate;
            Service.PluginLog.Debug("Exchange Completes");
        }

        public void Uninit()
        {
            isOnExchange = false;
            windowName = string.Empty;

            Service.Framework.Update -= OnFrameworkUpdate;
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, UI.Keys, BeginExchange);

            _initialized = false;
        }
    }
}
