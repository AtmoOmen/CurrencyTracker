using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Utility;
using System;
using System.Linq;

namespace CurrencyTracker.Manager.Trackers
{
    public partial class Tracker : IDisposable
    {
        private DateTime? lastUpdateTime;
        private bool isOnExchanging = false;
        private static readonly string[] ExchangeUI = new[] { "InclusionShop", "CollectablesShop", "FreeCompanyExchange", "FreeCompanyCreditShop", "ShopExchangeCurrency", "GrandCompanySupplyList", "GrandCompanyExchange", "Shop", "ItemSearch", "ShopExchangeItem", "SkyIslandExchange", "ShopExchangeItemDialog", "TripleTriadCoinExchange", "FreeCompanyChest", "RetainerList" };

        public void InitExchangeCompletes()
        {
            foreach (var exchange in ExchangeUI)
            {
                if (Service.GameGui.GetAddonByName(exchange) != nint.Zero)
                {
                    BeginExchange(AddonEvent.PostSetup, null);
                    break;
                }
            }
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, ExchangeUI , BeginExchange);
        }

        private void BeginExchange(AddonEvent type, AddonArgs? args)
        {
            if (!isOnExchanging)
            {
                isOnExchanging = true;
                DebindChatEvent();
                if (Service.TargetManager.Target != null)
                {
                    currentTargetName = Service.TargetManager.Target.Name.TextValue;
                }
                Service.PluginLog.Debug("Exchange Starts");
            }
        }

        private void IsOnExchange(DateTime lastUpdate)
        {
            if (lastUpdateTime == null)
            {
                lastUpdateTime = lastUpdate;
            }
            if ((lastUpdate - lastUpdateTime).Value.Seconds <= 0.5)
            {
                return;
            }
            else
            {
                lastUpdateTime = lastUpdate;
            }

            var exchangeState = Service.Condition[ConditionFlag.OccupiedSummoningBell] || Service.Condition[ConditionFlag.OccupiedInQuestEvent] || Service.Condition[ConditionFlag.OccupiedInEvent];

            if (!exchangeState && isOnExchanging)
            {
                isOnExchanging = false;

                if (!currentTargetName.IsNullOrEmpty())
                {
                    foreach (var currency in C.PresetCurrencies.Values.Concat(C.CustomCurrencies.Values))
                    {
                        CheckCurrency(currency, false, "-1", $"({Service.Lang.GetText("ExchangeWith", currentTargetName)})");
                    }
                }
                else
                {
                    Service.PluginLog.Warning("Failed to get exchange target.");
                    UpdateCurrencies();
                }

                currentTargetName = string.Empty;
                lastUpdateTime = null;

                Service.Chat.ChatMessage += OnChatMessage;

                Service.PluginLog.Debug("Exchange Completes");
            }
        }

        public void UninitExchangeCompletes()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, new[] { "InclusionShop", "CollectablesShop", "FreeCompanyExchange", "FreeCompanyCreditShop", "ShopExchangeCurrency", "GrandCompanySupplyList", "GrandCompanyExchange", "Shop", "ItemSearch", "ShopExchangeItem", "SkyIslandExchange", "ShopExchangeItemDialog", "TripleTriadCoinExchange", "FreeCompanyChest", "RetainerList" }, BeginExchange);
        }
    }
}
