using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CurrencyTracker.Manager.Trackers
{
    public partial class Tracker : IDisposable
    {
        public static bool BetweenAreas()
        {
            return Service.Condition[ConditionFlag.BetweenAreas] || Service.Condition[ConditionFlag.BetweenAreas51];
        }

        private bool WarpTPCheckState()
        {
            return warpTPBetweenAreas || warpTPInAreas;
        }

        private string[] ValidWarpText = new string[4]
        {
            "Gils", "金币", "ギル", "Gil"
        };

        private bool isReadyWarpTP = false;
        private bool warpTPBetweenAreas = false;
        private bool warpTPInAreas = false;

        private List<uint> GilCostsWarpTerriories = new();

        public void InitWarpCosts()
        {
            GilCostsWarpTerriories = Service.DataManager.GetExcelSheet<Warp>()
                .Where(x => Service.DataManager.GetExcelSheet<WarpCondition>()
                .Where(y => y.Gil != 0)
                .Any(y => x.WarpCondition.Value.RowId == y.RowId))
                .Select(x => x.TerritoryType.Value.RowId)
                .ToList();

            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectYesno", WarpConfirmationCheck);
        }

        private unsafe void WarpConfirmationCheck(AddonEvent type, AddonArgs args)
        {
            if (!GilCostsWarpTerriories.Any(x => Service.ClientState.TerritoryType == x))
            {
                return;
            }

            var SYN = args.Addon;

            if (SYN != nint.Zero)
            {
                var text = ((AddonSelectYesno*)SYN)->PromptText->NodeText.ToString();
                var buttonNode = ((AtkUnitBase*)SYN)->GetNodeById(8);
                if (!text.IsNullOrEmpty() && buttonNode != null)
                {
                    if (ValidWarpText.Any(x => text.Contains(x, StringComparison.OrdinalIgnoreCase)))
                    {
                        Service.PluginLog.Debug(text);
                        if (buttonNode != null)
                        {
                            Service.AddonEventManager.AddEvent(SYN, (nint)buttonNode, AddonEventType.ButtonClick, WarpConfirmationCheck);
                        }
                    }
                }
            }
        }

        private void WarpConfirmationCheck(AddonEventType atkEventType, nint atkUnitBase, nint atkResNode)
        {
            isReadyWarpTP = true;
        }

        private void WarpTPCheck()
        {
            if (!isReadyWarpTP) return;

            if (Service.Condition[ConditionFlag.BetweenAreas] && Service.Condition[ConditionFlag.BetweenAreas51])
            {
                warpTPBetweenAreas = true;
            }
            else if (Service.Condition[ConditionFlag.BetweenAreas])
            {
                warpTPInAreas = true;
            }

            if (warpTPInAreas)
            {
                if (CheckCurrency(1, currentLocationName, $"({Service.Lang.GetText("TeleportWithinArea", currentLocationName)})", RecordChangeType.Negative))
                {
                    isReadyWarpTP = warpTPBetweenAreas = warpTPInAreas = false;
                }
            }
        }

        private void WarpTPEndCheck()
        {
            if (warpTPBetweenAreas)
            {
                if (CheckCurrency(1, previousLocationName, $"({Service.Lang.GetText("TeleportTo", currentLocationName)})", RecordChangeType.Negative))
                {
                    isReadyWarpTP = warpTPBetweenAreas = warpTPInAreas = false;
                }
            }
        }

        public void UninitWarpCosts()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "SelectYesno", WarpConfirmationCheck);
            isReadyWarpTP = warpTPBetweenAreas = warpTPInAreas = false;
        }
    }
}
