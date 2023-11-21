using CurrencyTracker.Manager.Libs;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Linq;

namespace CurrencyTracker.Manager.Trackers
{
    public class GoldSaucer : ITrackerComponent
    {
        public GoldSaucer() 
        {
            Init();
        }

        public void Init()
        {
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "GoldSaucerReward", BeginGoldSaucer);
        }

        private void BeginGoldSaucer(AddonEvent type, AddonArgs args)
        {
            BeginGoldSaucerHandler(args);
        }

        private unsafe void BeginGoldSaucerHandler(AddonArgs args)
        {
            var GSR = (AtkUnitBase*)args.Addon;
            if (GSR != null && GSR->RootNode != null && GSR->RootNode->ChildNode != null && GSR->UldManager.NodeList != null)
            {
                var textNode = GSR->GetTextNodeById(5);
                if (textNode != null)
                {
                    var GameName = textNode->NodeText.ToString();
                    if (!GameName.IsNullOrEmpty())
                    {
                        if (Plugin.Instance.Configuration.AllCurrencies.TryGetValue(29, out var currencyName))
                        {
                            Transactions.EditLatestTransaction(currencyName, "None", $"({GameName})");
                            Plugin.Instance.Main.UpdateTransactions();
                        }
                    }
                }
            }
        }

        public void Uninit()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "GoldSaucerReward", BeginGoldSaucer);
        }
    }
}
