namespace CurrencyTracker.Manager.Trackers.Components
{
    public class MobDrops : ITrackerComponent
    {
        private bool _initialized = false;

        public bool Initialized
        {
            get { return _initialized; }
            set { _initialized = value; }
        }

        private bool inCombat = false;
        private List<string> enemiesList = new();

        public void Init()
        {
            Service.Condition.ConditionChange += OnConditionChange;

            _initialized = true;
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            if (!inCombat) return;

            var target = Service.TargetManager.Target;

            if (target != null)
            {
                if (target.ObjectKind != ObjectKind.BattleNpc && target.ObjectKind != ObjectKind.EventNpc) return;

                if (enemiesList.Contains(target.Name.TextValue)) return;

                enemiesList.Add(target.Name.TextValue);
            }
        }

        private void OnConditionChange(ConditionFlag flag, bool value)
        {
            if (Flags.IsBoundByDuty() || Flags.OccupiedInEvent() || Flags.BetweenAreas()) return;

            if (flag == ConditionFlag.InCombat && value)
            {
                inCombat = true;
                BeginMobDropsHandler();
            }
            else if (flag == ConditionFlag.InCombat && !value)
            {
                Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(t => EndMobDropsHandler());
            }
        }

        private void BeginMobDropsHandler()
        {
            inCombat = true;
            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = true;

            Service.Framework.Update += OnFrameworkUpdate;
        }

        private void EndMobDropsHandler()
        {
            if (Service.Condition[ConditionFlag.InCombat]) return;

            Service.PluginLog.Debug($"Combat Ends, Currency Change Check Starts.");

            Parallel.ForEach(Plugin.Instance.Configuration.AllCurrencies, currency =>
            {
                Service.Tracker.CheckCurrency(currency.Key, "", $"(Drops from {string.Join(", ", enemiesList.TakeLast(3))})");
            });

            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = false;

            inCombat = false;

            enemiesList.Clear();

            Service.Framework.Update -= OnFrameworkUpdate;
            Service.PluginLog.Debug("Currency Change Check Completes.");
        }

        public void Uninit()
        {
            inCombat = false;

            Service.Condition.ConditionChange -= OnConditionChange;
            Service.Framework.Update -= OnFrameworkUpdate;
            _initialized = false;
        }
    }
}
