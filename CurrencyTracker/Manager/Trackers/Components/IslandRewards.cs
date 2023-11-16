using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using System;

namespace CurrencyTracker.Manager.Trackers
{
    public partial class Tracker : IDisposable
    {
        private bool isInIsland = false;
        private bool isOnWorkshop = false;
        private string windowTitle = string.Empty;

        public void InitIslandRewards()
        {
            isInIsland = true;

            // 囤货仓库 Gathering House
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "MJIGatheringHouse", MGHStart);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "MJIGatheringHouse", MGHEnd);
            // 无人岛制作 Island Crafting
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "MJIRecipeNoteBook", MRNBStart);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "MJIRecipeNoteBook", MRNBEnd);
            // 无人岛建造 Island Building
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "MJIBuilding", MBStart);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "MJIBuilding", MBEnd);
            // 无人岛耕地 Island Farm
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "MJIFarmManagement", MFMStart);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "MJIFarmManagement", MFMEnd);
            // 无人岛牧场 Island Pasture
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "MJIAnimalManagement", MAMStart);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "MJIAnimalManagement", MAMEnd);
        }

        // 无人岛牧场
        private void MAMStart(AddonEvent type, AddonArgs args)
        {
            DebindChatEvent();
        }

        private void MAMEnd(AddonEvent type, AddonArgs args)
        {
            if (Flags.OccupiedInEvent())
                return;

            foreach (var currency in C.AllCurrencies)
            {
                CheckCurrency(currency.Value, "", $"({Service.Lang.GetText("IslandPasture")})");
            }
        }

        // 无人岛耕地
        private void MFMStart(AddonEvent type, AddonArgs args)
        {
            DebindChatEvent();
        }

        private void MFMEnd(AddonEvent type, AddonArgs args)
        {
            if (Flags.OccupiedInEvent())
                return;

            foreach (var currency in C.AllCurrencies)
            {
                CheckCurrency(currency.Value, "", $"({Service.Lang.GetText("IslandFarm")})");
            }
        }

        // 无人岛建造 Island Building
        private unsafe void MBStart(AddonEvent type, AddonArgs args)
        {
            windowTitle = GetWindowTitle(args, 25);
            DebindChatEvent();
        }

        private void MBEnd(AddonEvent type, AddonArgs args)
        {
            if (Flags.OccupiedInEvent())
                return;

            foreach (var currency in C.AllCurrencies)
            {
                CheckCurrency(currency.Value, "", $"({windowTitle})");
            }

            Service.Chat.ChatMessage += OnChatMessage;
        }

        // 无人岛制作
        private unsafe void MRNBStart(AddonEvent type, AddonArgs args)
        {
            windowTitle = GetWindowTitle(args, 37);
            DebindChatEvent();
        }

        private void MRNBEnd(AddonEvent type, AddonArgs args)
        {
            if (Flags.OccupiedInEvent())
                return;

            foreach (var currency in C.AllCurrencies)
            {
                CheckCurrency(currency.Value, "", $"({windowTitle})");
            }

            Service.Chat.ChatMessage += OnChatMessage;
        }

        // 无人岛屯货仓库
        private unsafe void MGHStart(AddonEvent type, AddonArgs args)
        {
            windowTitle = GetWindowTitle(args, 73);
            DebindChatEvent();
        }

        private void MGHEnd(AddonEvent type, AddonArgs args)
        {
            if (Flags.OccupiedInEvent())
                return;

            foreach (var currency in C.AllCurrencies)
            {
                CheckCurrency(currency.Value, "", $"({windowTitle})");
            }

            Service.Chat.ChatMessage += OnChatMessage;
        }

        // 无人岛工房
        private void WorkshopHandler()
        {
            if (Service.TargetManager.Target != null && Service.TargetManager.Target.DataId == 1043078 && !isOnWorkshop)
            {
                isOnWorkshop = true;
                DebindChatEvent();
            }

            if (Service.TargetManager.PreviousTarget != null && Service.TargetManager.PreviousTarget.DataId == 1043078 && isOnWorkshop)
            {
                if (Service.TargetManager.Target != null && Service.TargetManager.Target.DataId == Service.TargetManager.PreviousTarget.DataId)
                {
                    return;
                }
                isOnWorkshop = false;

                foreach (var currency in C.AllCurrencies)
                {
                    CheckCurrency(currency.Value, "", $"({Service.Lang.GetText("IslandWorkshop")})");
                }

                Service.Chat.ChatMessage += OnChatMessage;
            }
        }

        public void IsInIslandCheck()
        {
            if (!C.RecordIsland)
                return;

            if (Service.ClientState.TerritoryType == 1055)
            {
                InitIslandRewards();
            }

            if (isInIsland && Service.ClientState.TerritoryType != 1055)
            {
                UninitIslandRewards();
            }
        }

        private void IslandHandlers()
        {
            WorkshopHandler();
        }

        public void UninitIslandRewards()
        {
            isInIsland = false;
            isOnWorkshop = false;
            windowTitle = string.Empty;

            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "MJIGatheringHouse", MGHStart);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "MJIGatheringHouse", MGHEnd);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "MJIRecipeNotebook", MRNBStart);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "MJIRecipeNotebook", MRNBEnd);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "MJIBuilding", MBStart);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "MJIBuilding", MBEnd);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "MJIFarmManagement", MFMStart);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "MJIFarmManagement", MFMEnd);
        }
    }
}
