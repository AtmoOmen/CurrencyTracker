using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using System;
using System.Linq;

namespace CurrencyTracker.Manager.Trackers
{
    public partial class Tracker : IDisposable
    {
        private bool isOnExchanging = false;

        public void InitExchangeCompletes()
        {
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreSetup, new[] { "InclusionShop", "CollectablesShop", "FreeCompanyExchange", "FreeCompanyCreditShop", "ShopExchangeCurrency", "GrandCompanySupplyList", "GrandCompanyExchange", "GrandCompanySupplyReward", "Shop", "ItemSearch", "ShopExchangeItem", "SkyIslandExchange", "ShopExchangeItemDialog", "TripleTriadCoinExchange", "FreeCompanyChest", "RetainerList" }, BeginExchange);
        }

        private void BeginExchange(AddonEvent type, AddonArgs args)
        {
            if (!isOnExchanging)
            {
                isOnExchanging = true;
                Service.Chat.ChatMessage -= OnChatMessage;
                if (Service.TargetManager.Target != null)
                {
                    currentTargetName = Service.TargetManager.Target.Name.TextValue;
                }
                Service.PluginLog.Debug("Exchange Starts");
            }
        }

        private void IsOnExchange()
        {
            var exchangeState = Service.Condition[ConditionFlag.OccupiedSummoningBell] || Service.Condition[ConditionFlag.OccupiedInQuestEvent] || Service.Condition[ConditionFlag.OccupiedInEvent];


            if (!exchangeState && isOnExchanging)
            {
                isOnExchanging = false;

                if (!currentTargetName.IsNullOrEmpty())
                {
                    foreach (var currency in CurrencyType)
                    {
                        if (CurrencyInfo.presetCurrencies.TryGetValue(currency, out var currencyID))
                        {
                            CheckCurrency(currencyID, false, "-1", $"({Service.Lang.GetText("ExchangeWith")} {currentTargetName})");
                        }
                    }
                    foreach (var currency in Plugin.Instance.Configuration.CustomCurrencyType)
                    {
                        if (Plugin.Instance.Configuration.CustomCurrencies.TryGetValue(currency, out var currencyID))
                        {
                            CheckCurrency(currencyID, false, "-1", $"({Service.Lang.GetText("ExchangeWith")} {currentTargetName})");
                        }
                    }
                }
                else
                {
                    Service.PluginLog.Warning("Failed to get exchange target.");
                    UpdateCurrenciesByChat();
                }

                currentTargetName = string.Empty;

                Service.Chat.ChatMessage += OnChatMessage;

                Service.PluginLog.Debug("Exchange Completes");
            }
        }

        public void UninitExchangeCompletes()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreSetup, new[] { "InclusionShop", "CollectablesShop", "FreeCompanyExchange", "FreeCompanyCreditShop", "ShopExchangeCurrency", "GrandCompanySupplyList", "GrandCompanyExchange", "GrandCompanySupplyReward", "Shop", "ItemSearch", "ShopExchangeItem", "SkyIslandExchange", "ShopExchangeItemDialog", "TripleTriadCoinExchange", "FreeCompanyChest", "RetainerList" }, BeginExchange);
        }
    }
}
