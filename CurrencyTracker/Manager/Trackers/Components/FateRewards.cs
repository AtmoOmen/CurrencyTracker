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
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreSetup, "FateReward", FateHandler);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "FateReward", FateHandler);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "FateReward", FateHandler);
            _initialized = true;
        }

        private void FateHandler(AddonEvent type, AddonArgs args)
        {
            switch (type)
            {
                case AddonEvent.PreSetup:
                    HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = true;
                    break;
                case AddonEvent.PostSetup:
                    BeginFateHandler(args);
                    break;
                case AddonEvent.PreFinalize:
                    if (!Flags.OccupiedInEvent())
                    {
                        HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = false;
                    }
                    break;
            }
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
                    Service.Tracker.CheckAllCurrencies("", $"({Service.Lang.GetText("Fate")} {FateName})", RecordChangeType.All, 23, TransactionFileCategory.Inventory, 0);
                }
            }
        }

        public void Uninit()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreSetup, "FateReward", FateHandler);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "FateReward", FateHandler);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "FateReward", FateHandler);
            _initialized = false;
        }
    }
}
