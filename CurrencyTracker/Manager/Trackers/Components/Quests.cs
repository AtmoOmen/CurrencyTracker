using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Linq;

namespace CurrencyTracker.Manager.Trackers
{
    public partial class Tracker : IDisposable
    {
        private bool isQuestReadyFinish;
        private bool isQuestFinished;
        private string questName = string.Empty;

        private static readonly string[] QuestEndStrings = [
        "クエスト", "をコンプリートした", "完成了任务","Vous avez rempli", "Auftrag", "abgeschlossen" ,"complete" ];

        internal void InitQuests()
        {
            ResetQuestState();

            if (Service.GameGui.GetAddonByName("JournalResult") != nint.Zero)
            {
                Quests(AddonEvent.PostSetup, null);
            }

            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "JournalResult", Quests);
        }

        private void QuestEndCheck(string message)
        {
            if (!isQuestReadyFinish || Flags.OccupiedInEvent())
            {
                return;
            }

            if (isQuestReadyFinish && QuestEndStrings.Any(message.Contains))
            {
                isQuestFinished = true;
                return;
            }

            if (isQuestFinished)
            {
                Service.PluginLog.Debug($"Quest {questName} Finished, Currency Change Check Starts.");
                foreach (var currency in C.AllCurrencies)
                {
                    CheckCurrency(currency.Value, "", $"({Service.Lang.GetText("Quest", questName)})");
                }

                ResetQuestState();
                Service.PluginLog.Debug("Currency Change Check Completes.");
            }
        }

        private void Quests(AddonEvent type, AddonArgs? args)
        {
            ResetQuestState();
            unsafe
            {
                var JR = (AtkUnitBase*)Service.GameGui.GetAddonByName("JournalResult");

                if (JR != null && JR->RootNode != null && JR->RootNode->ChildNode != null && JR->UldManager.NodeList != null && !isQuestReadyFinish)
                {
                    questName = JR->GetTextNodeById(30)->NodeText.ToString();

                    if (!string.IsNullOrEmpty(questName))
                    {
                        Service.PluginLog.Debug($"Quest {questName} Ready to Finish!");
                    }
                    isQuestReadyFinish = true;
                }
            }
        }

        public void UninitQuests()
        {
            ResetQuestState();
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "JournalResult", Quests);
        }

        private void ResetQuestState()
        {
            isQuestFinished = isQuestReadyFinish = false;
            questName = string.Empty;
        }
    }
}
