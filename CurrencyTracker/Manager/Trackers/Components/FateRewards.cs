using CurrencyTracker.Manager.Libs;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Threading.Tasks;

namespace CurrencyTracker.Manager.Trackers
{
    public class FateRewards : ITrackerComponent
    {
        public FateRewards()
        {
            Init();
        }

        public void Init()
        {
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "FateReward", BeginFate);
        }

        private void BeginFate(AddonEvent type, AddonArgs args)
        {
            BeginFateHandler(args);
        }

        private unsafe void BeginFateHandler(AddonArgs args)
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
                        Parallel.ForEach(Plugin.Instance.Configuration.AllCurrencies, currency =>
                        {
                            Transactions.EditLatestTransaction(currency.Value, "None", $"({Service.Lang.GetText("Fate")} {FateName})");
                        });

                        FateName = string.Empty;
                    }
                }
            }
        }

        public void Uninit()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "FateReward", BeginFate);
        }
    }
}
