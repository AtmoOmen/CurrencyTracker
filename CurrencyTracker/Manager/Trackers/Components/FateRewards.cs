using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.IO;
using System.Linq;

namespace CurrencyTracker.Manager.Trackers
{
    public partial class Tracker : IDisposable
    {
        public void InitFateRewards()
        {
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "FateReward", FateHandler);
        }

        private unsafe void FateHandler(AddonEvent type, AddonArgs args)
        {
            var FR = (AtkUnitBase*)args.Addon;
            if (FR != null && FR->RootNode != null && FR->RootNode->ChildNode != null && FR->UldManager.NodeList != null)
            {
                var textNode = FR->GetTextNodeById(6);
                if (textNode != null)
                {
                    var FateName = textNode->NodeText.ToString();
                    if (!FateName.IsNullOrEmpty())
                    {
                        foreach (var currency in C.PresetCurrencies.Keys.Concat(C.CustomCurrencies.Keys))
                        {
                            if (currency.IsNullOrEmpty()) return;
                            var filePath = Path.Combine(Plugin.Instance.PlayerDataFolder, $"{currency}.txt");
                            var editedTransactions = Transactions.LoadAllTransactions(currency);

                            if (editedTransactions.Count == 0 || editedTransactions == null)
                            {
                                continue;
                            }

                            if ((DateTime.Now - editedTransactions.LastOrDefault().TimeStamp).TotalSeconds > 10)
                            {
                                continue;
                            }
                            editedTransactions.LastOrDefault().Note = $"(Fate {FateName})";

                            TransactionsConvertor.WriteTransactionsToFile(filePath, editedTransactions);
                            Plugin.Instance.Main.UpdateTransactions();
                        }
                        FateName = string.Empty;
                    }
                }
            }
        }

        private void FateRewardsCheck(string FateName)
        {
        }

        public void UninitFateRewards()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "FateReward", FateHandler);
        }
    }
}
