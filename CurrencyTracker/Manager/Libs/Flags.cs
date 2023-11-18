using Dalamud.Game.ClientState.Conditions;

namespace CurrencyTracker.Manager
{
    public static class Flags
    {
        public static bool OccupiedInEvent()
        {
            return Service.Condition[ConditionFlag.OccupiedInQuestEvent] || Service.Condition[ConditionFlag.OccupiedInEvent] || Service.Condition[ConditionFlag.OccupiedInCutSceneEvent] || Service.Condition[ConditionFlag.OccupiedSummoningBell];
        }

        public static bool BetweenAreas()
        {
            return Service.Condition[ConditionFlag.BetweenAreas] || Service.Condition[ConditionFlag.BetweenAreas51];
        }
    }
}
