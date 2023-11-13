using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Linq;

namespace CurrencyTracker.Manager.Trackers
{
    public partial class Tracker : IDisposable
    {
        private bool isQuestReadyFinish = false;
        private bool isQuestFinished = false;
        private string QuestName = string.Empty;

        private static readonly string[] QuestEndStrings = new[] {
            "クエスト", "をコンプリートした", "完成了任务", "Vous avez rempli un objectif de la quête", "Auftrag", "abgeschlossen" ,"complete"
        };

        internal void InitQuests()
        {
            isQuestFinished = false;
            isQuestReadyFinish = false;
            QuestName = string.Empty;

            if (Service.GameGui.GetAddonByName("JournalResult") != nint.Zero)
            {
                Quests(AddonEvent.PostSetup, null);
            }

            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "JournalResult", Quests);
        }

        private void QuestEndCheck(string message)
        {
            if (!isQuestReadyFinish)
            {
                return;
            }

            // 任务预备完成 Quest Ready to Finish
            if (isQuestReadyFinish && QuestEndStrings.Any(message.Contains))
            {
                isQuestFinished = true;
                return;
            }

            // 任务完成 奖励出现 Quest Finished, Rewards Appear
            if (isQuestFinished && !QuestName.IsNullOrEmpty() && !Service.Condition[ConditionFlag.OccupiedInQuestEvent])
            {
                Service.PluginLog.Debug("Quest Finished, Currency Change Check Starts.");
                foreach (var currency in C.AllCurrencies)
                {
                    CheckCurrency(currency.Value, "", $"({Service.Lang.GetText("Quest", QuestName)})");
                }

                isQuestReadyFinish = false;
                isQuestFinished = false;
                QuestName = string.Empty;
                Service.PluginLog.Debug("Currency Change Check Completes.");
            }
        }

        private void Quests(AddonEvent type, AddonArgs? args)
        {
            isQuestFinished = isQuestReadyFinish = false;
            QuestName = string.Empty;

            unsafe
            {
                var JR = (AtkUnitBase*)Service.GameGui.GetAddonByName("JournalResult");

                if (JR != null && JR->RootNode != null && JR->RootNode->ChildNode != null && JR->UldManager.NodeList != null && !isQuestReadyFinish)
                {
                    QuestName = JR->GetTextNodeById(30)->NodeText.ToString();

                    if (!QuestName.IsNullOrEmpty())
                    {
                        Service.PluginLog.Debug($"Quest {QuestName} Ready to Finish!");
                    }
                    isQuestReadyFinish = true;
                }
            }
        }

        public void UninitQuests()
        {
            isQuestFinished = false;
            isQuestReadyFinish = false;
            QuestName = string.Empty;

            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "JournalResult", Quests);
        }
    }
}
