using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;

namespace CurrencyTracker.Manager.Trackers
{
    public partial class Tracker : IDisposable
    {
        public void InitFateRewards()
        {
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "FateReward", FateHandler);
        }

        private unsafe void FateHandler(AddonEvent type, AddonArgs args)
        {
            var FR = (AtkUnitBase*)args.Addon;
            if (FR != null && FR->RootNode != null && FR->RootNode->ChildNode != null && FR->UldManager.NodeList != null)
            {
                var textNode = FR->GetTextNodeById(6);
                if (textNode != null)
                {
                    var FateName = textNode->NodeText.ToString();
                    if (!FateName.IsNullOrEmpty())
                    {
                        foreach (var currency in C.AllCurrencies)
                        {
                            if (currency.Key.IsNullOrEmpty()) return;

                            Transactions.EditLatestTransaction(currency.Key, "None", $"({Service.Lang.GetText("Fate")} {FateName})");

                            Plugin.Instance.Main.UpdateTransactions();
                        }
                        FateName = string.Empty;
                    }
                }
            }
        }

        public void UninitFateRewards()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "FateReward", FateHandler);
        }
    }
}
