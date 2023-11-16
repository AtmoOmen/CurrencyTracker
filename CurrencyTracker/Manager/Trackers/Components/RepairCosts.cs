using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using System;

namespace CurrencyTracker.Manager.Trackers
{
    public partial class Tracker : IDisposable
    {
        public void InitRepairCosts()
        {
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "Repair", RepairStart);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "Repair", RepairEnd);
        }

        private void RepairStart(AddonEvent type, AddonArgs args)
        {
            windowTitle = GetWindowTitle(args, 38);
            DebindChatEvent();
        }

        private void RepairEnd(AddonEvent type, AddonArgs args)
        {
            if (Flags.OccupiedInEvent())
                return;

            foreach (var currency in C.AllCurrencies)
            {
                CheckCurrency(currency.Value, "", $"({windowTitle})");
            }

            Service.Chat.ChatMessage += OnChatMessage;
        }

        public void UninitRepairCosts()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "Repair", RepairStart);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "Repair", RepairEnd);
        }
    }
}
