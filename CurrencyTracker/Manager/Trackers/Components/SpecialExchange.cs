using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CurrencyTracker.Manager.Trackers
{
    public partial class Tracker : IDisposable
    {
        private bool isOnSpecialExchanging = false;

        // Addon Name - Window Node ID
        private Dictionary<string, uint> SpecialExchangeUI = new()
        {
            { "RetainerList", 28 },
            { "GrandCompanySupplyList", 27 },
            { "WeeklyBingoResult", 99 }
        };

        public void InitSpecialExchange()
        {
            if (!isOnSpecialExchanging)
            {
                var exchange = SpecialExchangeUI.Keys.FirstOrDefault(ex => Service.GameGui.GetAddonByName(ex) != nint.Zero);
                if (exchange != null)
                {
                    EndSpecialExchangeHandler();
                    BeginSpecialExchangeHandler(Service.GameGui.GetAddonByName(exchange), exchange);
                }
            }

            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, SpecialExchangeUI.Keys, BeginSpecialExchange);
        }

        private void BeginSpecialExchange(AddonEvent type, AddonArgs args)
        {
            if (isOnSpecialExchanging)
            {
                return;
            }

            BeginSpecialExchangeHandler(args);
        }

        private void BeginSpecialExchangeHandler(AddonArgs args)
        {
            isOnSpecialExchanging = true;
            DebindChatEvent();
            exchangeWindowName = GetWindowTitle(args, SpecialExchangeUI[args.AddonName]) ?? string.Empty;
            Service.PluginLog.Debug("Exchange Starts");
        }

        private void BeginSpecialExchangeHandler(nint addon, string addonName)
        {
            isOnSpecialExchanging = true;
            DebindChatEvent();
            exchangeWindowName = GetWindowTitle(addon, SpecialExchangeUI[addonName]) ?? string.Empty;
            Service.PluginLog.Debug("Exchange Starts");
        }

        private void EndSpecialExchange()
        {
            if (Flags.OccupiedInEvent())
            {
                return;
            }

            EndSpecialExchangeHandler();
        }

        private void EndSpecialExchangeHandler()
        {
            isOnSpecialExchanging = false;

            foreach (var currency in C.AllCurrencies)
            {
                CheckCurrency(currency.Value, "", $"({exchangeWindowName})");
            }

            exchangeWindowName = string.Empty;

            Service.Chat.ChatMessage += OnChatMessage;
            Service.PluginLog.Debug("Exchange Completes");

        }

        public void UninitSpecialExchange()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, SpecialExchangeUI.Keys, BeginSpecialExchange);
        }
    }
}
