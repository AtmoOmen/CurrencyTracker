namespace CurrencyTracker.Manager.Trackers.Components
{
    public class TeleportCosts : ITrackerComponent
    {
        public bool Initialized
        {
            get { return _initialized; }
            set { _initialized = value; }
        }

        private const string ActorControlSig = "E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64";

        private delegate void ActorControlSelfDelegate(uint category, uint eventId, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6, UInt64 targetId, byte param7);

        private Hook<ActorControlSelfDelegate>? actorControlSelfHook;

        private bool _initialized = false;
        private bool isReadyTP = false;
        private bool tpBetweenAreas = false;
        private bool tpInAreas = false;
        
        public void Init()
        {
            var actorControlSelfPtr = Service.SigScanner.ScanText(ActorControlSig);
            actorControlSelfHook = Service.Hook.HookFromAddress<ActorControlSelfDelegate>(actorControlSelfPtr, ActorControlSelf);
            actorControlSelfHook.Enable();

            _initialized = true;
        }

        private void ActorControlSelf(uint category, uint eventId, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6, ulong targetId, byte param7)
        {
            actorControlSelfHook.Original(category, eventId, param1, param2, param3, param4, param5, param6, targetId, param7);

            if (eventId != 517)
                return;

            try
            {
                if (param1 == 4590 || param1 == 4591)
                {
                    ComponentManager.Components.OfType<TeleportCosts>().FirstOrDefault().TeleportWithCost();
                }
            }
            catch (Exception e)
            {
                Service.PluginLog.Warning(e.Message);
                Service.PluginLog.Warning(e.StackTrace ?? "Unknown");
            }
        }


        public void TeleportWithCost()
        {
            HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = true;

            isReadyTP = true;

            Service.Framework.Update += OnFrameworkUpdate;
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            if (!isReadyTP)
            {
                Service.Framework.Update -= OnFrameworkUpdate;
                return;
            }

            if (Service.Condition[ConditionFlag.BetweenAreas] && Service.Condition[ConditionFlag.BetweenAreas51])
            {
                tpBetweenAreas = true;
            }
            else if (Service.Condition[ConditionFlag.BetweenAreas])
            {
                tpInAreas = true;
            }

            if (Flags.BetweenAreas() || Flags.OccupiedInEvent()) return;

            if (tpBetweenAreas)
            {
                if (Service.Tracker.CheckCurrency(1, PreviousLocationName, Plugin.Instance.Configuration.ComponentProp["RecordTeleportDes"] ? $"({Service.Lang.GetText("TeleportTo", CurrentLocationName)})" : "", RecordChangeType.Negative, 19))
                {
                    ResetStates();
                    HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = false;
                    Service.PluginLog.Debug($"Teleport from {PreviousLocationName} to {CurrentLocationName}");
                }
            }
            else if (tpInAreas)
            {
                if (Service.Tracker.CheckCurrency(1, CurrentLocationName, Plugin.Instance.Configuration.ComponentProp["RecordTeleportDes"] ? $"({Service.Lang.GetText("TeleportWithinArea")})" : "", RecordChangeType.Negative, 20))
                {
                    ResetStates();
                    HandlerManager.Handlers.OfType<ChatHandler>().FirstOrDefault().isBlocked = false;
                }
            }
        }

        private void ResetStates()
        {
            isReadyTP = tpBetweenAreas = tpInAreas = false;
            Service.Framework.Update -= OnFrameworkUpdate;
        }

        public void Uninit()
        {
            ResetStates();

            actorControlSelfHook.Dispose();
            _initialized = false;
        }
    }
}
