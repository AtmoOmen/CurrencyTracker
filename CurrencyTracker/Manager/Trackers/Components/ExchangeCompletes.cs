using Dalamud.Game.ClientState.Conditions;
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
        }

        private void IsOnExchange()
        {
            // 罗薇娜商会
            var ICS = Service.GameGui.GetAddonByName("InclusionShop");
            // 收藏品交易
            var CS = Service.GameGui.GetAddonByName("CollectablesShop");
            // 部队战绩交换
            var FCE = Service.GameGui.GetAddonByName("FreeCompanyExchange");
            // 部队战绩交换 (道具)
            var FCCS = Service.GameGui.GetAddonByName("FreeCompanyCreditShop");
            // 货币交易
            var SEC = Service.GameGui.GetAddonByName("ShopExchangeCurrency");
            // 军队筹备
            var GCSL = Service.GameGui.GetAddonByName("GrandCompanySupplyList");
            // 军队补给
            var GCE = Service.GameGui.GetAddonByName("GrandCompanyExchange");
            // 军票提交确认
            var GCSR = Service.GameGui.GetAddonByName("GrandCompanySupplyReward");
            // 商店
            var SHOP = Service.GameGui.GetAddonByName("Shop");
            // 市场布告栏
            var IS = Service.GameGui.GetAddonByName("ItemSearch");
            // 以物易物
            var SEI = Service.GameGui.GetAddonByName("ShopExchangeItem");
            // 空岛商人
            var SIE = Service.GameGui.GetAddonByName("SkyIslandExchange");
            // 传唤铃/部队箱
            var SB = Service.Condition[ConditionFlag.OccupiedSummoningBell] ? 1 : nint.Zero;
            // 商店交换物品确认
            var SEID = Service.GameGui.GetAddonByName("ShopExchangeItemDialog");

            var exchangeUI = new nint[] { ICS, CS, FCE, FCCS, SEC, GCSL, GCE, GCSR, SHOP, IS, SEI, SIE, SB, SEID };

            var isAnyOpen = exchangeUI.Any(value => value != nint.Zero);

            if (isAnyOpen && !isOnExchanging)
            {
                isOnExchanging = true;
                Service.Chat.ChatMessage -= OnChatMessage;
                if (Service.TargetManager.Target != null)
                {
                    currentTargetName = Service.TargetManager.Target.Name.TextValue;
                }
                Service.PluginLog.Debug("Exchange Starts");
            }

            if (!isAnyOpen && isOnExchanging)
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
        }
    }
}
