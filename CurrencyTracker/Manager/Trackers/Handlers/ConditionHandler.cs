namespace CurrencyTracker.Manager.Trackers.Handlers
{
    public class ConditionHandler : ITrackerHandler
    {
        public bool Initialized
        {
            get { return _initialized; }
            set { _initialized = value; }
        }

        public bool isBlocked
        {
            get { return _isBlocked; }
            set { _isBlocked = value; }
        }

        private bool _isBlocked = false;
        private bool _initialized = false;

        private static readonly IntPtr FateAddress = unchecked((IntPtr)0x7FF7E5066D89);
        private static readonly IntPtr CEAddress = unchecked((IntPtr)0x7FF7E5049931);

        public static bool InFate;
        public static bool InCE;

        public void Init()
        {
            Service.Framework.Update += OnFrameworkUpdate;
            _initialized = true;
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
        }

        public void Uninit()
        {
            Service.Framework.Update -= OnFrameworkUpdate;
            _initialized = false;
        }
    }
}
