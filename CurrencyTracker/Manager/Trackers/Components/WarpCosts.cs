using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
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
        private static readonly string[] ValidWarpText = { "Gils", "金币", "ギル", "Gil" };
        private List<uint> GilCostsWarpTerriories = new();
        private bool isReadyWarpTP;
        private bool warpTPBetweenAreas;
        private bool warpTPInAreas;

        public static bool BetweenAreas()
        {
            return Service.Condition[ConditionFlag.BetweenAreas] || Service.Condition[ConditionFlag.BetweenAreas51];
        }

        public void InitWarpCosts()
        {
            GilCostsWarpTerriories = Service.DataManager.GetExcelSheet<Warp>()
                .Where(x => Service.DataManager.GetExcelSheet<WarpCondition>()
                .Any(y => y.Gil != 0 && x.WarpCondition.Value.RowId == y.RowId))
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

            if (SYN == nint.Zero) return;

            var text = ((AddonSelectYesno*)SYN)->PromptText->NodeText.ToString();
            var buttonNode = ((AtkUnitBase*)SYN)->GetNodeById(8);
            if (string.IsNullOrEmpty(text) || buttonNode == null) return;

            if (ValidWarpText.Any(x => text.Contains(x, StringComparison.OrdinalIgnoreCase)))
            {
                Service.PluginLog.Debug(text);
                Service.AddonEventManager.AddEvent(SYN, (nint)buttonNode, AddonEventType.ButtonClick, WarpConfirmationCheck);
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

            if (!warpTPInAreas) return;

            if (CheckCurrency(1, currentLocationName, $"({Service.Lang.GetText("TeleportWithinArea", currentLocationName)})", RecordChangeType.Negative))
            {
                ResetWarpFlags();
            }
        }

        private void WarpTPEndCheck()
        {
            if (!warpTPBetweenAreas) return;

            if (CheckCurrency(1, previousLocationName, $"({Service.Lang.GetText("TeleportTo", currentLocationName)})", RecordChangeType.Negative))
            {
                ResetWarpFlags();
            }
        }

        private void ResetWarpFlags()
        {
            isReadyWarpTP = warpTPBetweenAreas = warpTPInAreas = false;
        }

        public void UninitWarpCosts()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "SelectYesno", WarpConfirmationCheck);
            ResetWarpFlags();
        }
    }
}
