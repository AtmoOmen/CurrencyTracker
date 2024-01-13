using Lumina.Excel.GeneratedSheets2;

namespace CurrencyTracker.Manager.Trackers.Components;

public class WarpCosts : ITrackerComponent
{
    public bool Initialized { get; set; }

    // 有效的 NPC 传送对话内容 Valid Content Shown in Addon
    private static readonly HashSet<string> ValidWarpText = new() { "Gils", "Gil", "金币", "金幣", "ギル" };
    private static readonly uint[] tpCostCurrencies = { 1, 7569 };

    // 包含金币传送点的区域 Terriories that Have a Gil-Cost Warp
    private List<uint> ValidGilWarpTerriories = new();

    private bool isReadyWarpTP;
    private bool warpTPBetweenAreas;
    private bool warpTPInAreas;

    public void Init()
    {
        ValidGilWarpTerriories = Service.DataManager.GetExcelSheet<Warp>()
                                        .Where(x => Service.DataManager.GetExcelSheet<WarpCondition>()
                                                           .Any(y => y.Gil != 0 &&
                                                                     x.WarpCondition.Value.RowId == y.RowId))
                                        .Select(x => x.TerritoryType.Value.RowId)
                                        .ToList();

        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectYesno", WarpConfirmationCheck);

        Initialized = true;
    }

    private unsafe void WarpConfirmationCheck(AddonEvent type, AddonArgs args)
    {
        if (!ValidGilWarpTerriories.Any(x => Service.ClientState.TerritoryType == x)) return;

        var SYN = (AtkUnitBase*)args.Addon;
        if (!IsAddonNodesReady(SYN)) return;

        var text = SYN->GetTextNodeById(2)->NodeText.ToString();
        if (text.IsNullOrEmpty()) return;

        if (ValidWarpText.Any(x => text.Contains(x, StringComparison.OrdinalIgnoreCase)))
        {
            isReadyWarpTP = true;
            HandlerManager.ChatHandler.isBlocked = true;

            Service.Framework.Update += OnFrameworkUpdate;
        }
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!isReadyWarpTP)
        {
            Service.Framework.Update -= OnFrameworkUpdate;
            return;
        }

        switch (Service.Condition[ConditionFlag.BetweenAreas])
        {
            case true when Service.Condition[ConditionFlag.BetweenAreas51]:
                warpTPBetweenAreas = true;
                break;
            case true:
                warpTPInAreas = true;
                break;
        }

        if (Flags.BetweenAreas() || Flags.OccupiedInEvent()) return;

        if (warpTPBetweenAreas)
            Service.Tracker.CheckCurrencies(tpCostCurrencies, PreviousLocationName,
                                            $"({Service.Lang.GetText("TeleportTo", CurrentLocationName)})",
                                            RecordChangeType.Negative, 15);
        else if (warpTPInAreas)
            Service.Tracker.CheckCurrencies(tpCostCurrencies, CurrentLocationName,
                                            $"({Service.Lang.GetText("TeleportWithinArea")})",
                                            RecordChangeType.Negative, 16);

        if (!Flags.BetweenAreas() && !Flags.OccupiedInEvent())
        {
            ResetStates();
            HandlerManager.ChatHandler.isBlocked = false;
        }
    }

    private void ResetStates()
    {
        Service.Framework.Update -= OnFrameworkUpdate;
        isReadyWarpTP = warpTPBetweenAreas = warpTPInAreas = false;
    }

    public void Uninit()
    {
        ValidGilWarpTerriories.Clear();
        ResetStates();

        Initialized = false;
    }
}
