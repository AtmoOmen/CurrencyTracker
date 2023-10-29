using System;
using System.IO;
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
                            var currencyName = currencyInfo.CurrencyLocalName(7569);
                            if (!C.CustomCurrencyType.Contains(currencyName))
                            {
                                return;
                            }
                            var filePath = Path.Combine(Plugin.Instance.PlayerDataFolder, $"{currencyName}.txt");
                            var editedTransactions = transactions.LoadAllTransactions(currencyName);

                            editedTransactions.LastOrDefault().Note = $"({Service.Lang.GetText("TeleportTo")} {currentLocationName})";

                            Plugin.Instance.Main.transactionsConvertor.WriteTransactionsToFile(filePath, editedTransactions);
                            Plugin.Instance.Main.UpdateTransactions();
                        }
                    }
                    // 无花费 No Costs
                    else if (teleportCost == 0)
                    {
                        if (C.RecordTeleportDes)
                        {
                            CheckCurrency(1, false, previousLocationName, $"({Service.Lang.GetText("TeleportTo")} {currentLocationName})");
                        }
                    }
                    // 金币 Gil
                    else
                    {
                        if (C.RecordTeleportDes)
                        {
                            var currencyName = currencyInfo.CurrencyLocalName(1);
                            var filePath = Path.Combine(Plugin.Instance.PlayerDataFolder, $"{currencyName}.txt");
                            var editedTransactions = transactions.LoadAllTransactions(currencyName);

                            editedTransactions.LastOrDefault().Note = $"({Service.Lang.GetText("TeleportTo")} {currentLocationName})";

                            Plugin.Instance.Main.transactionsConvertor.WriteTransactionsToFile(filePath, editedTransactions);
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
        }
    }
}
