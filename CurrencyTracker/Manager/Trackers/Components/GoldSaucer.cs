using CurrencyTracker.Manager.Libs;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CurrencyTracker.Manager.Trackers.Components
{
    public class GoldSaucer : ITrackerComponent
    {
        private bool _initialized = false;

        public bool Initialized
        {
            get { return _initialized; }
            set { _initialized = value; }
        }

        public void Init()
        {
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "GoldSaucerReward", BeginGoldSaucer);
            _initialized = true;
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
                        if (Plugin.Instance.Configuration.AllCurrencies.TryGetValue(29, out var _))
                        {
                            Transactions.EditLatestTransaction(29, "None", $"({GameName})");
                            Plugin.Instance.Main.UpdateTransactions();
                        }
                    }
                }
            }
        }

        public void Uninit()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "GoldSaucerReward", BeginGoldSaucer);
            _initialized = false;
        }
    }
}
