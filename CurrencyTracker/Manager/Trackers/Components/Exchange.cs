using CurrencyTracker.Manager.Libs;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CurrencyTracker.Manager.Trackers
{
    public class Exchange : ITrackerComponent
    {
        private static readonly string[] UI = new string[]
        {
            "InclusionShop", "CollectablesShop", "FreeCompanyExchange", "FreeCompanyCreditShop", "ShopExchangeCurrency", "Shop", "ItemSearch", "ShopExchangeItem", "SkyIslandExchange", "TripleTriadCoinExchange", "FreeCompanyChest", "MJIDisposeShop", "GrandCompanyExchange", "ReconstructionBuyback"
        };

        // Addon Name - Window Node ID
        private static readonly Dictionary<string, uint> WindowUI = new()
        {
            { "Repair", 38 },
            { "PvpReward", 125 }
        };

        private string currentTargetName = string.Empty;
        private bool isOnExchange = false;
        private string windowName = string.Empty;

        public Exchange()
        {
            Init();
        }

        public void Init()
        {
            var allUI = new List<IEnumerable<string>> { UI, WindowUI.Keys };
            foreach (var ui in allUI)
            {
                var foundUI = ui.FirstOrDefault(exchange => Service.GameGui.GetAddonByName(exchange) != nint.Zero);
                if (foundUI == null) continue;

                EndExchangeHandler();
                if (ui == UI)
                {
                    BeginExchangeHandler();
                }
                else
                {
                    BeginExchangeWindowHandler(Service.GameGui.GetAddonByName(foundUI), foundUI);
                }
                break;
            }

            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, UI, BeginExchange);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, UI, EndExchange);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, WindowUI.Keys, BeginExchange);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, WindowUI.Keys, EndExchange);
        }

        private void BeginExchange(AddonEvent type, AddonArgs? args)
        {
            if (isOnExchange)
            {
                Service.Tracker.ChatHandler.isBlocked = true;
                EndExchangeHandler();
            }

            if (WindowUI.ContainsKey(args.AddonName))
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
            isOnExchange = true;
            Service.Tracker.ChatHandler.isBlocked = true;
            currentTargetName = Service.TargetManager.Target?.Name.TextValue ?? string.Empty;
            Service.PluginLog.Debug("Exchange Starts");
        }

        private void BeginExchangeWindowHandler(AddonArgs args)
        {
            isOnExchange = true;
            Service.Tracker.ChatHandler.isBlocked = true;

            if (args.AddonName == "PvpReward")
            {
                windowName = Service.Tracker.GetWindowTitle(args, WindowUI[args.AddonName], new uint[] { 4, 5 }) ?? string.Empty;
            }
            else
            {
                windowName = Service.Tracker.GetWindowTitle(args, WindowUI[args.AddonName]) ?? string.Empty;
            }
            Service.PluginLog.Debug("Exchange Starts");
        }

        private void BeginExchangeWindowHandler(nint addon, string addonName)
        {
            isOnExchange = true;
            Service.Tracker.ChatHandler.isBlocked = true;
            if (addonName == "PvpReward")
            {
                windowName = Service.Tracker.GetWindowTitle(addon, WindowUI[addonName], new uint[] { 4, 5 }) ?? string.Empty;
            }
            else
            {
                windowName = Service.Tracker.GetWindowTitle(addon, WindowUI[addonName]) ?? string.Empty;
            }
            Service.PluginLog.Debug("Exchange Starts");
        }

        private void EndExchange(AddonEvent type, AddonArgs args)
        {
            if (WindowUI.ContainsKey(args.AddonName))
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
            if (!isOnExchange) return;

            isOnExchange = false;

            Parallel.ForEach(Plugin.Instance.Configuration.AllCurrencies, currency =>
            {
                Service.Tracker.CheckCurrency(currency.Key, "", $"({Service.Lang.GetText("ExchangeWith", currentTargetName)})");
            }); 

            currentTargetName = string.Empty;

            Service.Tracker.ChatHandler.isBlocked = false;
            Service.PluginLog.Debug("Exchange Completes");
        }

        private void EndExchangeWindowHandler()
        {
            if (!isOnExchange) return;

            isOnExchange = false;

            Parallel.ForEach(Plugin.Instance.Configuration.AllCurrencies, currency =>
            {
                Service.Tracker.CheckCurrency(currency.Key, "", $"({windowName})");
            });

            windowName = string.Empty;

            Service.Tracker.ChatHandler.isBlocked = false;
            Service.PluginLog.Debug("Exchange Completes");
        }

        public void Uninit()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, UI, BeginExchange);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, UI, EndExchange);

            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, WindowUI.Keys, BeginExchange);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, WindowUI.Keys, EndExchange);
        }
    }
}
