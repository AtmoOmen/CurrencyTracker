namespace CurrencyTracker.Manager.Trackers.Components
{
    public class Retainer : ITrackerComponent
    {
        public bool Initialized
        {
            get { return _initialized; }
            set { _initialized = value; }
        }

        public static readonly InventoryType[] RetainerInventories = new InventoryType[]
        {
            InventoryType.RetainerPage1, InventoryType.RetainerPage2, InventoryType.RetainerPage3, InventoryType.RetainerPage4, InventoryType.RetainerGil,
            InventoryType.RetainerCrystals, InventoryType.RetainerPage5, InventoryType.RetainerPage6, InventoryType.RetainerPage7, InventoryType.RetainerMarket
        };

        private bool _initialized = false;

        public void Init()
        {

        }

        public void Uninit()
        {

        }
    }
}
