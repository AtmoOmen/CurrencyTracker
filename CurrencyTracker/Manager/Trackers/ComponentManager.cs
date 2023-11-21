using CurrencyTracker.Manager.Libs;
using System.Collections.Generic;

namespace CurrencyTracker.Manager.Trackers
{
    public class ComponentManager
    {
        public DutyRewards DutyRewards = null!;
        public Exchange Exchange = null!;
        public FateRewards FateRewards = null!;
        public GoldSaucer GoldSaucer = null!;
        public IslandSanctuary IslandSanctuary = null!;
        public QuestRewards QuestRewards = null!;
        public SpecialExchange SpecialExchange = null!;
        public TeleportCosts TeleportCosts = null!;
        public Trade Trade = null!;
        public TripleTriad TripleTriad = null!;
        public WarpCosts WarpCosts = null!;

        private static List<ITrackerComponent> Components = null!;

        public ComponentManager() 
        {
            DutyRewards = new();
            Exchange = new();
            FateRewards = new();
            GoldSaucer = new();
            IslandSanctuary = new();
            QuestRewards = new();
            SpecialExchange = new();
            TeleportCosts = new();
            Trade = new();
            TripleTriad = new();
            WarpCosts = new();

            Components = new()
            {
                DutyRewards, Exchange, FateRewards, GoldSaucer, IslandSanctuary, QuestRewards, SpecialExchange, TeleportCosts, Trade, TripleTriad, WarpCosts
            };
        }

        public void Init()
        {
            foreach (var component in Components)
            {
                component.Init();
            }
        }

        public void Uninit()
        {
            foreach (var component in Components)
            {
                component.Uninit();
            }
        }
    }
}
