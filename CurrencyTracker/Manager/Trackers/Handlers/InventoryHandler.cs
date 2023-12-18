namespace CurrencyTracker.Manager.Trackers.Handlers
{
    public class InventoryHandler : ITrackerHandler
    {
        public bool Initialized
        {
            get { return _initialized; }
            set { _initialized = value; }
        }

        public HashSet<uint> Items
        {
            get { return _items; }
            set { _items = value; }
        }

        public bool isBlocked
        {
            get { return _isBlocked; }
            set { _isBlocked = value; }
        }

        private HashSet<uint> _items = new();

        private bool _initialized = false;
        private bool _isBlocked = false;


        public InventoryHandler()
        {
            Init();
        }

        public void Init()
        {
            Service.GameInventory.InventoryChangedRaw += OnInventoryChangedRaw;

            _initialized = true;
        }

        private void OnInventoryChangedRaw(IReadOnlyCollection<InventoryEventArgs> events)
        {
            foreach (var eventArgs in events)
            {
                _items.Add(eventArgs.Item.ItemId);
                if (Plugin.Instance.PluginInterface.IsDev) Service.Log.Debug(eventArgs.ToString());
            }
        }

        public void Uninit()
        {
            Service.GameInventory.InventoryChangedRaw -= OnInventoryChangedRaw;

            _items.Clear();
            _initialized = false;
        }
    }
}
