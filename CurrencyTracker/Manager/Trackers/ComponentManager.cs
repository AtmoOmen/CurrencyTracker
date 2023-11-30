namespace CurrencyTracker.Manager.Trackers
{
    public class ComponentManager
    {
        public static List<ITrackerComponent> Components = new();

        public ComponentManager()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetInterfaces().Contains(typeof(ITrackerComponent)) && t.GetConstructor(Type.EmptyTypes) != null);

            foreach (var type in types)
            {
                var instance = Activator.CreateInstance(type);
                if (instance is ITrackerComponent component)
                {
                    Components.Add(component);
                }
            }
        }

        public static void Init()
        {
            foreach (var component in Components)
            {
                if (Plugin.Instance.Configuration.ComponentEnabled.TryGetValue(component.GetType().Name, out var enabled))
                {
                    if (!enabled) continue;
                }
                else
                {
                    Service.Log.Warning($"Fail to get component {component.GetType().Name} configurations");
                }

                try
                {
                    if (!component.Initialized)
                    {
                        component.Init();
                        Service.Log.Debug($"Loaded {component.GetType().Name} module");
                    }
                    else
                    {
                        Service.Log.Debug($"{component.GetType().Name} has been loaded, skip.");
                    }
                }
                catch (Exception ex)
                {
                    Service.Log.Error($"Failed to load component {component.GetType().Name} due to error: {ex.Message}");
                    Service.Log.Warning(ex.StackTrace ?? "Unknown");
                }
            }
        }

        public static void Load(ITrackerComponent component)
        {
            if (Components.Contains(component))
            {
                try
                {
                    if (!component.Initialized)
                    {
                        component.Init();
                        Service.Log.Debug($"Loaded {component.GetType().Name} module");
                    }
                    else
                    {
                        Service.Log.Debug($"{component.GetType().Name} has been loaded, skip.");
                    }
                }
                catch (Exception ex)
                {
                    Service.Log.Error($"Failed to load component {component.GetType().Name} due to error: {ex.Message}");
                }
            }
            else
            {
                Service.Log.Error($"Fail to fetch component {component}");
            }
        }

        public static void Unload(ITrackerComponent component)
        {
            if (Components.Contains(component))
            {
                component.Uninit();
                Service.Log.Debug($"Unloaded {component.GetType().Name} module");
            }
        }

        public static void Uninit()
        {
            foreach (var component in Components)
            {
                component.Uninit();
                Service.Log.Debug($"Unloaded {component.GetType().Name} module");
            }
        }
    }
}
