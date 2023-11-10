using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using System;

namespace CurrencyTracker.Manager.Trackers
{
    public partial class Tracker : IDisposable
    {
        private string TradeTargetName = string.Empty;
        private bool isOnTrading = false;

        internal void InitTrade()
        {
            TradeTargetName = string.Empty;
            isOnTrading = false;

            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "Trade", StartTrade);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "Trade", TradeEndCheck);
        }

        private void StartTrade(AddonEvent type, AddonArgs args)
        {
            var TGUI = Service.GameGui.GetAddonByName("Trade");

            if (TGUI != nint.Zero && !isOnTrading)
            {
                isOnTrading = true;
                if (Service.TargetManager.Target != null)
                {
                    TradeTargetName = Service.TargetManager.Target.Name.TextValue;
                }
                DebindChatEvent();
                Service.PluginLog.Debug("Trade Starts");
            }
        }

        private void TradeEndCheck(AddonEvent type, AddonArgs args)
        {
            if (isOnTrading)
            {
                foreach (var currency in C.AllCurrencies)
                {
                    CheckCurrency(currency.Value, true, "-1", $"({Service.Lang.GetText("TradeWith", TradeTargetName)})");
                }

                currentTargetName = string.Empty;
                isOnTrading = false;

                Service.Chat.ChatMessage += OnChatMessage;
                Service.PluginLog.Debug("Trade Ends");
            }
        }

        internal void UninitTrade()
        {
            TradeTargetName = string.Empty;
            isOnTrading = false;

            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "Trade", StartTrade);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "Trade", TradeEndCheck);
        }
    }
}
