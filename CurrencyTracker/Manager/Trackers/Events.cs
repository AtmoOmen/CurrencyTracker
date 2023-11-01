using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using System;
using System.Linq;

namespace CurrencyTracker.Manager.Trackers
{
    public partial class Tracker : IDisposable
    {
        // (人为触发)发现货币发生改变时触发的事件
        public virtual void OnTransactionsUpdate(EventArgs e)
        {
            OnCurrencyChanged?.Invoke(this, e);
        }

        // 收到新聊天信息时触发的事件
        private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            var chatmessage = message.TextValue;
            var typeValue = (ushort)type;

            if (DutyStarted)
            {
                DutyEndCheck(chatmessage);
                return;
            }

            if (isQuestReadyFinish)
            {
                QuestEndCheck(chatmessage);
                return;
            }

            if (Service.ClientState.TerritoryType == 144)
            {
            }

            if (TriggerChatTypes.Contains(typeValue))
            {
                UpdateCurrencies();

                var eventInfo = Service.ClientState.GetType().GetEvent("TerritoryChanged");
                if (eventInfo == null)
                {
                    Service.ClientState.TerritoryChanged += OnZoneChange;
                }
            }

            /*

            if (Plugin.Instance.PluginInterface.IsDev)
            {
                if (!IgnoreChatTypes.Contains(typeValue))
                {
                    Service.PluginLog.Debug($"[{typeValue}]{chatmessage}");
                }
            }
            */
        }

        // 区域发生改变时触发的事件
        private void OnZoneChange(ushort sender)
        {
            if (P.PlayerDataFolder.IsNullOrEmpty())
            {
                return;
            }

            Service.Chat.ChatMessage -= OnChatMessage;

            currentLocationName = TerritoryNames.TryGetValue(Service.ClientState.TerritoryType, out var currentLocation) ? currentLocation : Service.Lang.GetText("UnknownLocation");

            if (C.TrackedInDuty)
            {
                // 检查 PVP 对局是否结束 Check whether PVP ends
                if (PVPNames.ContainsKey(TerritoryNames.FirstOrDefault(kvp => kvp.Value == previousLocationName).Key) && DutyStarted)
                {
                    DutyEndCheck("PVPEnds");
                }

                // 强制结束 Force to end
                if (!IsBoundByDuty())
                {
                    DutyEndCheck("任务结束了");
                }
            }

            // 传送费用相关计算
            TeleportCheck();

            Service.Chat.ChatMessage += OnChatMessage;
        }

        // 开始副本攻略时触发的事件 (同时也包含PVP)
        private void isDutyStarted(object? sender, ushort e)
        {
            if (!C.TrackedInDuty) return;
            if (ContentNames.TryGetValue(Service.ClientState.TerritoryType, out _))
            {
                DutyStarted = true;
                dutyLocationName = TerritoryNames.TryGetValue(Service.ClientState.TerritoryType, out var currentLocation) ? currentLocation : Service.Lang.GetText("UnknownLocation");
                dutyContentName = ContentNames.TryGetValue(Service.ClientState.TerritoryType, out var currentContent) ? currentContent : Service.Lang.GetText("UnknownContent");

                Service.Chat.ChatMessage -= OnChatMessage;
                Service.PluginLog.Debug("Duty Starts");
            }
        }

        // 角色 Condition 改变时触发的事件
        private void OnConditionChanged(ConditionFlag flag, bool value)
        {
            /*
            if (flag == ConditionFlag.OccupiedInQuestEvent && isQuestReadyFinish)
            {
                if (!value)
                {
                    isQuestFinished = true;
                }
            }
            */
        }

        // 每一帧更新时触发的事件
        private void OnFrameworkUpdate(IFramework framework)
        {
            // 九宫幻卡 Triple Triad
            if (isTTOn)
            {
                TripleTriad();
            }

            // 等待交换完成 Wait for exchange to complete
            if (C.WaitExComplete)
            {
                if (isOnExchanging)
                {
                    IsOnExchange();
                }
            }
        }
    }
}
