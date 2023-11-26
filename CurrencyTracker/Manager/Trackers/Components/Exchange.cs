namespace CurrencyTracker.Manager.Trackers.Components
{
    public class Exchange : ITrackerComponent
    {
        private bool _initialized = false;

        public bool Initialized
        {
            get { return _initialized; }
            set { _initialized = value; }
        }

        private static readonly string[] UI = new string[]
        {
            "InclusionShop", "CollectablesShop", "FreeCompanyExchange", "FreeCompanyCreditShop", "ShopExchangeCurrency", "Shop", "ItemSearch", "ShopExchangeItem", "SkyIslandExchange", "TripleTriadCoinExchange", "FreeCompanyChest", "MJIDisposeShop", "GrandCompanyExchange", "ReconstructionBuyback"
        };

        // Addon Name - Window Node ID
        private static readonly Dictionary<string, uint> WindowUI = new()
        {
            { "Repair", 38 },
            { "PvpReward", 125 },
            { "Materialize", 16 },
            { "ColorantEquipment", 13 },
            { "MiragePrism", 28 },
        };

        private string currentTargetName = string.Empty;
        internal static bool isOnExchange = false;
        private string windowName = string.Empty;

        public void Init()
        {
            var allUI = new List<IEnumerable<string>> { UI, WindowUI.Keys };
            foreach (var ui in allUI)
            {
                var foundUI = ui.FirstOrDefault(exchange => Service.GameGui.GetAddonByName(exchange) != nint.Zero);
                if (foundUI == null) continue;

                EndExchangeHandler();
                if (ui == UI)
                {
                    BeginExchangeHandler();
                }
                else if (ui == WindowUI.Keys)
                {
                    Service.PluginLog.Debug(foundUI);
                    BeginExchangeWindowHandler(foundUI);
                    Service.PluginLog.Debug(foundUI);
                }
                break;
            }

            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, UI, BeginExchange);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, UI, EndExchange);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, WindowUI.Keys, BeginExchange);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, WindowUI.Keys, EndExchange);
            _initialized = true;
        }

        private void BeginExchange(AddonEvent type, AddonArgs? args)
        {
            if (isOnExchange || SpecialExchange.isOnExchange) return;

            if (WindowUI.ContainsKey(args.AddonName))
            {
                BeginExchangeWindowHandler(args);
            }
            else
            {
                BeginExchangeHandler();
            }
        }

        private void BeginExchangeHandler()
        {
            if (isOnExchange || SpecialExchange.isOnExchange) return;

            isOnExchange = true;
            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = true;
            currentTargetName = Service.TargetManager.Target?.Name.TextValue ?? string.Empty;
            Service.PluginLog.Debug("Exchange Starts");
        }

        private void BeginExchangeWindowHandler(AddonArgs args)
        {
            if (isOnExchange || SpecialExchange.isOnExchange) return;

            isOnExchange = true;
            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = true;

            if (args.AddonName == "PvpReward")
            {
                windowName = Service.Tracker.GetWindowTitle(args, WindowUI[args.AddonName], new uint[] { 4, 5 }) ?? string.Empty;
            }
            else
            {
                windowName = Service.Tracker.GetWindowTitle(args, WindowUI[args.AddonName]) ?? string.Empty;
            }
            Service.PluginLog.Debug("Exchange Starts");
        }

        private void BeginExchangeWindowHandler(string addonName)
        {
            if (isOnExchange || SpecialExchange.isOnExchange) return;

            if (!addonName.IsNullOrEmpty())
            {
                return;
            }

            var addon = Service.GameGui.GetAddonByName(addonName);
            isOnExchange = true;
            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = true;
            if (addonName == "PvpReward")
            {
                windowName = Service.Tracker.GetWindowTitle(addon, WindowUI[addonName], new uint[] { 4, 5 }) ?? string.Empty;
            }
            else
            {
                windowName = Service.Tracker.GetWindowTitle(addon, WindowUI[addonName]) ?? string.Empty;
            }
            Service.PluginLog.Debug("Exchange Starts");
        }

        private void EndExchange(AddonEvent type, AddonArgs args)
        {
            if (SpecialExchange.isOnExchange) return;

            if (WindowUI.ContainsKey(args.AddonName))
            {
                EndExchangeWindowHandler();
            }
            else
            {
                EndExchangeHandler();
            }
        }

        private void EndExchangeHandler()
        {
            isOnExchange = false;

            Parallel.ForEach(Plugin.Instance.Configuration.AllCurrencies, currency =>
            {
                Service.Tracker.CheckCurrency(currency.Key, "", $"({Service.Lang.GetText("ExchangeWith", currentTargetName)})", RecordChangeType.All, 2);
            });

            currentTargetName = string.Empty;

            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = false;
            Service.PluginLog.Debug("Exchange Completes");
        }

        private void EndExchangeWindowHandler()
        {
            isOnExchange = false;

            Parallel.ForEach(Plugin.Instance.Configuration.AllCurrencies, currency =>
            {
                Service.Tracker.CheckCurrency(currency.Key, "", $"({windowName})", RecordChangeType.All, 3);
            });

            windowName = string.Empty;

            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = false;
            Service.PluginLog.Debug("Exchange Completes");
        }

        public void Uninit()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, UI, BeginExchange);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, UI, EndExchange);

            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, WindowUI.Keys, BeginExchange);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, WindowUI.Keys, EndExchange);
            _initialized = false;
        }
    }
}
