namespace CurrencyTracker.Manager.Trackers.Components
{
    public class MobDrops : ITrackerComponent
    {
        public bool Initialized { get; set; } = false;

        private HashSet<string> enemiesList = new();
        private InventoryHandler? inventoryHandler;

        public void Init()
        {
            Service.Condition.ConditionChange += OnConditionChange;

            Initialized = true;
        }

        private unsafe void OnConditionChange(ConditionFlag flag, bool value)
        {
            if (flag != ConditionFlag.InCombat || Flags.IsBoundByDuty()) return;

            if (value)
            {
                if (FateManager.Instance()->CurrentFate != null || inventoryHandler != null) return;
                HandlerManager.ChatHandler.isBlocked = true;
                inventoryHandler = new();
                Service.Framework.Update += OnFrameworkUpdate;
            }
            else
            {
                Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(t => EndMobDropsHandler());
            }
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            var target = Service.TargetManager.Target;
            if (target is BattleNpc battleNPC && target.ObjectKind == ObjectKind.BattleNpc && (battleNPC.StatusFlags & (StatusFlags.Hostile | StatusFlags.InCombat | StatusFlags.WeaponOut)) != 0 && !enemiesList.Contains(battleNPC.Name.TextValue))
            {
                enemiesList.Add(battleNPC.Name.TextValue);
                Service.Log.Debug($"{battleNPC.Name.TextValue}");
            }
        }

        private void EndMobDropsHandler()
        {
            if (Service.Condition[ConditionFlag.InCombat]) 
            {
                Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(t => EndMobDropsHandler());
                return;
            };

            Service.Log.Debug("Combat Ends, Currency Change Check Starts.");
            Service.Framework.Update -= OnFrameworkUpdate;

            var items = inventoryHandler?.Items ?? new();
            Service.Tracker.CheckCurrencies(items, "", $"({Service.Lang.GetText("MobDrops-MobDropsNote", string.Join(", ", enemiesList.TakeLast(3)))})", RecordChangeType.All, 8);

            enemiesList.Clear();
            HandlerManager.ChatHandler.isBlocked = false;
            HandlerManager.Nullify(ref inventoryHandler);

            Service.Log.Debug("Currency Change Check Completes.");
        }

        public void Uninit()
        {
            Service.Condition.ConditionChange -= OnConditionChange;
            Service.Framework.Update -= OnFrameworkUpdate;
            HandlerManager.Nullify(ref inventoryHandler);

            Initialized = false;
        }
    }
}
