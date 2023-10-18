using System;

namespace CurrencyTracker.Manager.Trackers
{
    public partial class Tracker : IDisposable
    {
        public int teleportCost = 0;

        public string previousLocationName = string.Empty;

        public string currentLocationName = string.Empty;

        public void InitTeleportCosts()
        {
            previousLocationName = currentLocationName = TerritoryNames.TryGetValue(Service.ClientState.TerritoryType, out var currentLocation) ? currentLocation : Service.Lang.GetText("UnknownLocation");
            teleportCost = 0;

            Service.ClientState.TerritoryChanged += OnZoneChange;
            OnZoneChange(0);
        }

        public void TeleportWithCost(int GilAmount)
        {
            Service.Chat.ChatMessage -= OnChatMessage;
            teleportCost = GilAmount;

            // 传送网使用券 Aetheryte Ticket
            if (GilAmount == -1)
            {
                var currencyName = currencyInfo.CurrencyLocalName(7569);
                if (!C.CustomCurrencyType.Contains(currencyName))
                {
                    return;
                }
                CheckCurrency(7569, true, previousLocationName, C.RecordTeleportDes ? $"({Service.Lang.GetText("TeleportWithinArea")})" : "-1", GilAmount);
            }
            // 金币 Gil
            else if (GilAmount > 0)
            {
                CheckCurrency(1, true, previousLocationName, C.RecordTeleportDes ? $"({Service.Lang.GetText("TeleportWithinArea")})" : "-1", -GilAmount);
            }

            Service.Chat.ChatMessage += OnChatMessage;
        }

        public void UninitTeleportCosts()
        {
            previousLocationName = string.Empty;
            currentLocationName = string.Empty;
            teleportCost = 0;

            Service.ClientState.TerritoryChanged -= OnZoneChange;
        }
    }
}
