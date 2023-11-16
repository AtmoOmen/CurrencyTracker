using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using System;
using System.Linq;

namespace CurrencyTracker.Manager.Trackers
{
    public partial class Tracker : IDisposable
    {
        private string currentTargetName = string.Empty;
        private bool isOnExchanging = false;
        private static readonly string[] ExchangeUI = ["InclusionShop", "CollectablesShop", "FreeCompanyExchange", "FreeCompanyCreditShop", "ShopExchangeCurrency", "GrandCompanySupplyList", "GrandCompanyExchange", "Shop", "ItemSearch", "ShopExchangeItem", "SkyIslandExchange", "ShopExchangeItemDialog", "TripleTriadCoinExchange", "FreeCompanyChest", "RetainerList", "MJIDisposeShop"];

        public void InitExchangeCompletes()
        {
            if (!isOnExchanging && ExchangeUI.Any(exchange => Service.GameGui.GetAddonByName(exchange) != nint.Zero))
            {
                BeginExchange(AddonEvent.PostSetup, null);
            }
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, ExchangeUI, BeginExchange);
        }

        private void BeginExchange(AddonEvent type, AddonArgs? args)
        {
            if (isOnExchanging) return;

            isOnExchanging = true;
            DebindChatEvent();
            currentTargetName = Service.TargetManager.Target?.Name.TextValue ?? string.Empty;
            Service.PluginLog.Debug("Exchange Starts");
        }

        private void IsOnExchange()
        {
            if (!Flags.OccupiedInEvent() && isOnExchanging)
            {
                isOnExchanging = false;

                foreach (var currency in C.AllCurrencies)
                {
                    CheckCurrency(currency.Value, "", $"({Service.Lang.GetText("ExchangeWith", currentTargetName)})");
                }

                currentTargetName = string.Empty;

                Service.Chat.ChatMessage += OnChatMessage;
                Service.PluginLog.Debug("Exchange Completes");
            }
        }

        public void UninitExchangeCompletes()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, ExchangeUI, BeginExchange);
        }
    }
}
