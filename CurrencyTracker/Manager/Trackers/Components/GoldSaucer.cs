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
        // 当前正在玩的游戏 Currently Playing Minigame
        private string GameName = string.Empty;

        internal void InitGoldSacuer()
        {
            GameName = string.Empty;
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "GoldSaucerReward", GoldSaucerMain);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "MountainClimbingResult", MountainClimbing);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RideShootingResult", RideShooting);
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
                    GameName = textNode->NodeText.ToString();
                    if (!GameName.IsNullOrEmpty())
                    {
                        var currencyName = C.CustomCurrencies.FirstOrDefault(x => x.Value == 29).Key;
                        if (currencyName.IsNullOrEmpty()) return;
                        var filePath = Path.Combine(Plugin.Instance.PlayerDataFolder, $"{currencyName}.txt");
                        var editedTransactions = Transactions.LoadAllTransactions(currencyName);

                        if ((DateTime.Now - editedTransactions.LastOrDefault().TimeStamp).TotalSeconds > 10)
                        {
                            return;
                        }
                        editedTransactions.LastOrDefault().Note = $"({GameName})";

                        TransactionsConvertor.WriteTransactionsToFile(filePath, editedTransactions);
                        Plugin.Instance.Main.UpdateTransactions();

                        GameName = string.Empty;
                    }
                }
            }
        }

        private unsafe void MountainClimbing(AddonEvent eventtype, AddonArgs args)
        {
            var MCR = (AtkUnitBase*)args.Addon;
            if (MCR != null && MCR->RootNode != null && MCR->RootNode->ChildNode != null && MCR->UldManager.NodeList != null)
            {
                var textNode = MCR->GetTextNodeById(3);
                if (textNode != null)
                {
                    GameName = textNode->NodeText.ToString();
                    if (!GameName.IsNullOrEmpty())
                    {
                        var currencyName = C.CustomCurrencies.FirstOrDefault(x => x.Value == 29).Key;
                        if (currencyName.IsNullOrEmpty()) return;
                        var filePath = Path.Combine(Plugin.Instance.PlayerDataFolder, $"{currencyName}.txt");
                        var editedTransactions = Transactions.LoadAllTransactions(currencyName);

                        if ((DateTime.Now - editedTransactions.LastOrDefault().TimeStamp).TotalSeconds > 15)
                        {
                            return;
                        }
                        editedTransactions.LastOrDefault().Note = $"({GameName})";

                        TransactionsConvertor.WriteTransactionsToFile(filePath, editedTransactions);
                        Plugin.Instance.Main.UpdateTransactions();

                        GameName = string.Empty;
                    }
                }
            }
        }

        private unsafe void RideShooting(AddonEvent type, AddonArgs args)
        {
            var RSR = (AtkUnitBase*)args.Addon;
            if (RSR != null && RSR->RootNode != null && RSR->RootNode->ChildNode != null && RSR->UldManager.NodeList != null)
            {
                var textNode = RSR->GetTextNodeById(20);
                if (textNode != null)
                {
                    GameName = textNode->NodeText.ToString();
                    if (!GameName.IsNullOrEmpty())
                    {
                        var currencyName = C.CustomCurrencies.FirstOrDefault(x => x.Value == 29).Key;
                        if (currencyName.IsNullOrEmpty()) return;
                        var filePath = Path.Combine(Plugin.Instance.PlayerDataFolder, $"{currencyName}.txt");
                        var editedTransactions = Transactions.LoadAllTransactions(currencyName);

                        if ((DateTime.Now - editedTransactions.LastOrDefault().TimeStamp).TotalSeconds > 15)
                        {
                            return;
                        }
                        editedTransactions.LastOrDefault().Note = $"({GameName})";

                        TransactionsConvertor.WriteTransactionsToFile(filePath, editedTransactions);
                        Plugin.Instance.Main.UpdateTransactions();

                        GameName = string.Empty;
                    }
                }
            }
        }

        internal void UninitGoldSacuer()
        {
            GameName = string.Empty;

            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "GoldSaucerReward", GoldSaucerMain);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "MountainClimbingResult", MountainClimbing);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RideShootingResult", RideShooting);
        }
    }
}
