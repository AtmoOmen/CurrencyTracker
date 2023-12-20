namespace CurrencyTracker.Manager.Trackers.Components
{
    // 过时，需要重写 Outdate, Need Rewrite
    public class GoldSaucer : ITrackerComponent
    {
        public bool Initialized { get; set; } = false;

        public void Init()
        {
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreSetup, "GoldSaucerReward", GoldSaucerHandler);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "GoldSaucerReward", GoldSaucerHandler);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "GoldSaucerReward", GoldSaucerHandler);

            Initialized = true;
        }

        private void GoldSaucerHandler(AddonEvent type, AddonArgs args)
        {
            switch (type)
            {
                case AddonEvent.PreSetup:
                    HandlerManager.ChatHandler.isBlocked = true;
                    break;
                case AddonEvent.PostSetup:
                    BeginGoldSaucerHandler(args);
                    break;
                case AddonEvent.PreFinalize:
                    if (!Flags.OccupiedInEvent())
                    {
                        HandlerManager.ChatHandler.isBlocked = false;
                    }
                    break;
            }
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
                        Service.Tracker.CheckCurrency(29, "", $"({GameName})", RecordChangeType.All, 23, TransactionFileCategory.Inventory, 0);
                    }
                }
            }
        }

        public void Uninit()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreSetup, "GoldSaucerReward", GoldSaucerHandler);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "GoldSaucerReward", GoldSaucerHandler);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "GoldSaucerReward", GoldSaucerHandler);

            Initialized = false;
        }
    }
}
