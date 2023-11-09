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
        internal void InitGoldSacuer()
        {
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "GoldSaucerReward", GoldSaucerMain);
        }

        // 金碟内主要处理逻辑 Main handle logic in Gold Saucer
        private unsafe void GoldSaucerMain(AddonEvent eventtype, AddonArgs args)
        {
            var GSR = (AtkUnitBase*)args.Addon;
            if (GSR != null && GSR->RootNode != null && GSR->RootNode->ChildNode != null && GSR->UldManager.NodeList != null)
            {
                var textNode = GSR->GetTextNodeById(5);
                if (textNode != null)
                {
                    var GameName = textNode->NodeText.ToString();
                    if (!GameName.IsNullOrEmpty())
                    {
                        var currencyName = C.CustomCurrencies.FirstOrDefault(x => x.Value == 29).Key;
                        if (currencyName.IsNullOrEmpty()) return;
                        var filePath = Path.Combine(Plugin.Instance.PlayerDataFolder, $"{currencyName}.txt");
                        var editedTransactions = Transactions.LoadAllTransactions(currencyName);

                        if (editedTransactions.Count == 0 || editedTransactions == null)
                        {
                            return;
                        }

                        if ((DateTime.Now - editedTransactions.LastOrDefault().TimeStamp).TotalSeconds > 10)
                        {
                            return;
                        }
                        editedTransactions.LastOrDefault().Note = $"({GameName})";

                        TransactionsConvertor.WriteTransactionsToFile(filePath, editedTransactions);
                        Plugin.Instance.Main.UpdateTransactions();
                    }
                }
            }
        }

        internal void UninitGoldSacuer()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "GoldSaucerReward", GoldSaucerMain);
        }
    }
}
