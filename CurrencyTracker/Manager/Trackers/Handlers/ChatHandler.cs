namespace CurrencyTracker.Manager.Trackers.Handlers
{
    public class ChatHandler : ITrackerHandler
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

        private static readonly ushort[] ValidChatTypes = new ushort[9]
        {
            0, 57, 62, 2110, 2105, 2238, 2622, 3001, 3006
        };

        private bool _isBlocked = false;
        private bool _initialized = false;

        public void Init()
        {
            Service.Chat.ChatMessage += OnChatMessage;

            _initialized = true;
        }

        private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (_isBlocked) return;
            if (!ValidChatTypes.Contains((ushort)type)) return;

            Service.Tracker.CheckAllCurrencies("", "", RecordChangeType.All, 17);
        }

        public void Uninit()
        {
            Service.Chat.ChatMessage -= OnChatMessage;

            _initialized = false;
        }
    }
}
