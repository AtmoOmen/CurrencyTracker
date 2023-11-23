using CurrencyTracker.Manager.Libs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
                if (!component.Initialized)
                {
                    component.Init();
                    Service.PluginLog.Debug($"Loaded {component.GetType().Name} module");
                }
                else
                {
                    Service.PluginLog.Debug($"{component.GetType().Name} has been loaded, skip.");
                }
            }
        }

        public static void Load<T>() where T : ITrackerComponent, new()
        {
            if (!Components.OfType<T>().Any())
            {
                var component = new T();

                if (!component.Initialized)
                {
                    component.Init();
                    Components.Add(component);
                    Service.PluginLog.Debug($"Loaded {typeof(T).Name} module");
                }
                else
                {
                    Service.PluginLog.Debug($"{component.GetType().Name} has been loaded, skip.");
                }
            }
        }

        public static void Unload<T>() where T : ITrackerComponent
        {
            var component = Components.OfType<T>().FirstOrDefault();
            if (component != null)
            {
                component.Uninit();
                Components.Remove(component);
            }
            Service.PluginLog.Debug($"Unloaded {typeof(T).Name} module");
        }

        public static void Uninit()
        {
            foreach (var component in Components)
            {
                component.Uninit();
                Service.PluginLog.Debug($"Unloaded {component.GetType().Name} module");
            }
        }
    }
}
