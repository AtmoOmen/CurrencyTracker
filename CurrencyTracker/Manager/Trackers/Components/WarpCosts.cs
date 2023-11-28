namespace CurrencyTracker.Manager.Trackers.Components
{
    // 与 TeleportCosts / TerrioryHandler / ChatHandler 联动
    public class WarpCosts : ITrackerComponent
    {
        private bool _initialized = false;

        public bool Initialized
        {
            get { return _initialized; }
            set { _initialized = value; }
        }

        // 有效的 NPC 传送对话内容 Valid Content Shown in Addon
        private static readonly string[] ValidWarpText = { "Gils", "Gil", "金币", "ギル" };

        // 包含金币传送点的区域 Terriories that Have a Gil-Cost Warp
        private List<uint> ValidGilWarpTerriories = new();

        // 是否准备进行 NPC 传送 Is Ready to Have a Warp Teleportation
        private bool isReadyWarpTP;

        // 区域间 NPC 传送 Warp Teleportation Between Areas
        private bool warpTPBetweenAreas;

        // 区域内 NPC 传送 Warp Teleportation Within Areas
        private bool warpTPInAreas;

        public void Init()
        {
            ValidGilWarpTerriories = Service.DataManager.GetExcelSheet<Warp>()
                .Where(x => Service.DataManager.GetExcelSheet<WarpCondition>()
                .Any(y => y.Gil != 0 && x.WarpCondition.Value.RowId == y.RowId))
                .Select(x => x.TerritoryType.Value.RowId)
                .ToList();

            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectYesno", WarpConfirmationCheck);

            _initialized = true;
        }

        private unsafe void WarpConfirmationCheck(AddonEvent type, AddonArgs args)
        {
            if (!ValidGilWarpTerriories.Any(x => Service.ClientState.TerritoryType == x))
            {
                return;
            }

            var SYN = args.Addon;
            if (SYN == nint.Zero) return;

            var text = ((AddonSelectYesno*)SYN)->PromptText->NodeText.ToString();
            if (text.IsNullOrEmpty()) return;

            if (ValidWarpText.Any(x => text.Contains(x, StringComparison.OrdinalIgnoreCase)))
            {
                isReadyWarpTP = true;
                HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = true;

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

            if (Service.Condition[ConditionFlag.BetweenAreas] && Service.Condition[ConditionFlag.BetweenAreas51])
            {
                warpTPBetweenAreas = true;
            }
            else if (Service.Condition[ConditionFlag.BetweenAreas])
            {
                warpTPInAreas = true;
            }

            if (Flags.BetweenAreas() || Flags.OccupiedInEvent()) return;

            if (warpTPBetweenAreas)
            {
                if (Service.Tracker.CheckCurrencies(new uint[] { 1, 7569 }, PreviousLocationName, Plugin.Instance.Configuration.ComponentProp["RecordTeleportDes"] ? $"({Service.Lang.GetText("TeleportTo", CurrentLocationName)})" : "", RecordChangeType.Negative, 15))
                {
                    ResetStates();
                    HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = false;
                    Service.PluginLog.Debug($"Teleport from {PreviousLocationName} to {CurrentLocationName}");
                }
            }
            else if (warpTPInAreas)
            {
                if (Service.Tracker.CheckCurrencies(new uint[] { 1, 7569 }, CurrentLocationName, Plugin.Instance.Configuration.ComponentProp["RecordTeleportDes"] ? $"({Service.Lang.GetText("TeleportWithinArea")})" : "", RecordChangeType.Negative, 16))
                {
                    ResetStates();
                    HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = false;
                }
            }

            if (!Flags.BetweenAreas() && !Flags.OccupiedInEvent())
            {
                ResetStates();
                HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = false;
            }
        }

        private void ResetStates()
        {
            isReadyWarpTP = warpTPBetweenAreas = warpTPInAreas = false;
            Service.Framework.Update -= OnFrameworkUpdate;
        }

        public void Uninit()
        {
            ValidGilWarpTerriories.Clear();
            ResetStates();

            _initialized = false;
        }
    }
}
