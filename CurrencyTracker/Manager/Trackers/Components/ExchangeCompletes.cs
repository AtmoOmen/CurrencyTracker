using Dalamud.Game.ClientState.Conditions;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
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

            var nintVariables = new nint[] { FCE, FCCS, SEC, GCSL, GCE, SHOP, IS, SEI, SIE, SB };

            var hasNonZeroValue = nintVariables.Any(value => value != nint.Zero);

            if (hasNonZeroValue && !isOnExchanging)
            {
                isOnExchanging = true;
                Service.Chat.ChatMessage -= OnChatMessage;
                Service.PluginLog.Debug("正在进行交换");
            }
            if (!hasNonZeroValue && isOnExchanging)
            {
                isOnExchanging = false;
                Service.Chat.ChatMessage += OnChatMessage;
                UpdateCurrenciesByChat();
                Service.PluginLog.Debug("交换完成");
            }


        }

        public void UninitExchangeCompletes()
        {
        }
    }
}
