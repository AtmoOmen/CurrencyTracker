using Dalamud.Game.ClientState.Conditions;

namespace CurrencyTracker.Manager.Tools
{
    public static class Flags
    {
        public static bool OccupiedInEvent()
        {
            return Service.Condition[ConditionFlag.OccupiedInQuestEvent] || Service.Condition[ConditionFlag.OccupiedInEvent] || Service.Condition[ConditionFlag.OccupiedInCutSceneEvent] || Service.Condition[ConditionFlag.OccupiedSummoningBell] || Service.Condition[ConditionFlag.TradeOpen];
        }

        public static bool BetweenAreas()
        {
            return Service.Condition[ConditionFlag.BetweenAreas] || Service.Condition[ConditionFlag.BetweenAreas51];
        }

        public static bool IsBoundByDuty()
        {
            return Service.Condition[ConditionFlag.BoundByDuty] ||
                   Service.Condition[ConditionFlag.BoundByDuty56] ||
                   Service.Condition[ConditionFlag.BoundByDuty95];
        }
    }
}
