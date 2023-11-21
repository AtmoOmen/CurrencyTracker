using CurrencyTracker.Manager.Libs;
using Dalamud.Utility;
using System.Linq;
using static CurrencyTracker.Manager.Trackers.Tracker;

namespace CurrencyTracker.Manager.Trackers
{
    public class TeleportCosts : ITrackerComponent
    {
        private int teleportCost = 0;

        public TeleportCosts() 
        {
            Init();
        }

        public void Init()
        {
            teleportCost = 0;
            Service.ClientState.TerritoryChanged += TeleportBetweenAreas;
        }

        public void TeleportBetweenAreas(ushort obj)
        {
            if (teleportCost == 0) return;

            if (TerrioryHandler.CurrentLocationName != TerrioryHandler.PreviousLocationName)
            {
                // 传送网使用券 Aetheryte Ticket
                if (teleportCost == -1)
                {
                    if (Plugin.Instance.Configuration.AllCurrencies.TryGetValue(7569, out var currencyName))
                    {
                        Transactions.EditLatestTransaction(currencyName, "None", $"({Service.Lang.GetText("TeleportTo", TerrioryHandler.CurrentLocationName)})");
                        Plugin.Instance.Main.UpdateTransactions();
                    }
                }
                // 金币 Gil
                else if (teleportCost > 0)
                {
                    if (Plugin.Instance.Configuration.AllCurrencies.TryGetValue(1, out var currencyName))
                    {
                        Transactions.EditLatestTransaction(currencyName, "None", $"({Service.Lang.GetText("TeleportTo", TerrioryHandler.CurrentLocationName)})");
                        Plugin.Instance.Main.UpdateTransactions();
                    }
                }

                teleportCost = 0;
            }
        }

        public void TeleportWithCost(int GilAmount)
        {
            Service.Tracker.ChatHandler.isBlocked = true;

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

            Service.Tracker.ChatHandler.isBlocked = false;
        }

        public void Uninit()
        {
            teleportCost = 0;
            Service.ClientState.TerritoryChanged -= TeleportBetweenAreas;

        }
    }
}
