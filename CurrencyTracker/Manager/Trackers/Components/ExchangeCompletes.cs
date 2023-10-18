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
            var GAN = Service.GameGui.GetAddonByName;
            // 部队战绩交换
            var FCE = GAN("FreeCompanyExchange");
            // 部队战绩交换 (道具)
            var FCCS = GAN("FreeCompanyCreditShop");
            // 货币交易
            var SEC = GAN("ShopExchangeCurrency");
            // 军队筹备
            var GCSL = GAN("GrandCompanySupplyList");
            // 军队补给
            var GCE = GAN("GrandCompanyExchange");
            // 商店
            var SHOP = GAN("Shop");
            // 市场布告栏
            var IS = GAN("ItemSearch");
            // 以物易物
            var SEI = GAN("ShopExchangeItem");
            // 空岛商人
            var SIE = GAN("SkyIslandExchange");
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
