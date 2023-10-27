using Dalamud.Game.ClientState.Conditions;
using Dalamud.Memory;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
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
                foreach (var currency in CurrencyType)
                {
                    if (CurrencyInfo.presetCurrencies.TryGetValue(currency, out var currencyID))
                    {
                        CheckCurrency(currencyID, true, "-1", $"({Service.Lang.GetText("Quest")} {QuestName})");
                    }
                }
                foreach (var currency in Plugin.Instance.Configuration.CustomCurrencyType)
                {
                    if (Plugin.Instance.Configuration.CustomCurrencies.TryGetValue(currency, out var currencyID))
                    {
                        CheckCurrency(currencyID, true, "-1", $"({Service.Lang.GetText("Quest")} {QuestName})");
                    }
                }

                isQuestReadyFinish = false;
                isQuestFinished = false;
                QuestName = string.Empty;
                Service.PluginLog.Debug("Currency Change Check Completes.");
            }
        }

        private void Quests()
        {
            var JR = Service.GameGui.GetAddonByName("JournalResult");

            if (JR != nint.Zero && !isQuestReadyFinish)
            {
                unsafe
                {
                    var addon = Dalamud.SafeMemory.PtrToStructure<AddonJournalResult>(JR);
                    if (addon != null)
                    {
                        QuestName = addon.Value.AtkTextNode250->NodeText.ToString();

                        if (!QuestName.IsNullOrEmpty())
                        {
                            Service.PluginLog.Debug($"Quest {QuestName} Ready to Finish!");
                        }
                    }
                    isQuestReadyFinish = true;
                }
            }
        }
    }
}
