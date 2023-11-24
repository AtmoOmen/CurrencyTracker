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
            if (teleportCost == 0) return;

            if (TerrioryHandler.CurrentLocationName != TerrioryHandler.PreviousLocationName)
            {
                // 传送网使用券 Aetheryte Ticket
                if (teleportCost == -1)
                {
                    if (Plugin.Instance.Configuration.AllCurrencies.TryGetValue(7569, out var _))
                    {
                        Transactions.EditLatestTransaction(7569, "None", $"({Service.Lang.GetText("TeleportTo", TerrioryHandler.CurrentLocationName)})");
                        Plugin.Instance.Main.UpdateTransactions();
                    }
                }
                // 金币 Gil
                else if (teleportCost > 0)
                {
                    if (Plugin.Instance.Configuration.AllCurrencies.TryGetValue(1, out var _))
                    {
                        Transactions.EditLatestTransaction(1, "None", $"({Service.Lang.GetText("TeleportTo", TerrioryHandler.CurrentLocationName)})");
                        Plugin.Instance.Main.UpdateTransactions();
                    }
                }

                teleportCost = 0;
            }
        }

        public void TeleportWithCost(int GilAmount)
        {
            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = true;

            teleportCost = GilAmount;

            // 传送网使用券 Aetheryte Ticket
            if (GilAmount == -1)
            {
                Service.Tracker.CheckCurrency(7569, TerrioryHandler.PreviousLocationName, $"({Service.Lang.GetText("TeleportWithinArea")})", RecordChangeType.Negative);
            }
            // 金币 Gil
            else if (GilAmount > 0)
            {
                Service.Tracker.CheckCurrency(1, TerrioryHandler.PreviousLocationName, $"({Service.Lang.GetText("TeleportWithinArea")})", RecordChangeType.Negative);
            }

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
