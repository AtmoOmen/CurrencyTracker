using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CurrencyTracker.Infos;

namespace CurrencyTracker.Manager.Trackers;

public class ComponentManager
{
    private static readonly Dictionary<Type, ITrackerComponent> Components = [];

    public static void Init()
    {
        if (Components.Count != 0) return;

        var componentTypes = Assembly.GetExecutingAssembly().GetTypes()
                                     .Where(t => typeof(ITrackerComponent).IsAssignableFrom(t) &&
                                                 t is { IsInterface: false, IsAbstract: false });

        foreach (var type in componentTypes)
            if (Activator.CreateInstance(type) is ITrackerComponent component)
                Components[type] = component;

        foreach (var (type, component) in Components)
        {
            if (!Service.Config.ComponentEnabled.TryGetValue(type.Name, out var enabled) || !enabled)
            {
                DService.Log.Warning($"Component {type.Name} is not enabled or configuration is missing. Skipping.");
                continue;
            }

            try
            {
                if (!component.Initialized)
                {
                    component.Init();
                    component.Initialized = true;
                    DService.Log.Debug($"Loaded {type.Name} module");
                }
                else DService.Log.Debug($"{type.Name} has already been loaded. Skipping.");
            }
            catch (Exception ex)
            {
                HandleComponentError(component, ex);
            }
        }
    }

    public static void Load(ITrackerComponent component)
    {
        var type = component.GetType();
        if (!Components.ContainsKey(type))
        {
            DService.Log.Error($"Failed to fetch component {type.Name}");
            return;
        }

        try
        {
            if (!component.Initialized)
            {
                component.Init();
                component.Initialized = true;
                Service.Config.ComponentEnabled[type.Name] = true;
                DService.Log.Debug($"Loaded {type.Name} module");
            }
            else DService.Log.Debug($"{type.Name} has already been loaded. Skipping.");
        }
        catch (Exception ex)
        {
            HandleComponentError(component, ex);
        }
    }

    public static void Unload(ITrackerComponent component)
    {
        var type = component.GetType();
        if (!Components.ContainsKey(type)) return;

        try
        {
            component.Uninit();
            component.Initialized = false;
            Service.Config.ComponentEnabled[type.Name] = false;
            DService.Log.Debug($"Unloaded {type.Name} module");
        }
        catch (Exception ex)
        {
            HandleComponentError(component, ex);
        }
    }

    public static void Uninit()
    {
        foreach (var (type, component) in Components)
        {
            try
            {
                component.Uninit();
                component.Initialized = false;
                DService.Log.Debug($"Unloaded {type.Name} module");
            }
            catch (Exception ex)
            {
                HandleComponentError(component, ex);
            }
        }
    }

    public static T Get<T>() where T : ITrackerComponent 
        => Components.TryGetValue(typeof(T), out var component) ? (T)component : default;

    public static bool TryGet<T>(out T component) where T : ITrackerComponent
    {
        if (Components.TryGetValue(typeof(T), out var rawComponent) && rawComponent is T typedComponent)
        {
            component = typedComponent;
            return true;
        }
        component = default;
        return false;
    }

    private static void HandleComponentError(ITrackerComponent component, Exception ex)
    {
        var type = component.GetType();
        component.Uninit();
        component.Initialized = false;
        Service.Config.ComponentEnabled[type.Name] = false;
        DService.Log.Error(ex, $"Error in component {type.Name}");
    }
}
