using CurrencyTracker.Manager.Libs;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CurrencyTracker.Manager.Trackers.Handlers
{
    public class ChatHandler : ITrackerHandler
    {
        public bool isBlocked
        {
            get { return _isBlocked; }
            set { _isBlocked = value; }
        }

        private static readonly ushort[] ValidChatTypes = new ushort[9]
        {
            0, 57, 62, 2110, 2105, 2238, 2622, 3001, 3006
        };

        private bool _isBlocked;

        public ChatHandler()
        {
            Init();
        }

        public void Init()
        {
            _isBlocked = false;

            Service.Chat.ChatMessage += OnChatMessage;
        }

        private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (_isBlocked) return;
            if (!ValidChatTypes.Contains((ushort)type)) return;

            Parallel.ForEach(Plugin.Instance.Configuration.AllCurrencies, currency =>
            {
                Service.Tracker.CheckCurrency(currency.Value);
            });
        }

        public void Uninit()
        {
            Service.Chat.ChatMessage -= OnChatMessage;
        }
    }
}
