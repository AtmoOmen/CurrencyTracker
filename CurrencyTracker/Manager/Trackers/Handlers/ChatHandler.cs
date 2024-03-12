using System.Collections.Generic;
using CurrencyTracker.Manager.Infos;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using ECommons.Automation;

namespace CurrencyTracker.Manager.Trackers.Handlers;

public class ChatHandler : ITrackerHandler
{
    public bool Initialized { get; set; }
    public bool isBlocked { get; set; } = false;

    private static TaskManager? TaskManager;

    private static readonly HashSet<ushort> ValidChatTypes = new()
    {
        0, 57, 62, 2110, 2105, 2238, 2622, 3001, 3006
    };

    public void Init()
    {
        TaskManager ??= new TaskManager { AbortOnTimeout = true, TimeLimitMS = 5000, ShowDebug = false };

        Service.Chat.ChatMessage += OnChatMessage;
    }

    private void OnChatMessage(
        XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (isBlocked) return;
        if (!ValidChatTypes.Contains((ushort)type)) return;

        TaskManager.Abort();
        TaskManager.DelayNext(100);
        TaskManager.Enqueue(UpdateAllCurrencies);
    }

    private static void UpdateAllCurrencies() => Service.Tracker.CheckAllCurrencies("", "", RecordChangeType.All, 17);

    public void Uninit()
    {
        Service.Chat.ChatMessage -= OnChatMessage;
        TaskManager?.Abort();
    }
}
