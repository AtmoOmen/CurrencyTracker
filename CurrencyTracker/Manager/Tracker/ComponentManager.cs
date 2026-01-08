using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using CurrencyTracker.Trackers;

namespace CurrencyTracker.Manager.Tracker;

public class ComponentManager
{
    private static readonly Dictionary<Type, TrackerComponentBase> Components = [];

    static ComponentManager()
    {
        var componentTypes = Assembly.GetExecutingAssembly().GetTypes()
                                     .Where(t => typeof(TrackerComponentBase).IsAssignableFrom(t) &&
                                                 t is { IsInterface: false, IsAbstract: false });

        foreach (var type in componentTypes)
        {
            if (Activator.CreateInstance(type) is not TrackerComponentBase component) continue;
            Components[type] = component;
        }
    }
    
    public static void Init()
    {
        foreach (var (type, component) in Components)
        {
            if (!Service.Config.ComponentEnabled.TryGetValue(type.Name, out var enabled) || !enabled) continue;
            component.Init();
        }
    }

    public static void Load(TrackerComponentBase component)
    {
        var type = component.GetType();
        if (!Components.ContainsKey(type))
        {
            DService.Instance().Log.Error($"Failed to fetch component {type.Name}");
            return;
        }

        component.Init();
        Service.Config.ComponentEnabled[type.Name] = true;
    }

    public static void Unload(TrackerComponentBase component)
    {
        var type = component.GetType();
        if (!Components.ContainsKey(type)) return;

        component.Uninit();
        Service.Config.ComponentEnabled[type.Name] = false;
    }

    public static void Uninit()
    {
        foreach (var (type, component) in Components)
            component.Uninit();
    }

    public static T? Get<T>() where T : TrackerComponentBase 
        => Components.TryGetValue(typeof(T), out var component) ? (T)component : null;

    public static bool TryGet<T>([NotNullWhen(true)] out T? component) where T : TrackerComponentBase
    {
        if (Components.TryGetValue(typeof(T), out var rawComponent) && rawComponent is T typedComponent)
        {
            component = typedComponent;
            return true;
        }
        
        component = null;
        return false;
    }
}
