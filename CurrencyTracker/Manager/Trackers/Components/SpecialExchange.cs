using CurrencyTracker.Manager.Libs;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CurrencyTracker.Manager.Trackers
{
    public class SpecialExchange : ITrackerComponent
    {
        // Addon Name - Window Node ID
        private static readonly Dictionary<string, uint> UI = new()
        {
            { "RetainerList", 28 },
            { "GrandCompanySupplyList", 27 },
            { "WeeklyBingoResult", 99 },
            { "ReconstructionBox", 31 }
        };

        private bool isOnExchang = false;
        private string windowName = string.Empty;

        public SpecialExchange() 
        {
            Init();
        }

        public void Init()
        {
            if (!isOnExchang)
            {
                var exchange = UI.Keys.FirstOrDefault(ex => Service.GameGui.GetAddonByName(ex) != nint.Zero);
                if (exchange != null)
                {
                    BeginExchangeHandler(Service.GameGui.GetAddonByName(exchange), exchange);
                }
            }

            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, UI.Keys, BeginExchange);
        }

        private void BeginExchange(AddonEvent type, AddonArgs args)
        {
            if (isOnExchang) return;

            BeginExchangeHandler(args);
        }

        private void BeginExchangeHandler(AddonArgs args)
        {
            Service.Tracker.ChatHandler.isBlocked = true;
            isOnExchang = true;
            windowName = Service.Tracker.GetWindowTitle(args, UI[args.AddonName]) ?? string.Empty;

            Service.Framework.Update += OnFrameworkUpdate;
            Service.PluginLog.Debug("Exchange Starts");
        }

        private void BeginExchangeHandler(nint addon, string addonName)
        {
            Service.Tracker.ChatHandler.isBlocked = true;
            isOnExchang = true;
            windowName = Service.Tracker.GetWindowTitle(addon, UI[addonName]) ?? string.Empty;

            Service.Framework.Update += OnFrameworkUpdate;
            Service.PluginLog.Debug("Exchange Starts");
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            if (!isOnExchang)
            {
                Service.Framework.Update -= OnFrameworkUpdate;
                return;
            }

            if (Flags.OccupiedInEvent()) return;

            EndExchangeHandler();
        }

        private void EndExchangeHandler()
        {
            isOnExchang = false;

            Parallel.ForEach(Plugin.Instance.Configuration.AllCurrencies, currency =>
            {
                Service.Tracker.CheckCurrency(currency.Key, "", $"({windowName})");
            });

            windowName = string.Empty;

            Service.Tracker.ChatHandler.isBlocked = false;
            Service.Framework.Update -= OnFrameworkUpdate;
            Service.PluginLog.Debug("Exchange Completes");

        }

        public void Uninit()
        {
            isOnExchang = false;
            windowName = string.Empty;

            Service.Framework.Update -= OnFrameworkUpdate;
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, UI.Keys, BeginExchange);
        }
    }
}
