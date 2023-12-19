namespace CurrencyTracker.Manager.Trackers.Components
{
    public class MobDrops : ITrackerComponent
    {
        public bool Initialized { get; set; } = false;

        private HashSet<string> enemiesList = new();

        private InventoryHandler? inventoryHandler = null;

        public void Init()
        {
            Service.Condition.ConditionChange += OnConditionChange;

            Initialized = true;
        }

        private unsafe void OnConditionChange(ConditionFlag flag, bool value)
        {
            if (flag != ConditionFlag.InCombat || Flags.IsBoundByDuty() || FateManager.Instance()->CurrentFate != null) return;

            if (value)
            {
                BeginMobDropsHandler();
            }
            else
            {
                Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(t => EndMobDropsHandler());
            }

        }

        private void BeginMobDropsHandler()
        {
            HandlerManager.ChatHandler.isBlocked = true;
            inventoryHandler = new();

            Service.Framework.Update += OnFrameworkUpdate;
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            var target = Service.TargetManager.Target;

            if (target != null)
            {
                if (target.ObjectKind != ObjectKind.BattleNpc) return;
                var battleNPC = (BattleNpc)target;
                if ((battleNPC.StatusFlags & (StatusFlags.Hostile | StatusFlags.InCombat | StatusFlags.WeaponOut)) == 0) return;

                if (!enemiesList.Contains(battleNPC.Name.TextValue))
                {
                    enemiesList.Add(battleNPC.Name.TextValue);
                    Service.Log.Debug($"{battleNPC.Name.TextValue}");
                }
            }
        }

        private void EndMobDropsHandler()
        {
            if (Service.Condition[ConditionFlag.InCombat]) return;

            Service.Log.Debug($"Combat Ends, Currency Change Check Starts.");
            Service.Framework.Update -= OnFrameworkUpdate;

            Service.Tracker.CheckCurrencies(inventoryHandler.Items, "",  $"({Service.Lang.GetText("MobDrops-MobDropsNote", string.Join(", ", enemiesList.TakeLast(3)))})", RecordChangeType.All, 8);

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
