namespace CurrencyTracker.Manager.Trackers.Handlers
{
    public class ConditionHandler : ITrackerHandler
    {
        public bool Initialized { get; set; } = false;
        public bool isBlocked { get; set; } = false;

        public static bool InFate { get; private set; }

        public void Init()
        {
            Service.Framework.Update += OnFrameworkUpdate;
        }

        private unsafe void OnFrameworkUpdate(IFramework framework)
        {
            InFate = FateManager.Instance()->CurrentFate != null;
        }

        public void Uninit()
        {
            Service.Framework.Update -= OnFrameworkUpdate;
        }
    }
}
