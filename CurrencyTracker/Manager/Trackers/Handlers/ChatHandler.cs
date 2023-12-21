namespace CurrencyTracker.Manager.Trackers.Handlers
{
    public class ChatHandler : ITrackerHandler
    {
        public bool Initialized { get; set; } = false;
        public bool isBlocked { get; set; } = false;

        private static readonly HashSet<ushort> ValidChatTypes = new()
        {
            0, 57, 62, 2110, 2105, 2238, 2622, 3001, 3006
        };

        private readonly Timer checkTimer = new(500);


        public void Init()
        {
            Service.Chat.ChatMessage += OnChatMessage;

            checkTimer.AutoReset = false;
            checkTimer.Elapsed += CheckTimerElapsed;

            Initialized = true;
        }

        private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (isBlocked) return;
            if (!ValidChatTypes.Contains((ushort)type)) return;

            checkTimer.Restart();
        }

        private void CheckTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            Service.Tracker.CheckAllCurrencies("", "", RecordChangeType.All, 17);
        }

        public void Uninit()
        {
            Service.Chat.ChatMessage -= OnChatMessage;

            checkTimer.Elapsed -= CheckTimerElapsed;
            checkTimer.Stop();
            checkTimer.Dispose();

            Initialized = false;
        }
    }
}
