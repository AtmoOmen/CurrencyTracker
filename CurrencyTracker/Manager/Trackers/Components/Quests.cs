using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;

namespace CurrencyTracker.Manager.Trackers
{
    public partial class Tracker : IDisposable
    {
        private bool isQuestReadyFinish;
        private string questName = string.Empty;

        internal void InitQuests()
        {
            ResetQuestState();

            if (Service.GameGui.GetAddonByName("JournalResult") != nint.Zero)
            {
                Quests(AddonEvent.PostSetup, null);
            }

            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "JournalResult", Quests);
        }

        private unsafe void Quests(AddonEvent type, AddonArgs? args)
        {
            ResetQuestState();
            var JR = (AtkUnitBase*)Service.GameGui.GetAddonByName("JournalResult");

            if (JR != null && JR->RootNode != null && JR->RootNode->ChildNode != null && JR->UldManager.NodeList != null && !isQuestReadyFinish)
            {
                questName = JR->GetTextNodeById(30)->NodeText.ToString();
                var buttonNode = JR->GetNodeById(37);

                if (questName.IsNullOrEmpty() || buttonNode == null) return;

                isQuestReadyFinish = true;
                DebindChatEvent();

                Service.PluginLog.Debug($"Quest {questName} Ready to Finish!");
            }
        }

        private void QuestEndCheck()
        {
            if (!isQuestReadyFinish || Flags.OccupiedInEvent()) return;

            isQuestReadyFinish = false;

            Service.PluginLog.Debug($"Quest {questName} Finished, Currency Change Check Starts.");

            foreach (var currency in C.AllCurrencies)
            {
                CheckCurrency(currency.Value, "", $"({Service.Lang.GetText("Quest", questName)})");
            }

            ResetQuestState();
            Service.Chat.ChatMessage += OnChatMessage;

            Service.PluginLog.Debug("Currency Change Check Completes.");
        }

        private void ResetQuestState()
        {
            isQuestReadyFinish = false;
            questName = string.Empty;
        }

        public void UninitQuests()
        {
            ResetQuestState();
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "JournalResult", Quests);
        }
    }
}
