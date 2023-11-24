namespace CurrencyTracker.Manager.Trackers.Components
{
    public class FateRewards : ITrackerComponent
    {
        private bool _initialized = false;

        public bool Initialized
        {
            get { return _initialized; }
            set { _initialized = value; }
        }

        public void Init()
        {
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "FateReward", BeginFate);
            _initialized = true;
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
                    Parallel.ForEach(Plugin.Instance.Configuration.AllCurrencies, currency =>
                    {
                        Transactions.EditLatestTransaction(currency.Key, "None", $"({Service.Lang.GetText("Fate")} {FateName})");
                    });

                    FateName = string.Empty;
                }
            }
        }

        public void Uninit()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "FateReward", BeginFate);
            _initialized = false;
        }
    }
}
