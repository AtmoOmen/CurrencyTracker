using CurrencyTracker.Manager.Libs;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

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
                component.Init();
            }
        }

        public static void Load<T>() where T : ITrackerComponent, new()
        {
            if (!Components.OfType<T>().Any())
            {
                var component = new T();
                component.Init();
                Components.Add(component);
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
        }

        public static void Uninit()
        {
            foreach (var component in Components)
            {
                component.Uninit();
            }
        }
    }
}
