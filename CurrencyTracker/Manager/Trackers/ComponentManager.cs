using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CurrencyTracker.Manager.Infos;
using CurrencyTracker.Manager.Trackers.Components;

namespace CurrencyTracker.Manager.Trackers;

public class ComponentManager
{
    public static Dictionary<Type, ITrackerComponent> Components { get; private set; } = new();

    public static void Init()
    {
        if (!Components.Any())
        {
            var types = Assembly.GetExecutingAssembly().GetTypes()
                                .Where(t => t.GetInterfaces().Contains(typeof(ITrackerComponent)) &&
                                            t.GetConstructor(Type.EmptyTypes) != null);

            foreach (var type in types)
            {
                var instance = Activator.CreateInstance(type);
                if (instance is ITrackerComponent component) Components.TryAdd(type, component);
            }
        }

        foreach (var component in Components)
        {
            if (Service.Config.ComponentEnabled.TryGetValue(component.Key.Name, out var enabled))
            {
                if (!enabled) continue;
            }
            else
            {
                Service.Log.Warning($"Fail to get component {component.Key.Name} configurations, skip loading");
                continue;
            }

            try
            {
                if (!component.Value.Initialized)
                {
                    component.Value.Init();
                    component.Value.Initialized = true;
                    Service.Log.Debug($"Loaded {component.Key.Name} module");
                }
                else
                    Service.Log.Debug($"{component.Key.Name} has been loaded, skip.");
            }
            catch (Exception ex)
            {
                component.Value.Uninit();
                component.Value.Initialized = false;
                Service.Config.ComponentEnabled[component.Key.Name] = false;

                Service.Log.Error($"Failed to load component {component.Key.Name} due to error: {ex.Message}");
                Service.Log.Error(ex.StackTrace ?? "Unknown");
            }
        }
    }

    public static void Load(ITrackerComponent component)
    {
        if (Components.ContainsValue(component))
        {
            try
            {
                if (!component.Initialized)
                {
                    component.Init();
                    component.Initialized = true;
                    Service.Config.ComponentEnabled[component.GetType().Name] = true;

                    Service.Log.Debug($"Loaded {component.GetType().Name} module");
                }
                else
                    Service.Log.Debug($"{component.GetType().Name} has been loaded, skip.");
            }
            catch (Exception ex)
            {
                component.Uninit();
                component.Initialized = false;
                Service.Config.ComponentEnabled[component.GetType().Name] = false;

                Service.Log.Error($"Failed to load component {component.GetType().Name} due to error: {ex.Message}");
                Service.Log.Error($"{ex.StackTrace}");
            }
        }
        else
            Service.Log.Error($"Fail to fetch component {component}");
    }

    public static void Unload(ITrackerComponent component)
    {
        try
        {
            if (Components.ContainsValue(component))
            {
                component.Uninit();
                component.Initialized = false;
                Service.Config.ComponentEnabled[component.GetType().Name] = false;

                Service.Log.Debug($"Unloaded {component.GetType().Name} module");
            }
        }
        catch (Exception ex)
        {
            Service.Config.ComponentEnabled[component.GetType().Name] = false;
            Service.Log.Error($"Failed to unload component {component.GetType().Name} due to error: {ex.Message}");
            Service.Log.Error($"{ex.StackTrace}");
        }
    }

    public static void Uninit()
    {
        foreach (var component in Components)
            try
            {
                component.Value.Uninit();
                component.Value.Initialized = false;
                Service.Log.Debug($"Unloaded {component.Key.Name} module");
            }
            catch (Exception ex)
            {
                Service.Config.ComponentEnabled[component.Key.Name] = false;
                Service.Log.Error($"Failed to unload component {component.Key.Name} due to error: {ex.Message}");
                Service.Log.Error($"{ex.StackTrace}");
            }

        ServerBar.DtrEntry?.Dispose();
    }
}
