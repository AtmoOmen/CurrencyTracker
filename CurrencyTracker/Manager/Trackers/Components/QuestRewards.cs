using CurrencyTracker.Manager.Libs;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Threading.Tasks;

namespace CurrencyTracker.Manager.Trackers
{
    public class QuestRewards : ITrackerComponent
    {
        private bool isReadyFinish = false;
        private string questName = string.Empty;

        public void Init()
        {
            if (Service.GameGui.GetAddonByName("JournalResult") != nint.Zero)
            {
                BeginQuestHandler();
            }

            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "JournalResult", BeginQuest);
        }

        private void BeginQuest(AddonEvent type, AddonArgs? args)
        {
            if (isReadyFinish) return;

            BeginQuestHandler();
        }

        private unsafe void BeginQuestHandler()
        {
            ResetQuestState();
            var JR = (AtkUnitBase*)Service.GameGui.GetAddonByName("JournalResult");

            if (JR != null && JR->RootNode != null && JR->RootNode->ChildNode != null && JR->UldManager.NodeList != null && !isReadyFinish)
            {
                questName = JR->GetTextNodeById(30)->NodeText.ToString();
                var buttonNode = JR->GetNodeById(37);

                if (questName.IsNullOrEmpty() || buttonNode == null) return;

                isReadyFinish = true;

                Service.Tracker.ChatHandler.isBlocked = true;
                Service.Framework.Update += OnFrameworkUpdate;

                Service.PluginLog.Debug($"Quest {questName} Ready to Finish!");
            }
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            if (!isReadyFinish)
            {
                Service.Framework.Update -= OnFrameworkUpdate;
                return;
            }

            if (Flags.OccupiedInEvent() || Flags.BetweenAreas()) return;

            EndQuestHandler();
        }

        private void EndQuestHandler()
        {
            if (Flags.OccupiedInEvent()) return;

            isReadyFinish = false;

            Service.PluginLog.Debug($"Quest {questName} Finished, Currency Change Check Starts.");

            Parallel.ForEach(Plugin.Instance.Configuration.AllCurrencies, currency =>
            {
                Service.Tracker.CheckCurrency(currency.Key, "", $"({Service.Lang.GetText("Quest", questName)})");
            });

            ResetQuestState();
            Service.Framework.Update -= OnFrameworkUpdate;
            Service.Tracker.ChatHandler.isBlocked = false;

            Service.PluginLog.Debug("Currency Change Check Completes.");
        }

        private void ResetQuestState()
        {
            isReadyFinish = false;
            questName = string.Empty;
        }

        public void Uninit()
        {
            Service.Framework.Update -= OnFrameworkUpdate;
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "JournalResult", BeginQuest);
        }
    }
}
