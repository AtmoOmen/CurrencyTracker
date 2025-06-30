using System.Collections.Generic;
using CurrencyTracker.Infos;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using OmenTools.Helpers;

namespace CurrencyTracker.Manager.Trackers.Handlers;

public class ChatHandler : ITrackerHandler
{
    public bool Initialized { get; set; }
    public bool isBlocked   { get; set; } = false;

    private static TaskHelper? TaskHelper;

    private static readonly HashSet<ushort> ValidChatTypes = [0, 57, 62, 2110, 2105, 2238, 2622, 3001, 3006];

    public void Init()
    {
        TaskHelper ??= new TaskHelper { TimeLimitMS = 5000 };

        DService.Chat.ChatMessage += OnChatMessage;
    }

    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (isBlocked) return;
        if (!ValidChatTypes.Contains((ushort)type)) return;

        TaskHelper.Abort();
        TaskHelper.DelayNext(100);
        TaskHelper.Enqueue(UpdateAllCurrencies);
    }

    private static void UpdateAllCurrencies() => Tracker.CheckAllCurrencies(string.Empty, string.Empty, RecordChangeType.All, 17);

    public void Uninit()
    {
        DService.Chat.ChatMessage -= OnChatMessage;
        TaskHelper?.Abort();
        TaskHelper = null;
    }
}
