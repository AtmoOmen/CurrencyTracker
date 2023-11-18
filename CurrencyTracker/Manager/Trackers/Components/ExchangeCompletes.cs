using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CurrencyTracker.Manager.Trackers
{
    public partial class Tracker : IDisposable
    {
        private string currentTargetName = string.Empty;
        private bool isOnExchanging = false;
        private string exchangeWindowName = string.Empty;


        private static readonly string[] ExchangeUI = new string[]
        {
            "InclusionShop", "CollectablesShop", "FreeCompanyExchange", "FreeCompanyCreditShop", "ShopExchangeCurrency", "Shop", "ItemSearch", "ShopExchangeItem", "SkyIslandExchange", "TripleTriadCoinExchange", "FreeCompanyChest", "MJIDisposeShop", "GrandCompanyExchange", "ReconstructionBuyback"
        };

        // Addon Name - Window Node ID
        private static readonly Dictionary<string, uint> ExchangeWindowUI = new()
        {
            { "Repair", 38 },
            { "PvpReward", 125 }
        };

        public void InitExchangeCompletes()
        {
            var allExchangeUI = new List<IEnumerable<string>> { ExchangeUI, ExchangeWindowUI.Keys };
            foreach (var exchangeUI in allExchangeUI)
            {
                var foundExchange = exchangeUI.FirstOrDefault(exchange => Service.GameGui.GetAddonByName(exchange) != nint.Zero);
                if (foundExchange == null) continue;

                EndExchangeHandler();
                if (exchangeUI == ExchangeUI)
                {
                    BeginExchangeHandler();
                }
                else
                {
                    BeginExchangeWindowHandler(Service.GameGui.GetAddonByName(foundExchange), foundExchange);
                }
                break;
            }

            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, ExchangeUI, BeginExchange);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, ExchangeUI, EndExchange);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, ExchangeWindowUI.Keys, BeginExchange);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, ExchangeWindowUI.Keys, EndExchange);
        }


        private void BeginExchange(AddonEvent type, AddonArgs? args)
        {
            if (isOnExchanging)
            {
                DebindChatEvent();
                EndExchangeHandler();
            }

            if (ExchangeWindowUI.ContainsKey(args.AddonName))
            {
                BeginExchangeWindowHandler(args);
            }
            else
            {
                BeginExchangeHandler();
            }
        }

        private void BeginExchangeHandler()
        {
            isOnExchanging = true;
            DebindChatEvent();
            currentTargetName = Service.TargetManager.Target?.Name.TextValue ?? string.Empty;
            Service.PluginLog.Debug("Exchange Starts");
        }

        private void BeginExchangeWindowHandler(AddonArgs args)
        {
            isOnExchanging = true;
            DebindChatEvent();
            if (args.AddonName == "PvpReward")
            {
                exchangeWindowName = GetWindowTitle(args, ExchangeWindowUI[args.AddonName], new uint[] { 4, 5 }) ?? string.Empty;
            }
            else
            {
                exchangeWindowName = GetWindowTitle(args, ExchangeWindowUI[args.AddonName]) ?? string.Empty;
            }
            Service.PluginLog.Debug("Exchange Starts");
        }

        private void BeginExchangeWindowHandler(nint addon, string addonName)
        {
            isOnExchanging = true;
            DebindChatEvent();
            if (addonName == "PvpReward")
            {
                exchangeWindowName = GetWindowTitle(addon, ExchangeWindowUI[addonName], new uint[] { 4, 5 }) ?? string.Empty;
            }
            else
            {
                exchangeWindowName = GetWindowTitle(addon, ExchangeWindowUI[addonName]) ?? string.Empty;
            }
            Service.PluginLog.Debug("Exchange Starts");
        }

        private void EndExchange(AddonEvent type, AddonArgs args)
        {
            if (ExchangeWindowUI.ContainsKey(args.AddonName))
            {
                EndExchangeWindowHandler();
            }
            else
            {
                EndExchangeHandler();
            }
        }

        private void EndExchangeHandler()
        {
            if (!isOnExchanging) return;

            isOnExchanging = false;

            foreach (var currency in C.AllCurrencies)
            {
                CheckCurrency(currency.Value, "", $"({Service.Lang.GetText("ExchangeWith", currentTargetName)})");
            }

            currentTargetName = string.Empty;

            Service.Chat.ChatMessage += OnChatMessage;
            Service.PluginLog.Debug("Exchange Completes");
        }

        private void EndExchangeWindowHandler()
        {
            if (!isOnExchanging) return;

            isOnExchanging = false;

            foreach (var currency in C.AllCurrencies)
            {
                CheckCurrency(currency.Value, "", $"({exchangeWindowName})");
            }

            exchangeWindowName = string.Empty;

            Service.Chat.ChatMessage += OnChatMessage;
            Service.PluginLog.Debug("Exchange Completes");
        }

        public void UninitExchangeCompletes()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, ExchangeUI, BeginExchange);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, ExchangeUI, EndExchange);

            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, ExchangeWindowUI.Keys, BeginExchange);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, ExchangeWindowUI.Keys, EndExchange);
        }
    }
}
