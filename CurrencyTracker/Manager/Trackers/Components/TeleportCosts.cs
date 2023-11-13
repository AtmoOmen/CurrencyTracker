using Dalamud.Utility;
using System;
using System.Linq;

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
        }

        private void TeleportCheck()
        {
            if (currentLocationName != previousLocationName)
            {
                if (C.RecordTeleport)
                {
                    // 传送网使用券 Aetheryte Ticket
                    if (teleportCost == -1)
                    {
                        if (C.RecordTeleportDes)
                        {
                            var currencyName = C.CustomCurrencies.FirstOrDefault(x => x.Value == 7569).Key;
                            if (currencyName.IsNullOrEmpty()) return;
                            Transactions.EditLatestTransaction(currencyName, "None", $"({Service.Lang.GetText("TeleportTo", currentLocationName)})");
                            Plugin.Instance.Main.UpdateTransactions();
                        }
                    }
                    // 金币 Gil
                    else if (teleportCost > 0)
                    {
                        if (C.RecordTeleportDes)
                        {
                            var currencyName = C.PresetCurrencies.FirstOrDefault(x => x.Value == 1).Key;
                            if (currencyName.IsNullOrEmpty()) return;
                            Transactions.EditLatestTransaction(currencyName, "None", $"({Service.Lang.GetText("TeleportTo", currentLocationName)})");
                            Plugin.Instance.Main.UpdateTransactions();
                        }
                    }
                }
                teleportCost = 0;
                previousLocationName = currentLocationName;
            }
        }

        public void TeleportWithCost(int GilAmount)
        {
            DebindChatEvent();
            teleportCost = GilAmount;

            // 传送网使用券 Aetheryte Ticket
            if (GilAmount == -1)
            {
                CheckCurrency(7569, previousLocationName, C.RecordTeleportDes ? $"({Service.Lang.GetText("TeleportWithinArea")})" : "", RecordChangeType.Negative);
            }
            // 金币 Gil
            else if (GilAmount > 0)
            {
                CheckCurrency(1, previousLocationName, C.RecordTeleportDes ? $"({Service.Lang.GetText("TeleportWithinArea")})" : "", RecordChangeType.Negative);
            }

            Service.Chat.ChatMessage += OnChatMessage;
        }

        public void UninitTeleportCosts()
        {
            previousLocationName = string.Empty;
            currentLocationName = string.Empty;
            teleportCost = 0;
        }
    }
}
