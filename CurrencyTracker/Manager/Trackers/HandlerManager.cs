namespace CurrencyTracker.Manager.Trackers
{
    public class HandlerManager
    {
        public static List<ITrackerHandler> Handlers = new();

        public HandlerManager()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetInterfaces().Contains(typeof(ITrackerHandler)) && t.GetConstructor(Type.EmptyTypes) != null);

            foreach (var type in types)
            {
                var instance = Activator.CreateInstance(type);
                if (instance is ITrackerHandler handler)
                {
                    Handlers.Add(handler);
                }
            }
        }

        public static void Init()
        {
            foreach (var handler in Handlers)
            {
                if (!handler.Initialized)
                {
                    handler.Init();
                    Service.PluginLog.Debug($"Loaded {handler.GetType().Name} handler");
                }
                else
                {
                    Service.PluginLog.Debug($"{handler.GetType().Name} has been loaded, skip.");
                }
            }
        }

        public static void Load<T>() where T : ITrackerHandler, new()
        {
            if (!Handlers.OfType<T>().Any())
            {
                var handler = new T();

                if (!handler.Initialized)
                {
                    handler.Init();
                    Handlers.Add(handler);
                    Service.PluginLog.Debug($"Loaded {typeof(T).Name} handler");
                }
                else
                {
                    Service.PluginLog.Debug($"{handler.GetType().Name} has been loaded, skip.");
                }
            }
        }

        public static void Unload<T>() where T : ITrackerHandler
        {
            var handler = Handlers.OfType<T>().FirstOrDefault();
            if (handler != null)
            {
                handler.Uninit();
                Handlers.Remove(handler);
            }
            Service.PluginLog.Debug($"Unloaded {typeof(T).Name} module");
        }

        public static void Uninit()
        {
            foreach (var handler in Handlers)
            {
                handler.Uninit();
                Service.PluginLog.Debug($"Unloaded {handler.GetType().Name} module");
            }
        }
    }
}
