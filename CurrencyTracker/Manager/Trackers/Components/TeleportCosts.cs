namespace CurrencyTracker.Manager.Trackers.Components
{
    public class TeleportCosts : ITrackerComponent
    {
        private bool _initialized = false;

        public bool Initialized
        {
            get { return _initialized; }
            set { _initialized = value; }
        }

        private int teleportCost = 0;

        public void Init()
        {
            teleportCost = 0;
            Service.ClientState.TerritoryChanged += TeleportBetweenAreas;

            _initialized = true;
        }

        public void TeleportBetweenAreas(ushort obj)
        {
            if (teleportCost == 0 || !Plugin.Instance.Configuration.ComponentProp["RecordTeleportDes"]) return;

            if (CurrentLocationName != PreviousLocationName)
            {
                Parallel.ForEach(Plugin.Instance.Configuration.AllCurrencies, currency =>
                {
                    Transactions.EditLatestTransaction(currency.Key, PreviousLocationName, $"({Service.Lang.GetText("TeleportTo", CurrentLocationName)})", false, 5, false);
                });

                Plugin.Instance.Main.UpdateTransactions();

                teleportCost = 0;
                Service.PluginLog.Debug($"Teleport from {PreviousLocationName} to {CurrentLocationName}");
            }
        }

        public void TeleportWithCost(int GilAmount)
        {
            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = true;

            teleportCost = GilAmount;

            Parallel.ForEach(Plugin.Instance.Configuration.AllCurrencies, currency =>
            {
                Service.Tracker.CheckCurrency(currency.Key, CurrentLocationName, Plugin.Instance.Configuration.ComponentProp["RecordTeleportDes"] ? $"({Service.Lang.GetText("TeleportWithinArea")})" : "", RecordChangeType.Negative, 19);
            });

            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = false;
        }

        public void Uninit()
        {
            teleportCost = 0;
            Service.ClientState.TerritoryChanged -= TeleportBetweenAreas;
            _initialized = false;
        }
    }
}
