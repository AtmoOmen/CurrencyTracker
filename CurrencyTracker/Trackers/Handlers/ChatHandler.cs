using System.Collections.Generic;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager.Tracker;
using CurrencyTracker.Trackers;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using OmenTools.Helpers;

namespace CurrencyTracker.Manager.Trackers.Handlers;

public class ChatHandler : TrackerHandlerBase
{

    private static TaskHelper? TaskHelper;

    private static readonly HashSet<ushort> ValidChatTypes = [0, 57, 62, 2110, 2105, 2238, 2622, 3001, 3006];

    protected override void OnInit()
    {
        TaskHelper ??= new TaskHelper { TimeLimitMS = 5000 };

        DService.Chat.ChatMessage += OnChatMessage;
    }

    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (IsBlocked) return;
        if (!ValidChatTypes.Contains((ushort)type)) return;

        TaskHelper.Abort();
        TaskHelper.DelayNext(100);
        TaskHelper.Enqueue(UpdateAllCurrencies);
    }

    private static void UpdateAllCurrencies() => TrackerManager.CheckAllCurrencies(string.Empty, string.Empty, RecordChangeType.All, 17);

    protected override void OnUninit()
    {
        DService.Chat.ChatMessage -= OnChatMessage;
        TaskHelper?.Abort();
        TaskHelper = null;
    }
}
