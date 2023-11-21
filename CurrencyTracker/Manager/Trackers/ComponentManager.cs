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
