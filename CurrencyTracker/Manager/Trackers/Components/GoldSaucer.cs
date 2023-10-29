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

        // 金碟相关是否已经记录过一次 If have recorded GS related transaction
        private bool hasRecordGSM = false;


        internal void InitGoldSacuer()
        {
            GameName = string.Empty;
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "GoldSaucerReward", GoldSaucerMain);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreSetup, "GoldSaucerReward", GSMPre);
        }

        private void GSMPre(AddonEvent type, AddonArgs args)
        {
            hasRecordGSM = false;
        }

        

        // 金碟内的大部分处理逻辑
        public void GoldSaucerMain(AddonEvent eventtype, AddonArgs args)
        {
            unsafe
            {
                var GSR = (AtkUnitBase*)Service.GameGui.GetAddonByName("GoldSaucerReward");
                if (GSR != null && GSR->RootNode != null && GSR->RootNode->ChildNode != null && GSR->UldManager.NodeList != null && !hasRecordGSM)
                {
                    var textNode = GSR->GetTextNodeById(5);
                    if (textNode != null)
                    {
                        GameName = textNode->NodeText.ToString();
                        if (!GameName.IsNullOrEmpty())
                        {
                            hasRecordGSM = true;
                            var currencyName = currencyInfo.CurrencyLocalName(29);
                            if (!C.CustomCurrencyType.Contains(currencyName))
                            {
                                return;
                            }
                            var filePath = Path.Combine(Plugin.Instance.PlayerDataFolder, $"{currencyName}.txt");
                            var editedTransactions = transactions.LoadAllTransactions(currencyName);

                            editedTransactions.LastOrDefault().Note = $"({GameName})";

                            Plugin.Instance.Main.transactionsConvertor.WriteTransactionsToFile(filePath, editedTransactions);
                            Plugin.Instance.Main.UpdateTransactions();

                            GameName = string.Empty;
                        }
                    }
                }
            }
        }

        internal void UninitGoldSacuer()
        {
            GameName = string.Empty;

            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostDraw, "GoldSaucerReward", GoldSaucerMain);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreSetup, "GoldSaucerReward", GSMPre);
        }
    }
}
