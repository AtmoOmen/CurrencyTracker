namespace CurrencyTracker.Manager.Trackers.Components
{
    // 过时，需要重写 Outdate, Need Rewrite
    public class FateRewards : ITrackerComponent
    {
        public bool Initialized { get; set; } = false;

        public void Init()
        {
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreSetup, "FateReward", FateHandler);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "FateReward", FateHandler);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "FateReward", FateHandler);

            Initialized = true;
        }

        private void FateHandler(AddonEvent type, AddonArgs args)
        {
            switch (type)
            {
                case AddonEvent.PreSetup:
                    HandlerManager.ChatHandler.isBlocked = true;
                    break;
                case AddonEvent.PostSetup:
                    BeginFateHandler(args);
                    break;
                case AddonEvent.PreFinalize:
                    if (!Flags.OccupiedInEvent())
                    {
                        HandlerManager.ChatHandler.isBlocked = false;
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
                    Service.Tracker.CheckAllCurrencies("", $"({Service.Lang.GetText("Fate", FateName)})", RecordChangeType.All, 23, TransactionFileCategory.Inventory, 0);
                }
            }
        }

        public void Uninit()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreSetup, "FateReward", FateHandler);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "FateReward", FateHandler);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "FateReward", FateHandler);

            Initialized = false;
        }
    }
}
