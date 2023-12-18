namespace CurrencyTracker.Manager.Trackers
{
    public class HandlerManager
    {
        public static HashSet<ITrackerHandler> Handlers = new();
        public static ChatHandler? ChatHandler;

        public HandlerManager()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetInterfaces().Contains(typeof(ITrackerHandler)) && t.GetConstructor(Type.EmptyTypes) != null);

            foreach (var type in types)
            {
                if (type.Name.Contains("InventoryHandler")) continue;

                var instance = Activator.CreateInstance(type);
                if (instance is ITrackerHandler handler)
                {
                    Handlers.Add(handler);
                }
            }

            ChatHandler = Handlers.OfType<ChatHandler>().FirstOrDefault();
        }

        public static void Init()
        {
            foreach (var handler in Handlers)
            {
                if (!handler.Initialized)
                {
                    handler.Init();
                    Service.Log.Debug($"Loaded {handler.GetType().Name} handler");
                }
                else
                {
                    Service.Log.Debug($"{handler.GetType().Name} has been loaded, skip.");
                }
            }
        }

        public static void Nullify<T>(ref T handler) where T : ITrackerHandler?
        {
            if (handler == null) return;

            handler.Uninit();
            handler = default;
        }
        
        public static void Uninit()
        {
            foreach (var handler in Handlers)
            {
                handler.Uninit();
                Service.Log.Debug($"Unloaded {handler.GetType().Name} module");
            }
        }
    }
}
