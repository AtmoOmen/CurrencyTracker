using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CurrencyTracker.Manager.Trackers.Handlers;
using CurrencyTracker.Trackers;

namespace CurrencyTracker.Manager.Tracker;

public class HandlerManager
{
    public static HashSet<TrackerHandlerBase> Handlers    { get; private set; } = [];
    
    public static ChatHandler?                ChatHandler { get; private set; }

    private static readonly HashSet<string> BlacklistHandlerNames = ["InventoryHandler"];

    static HandlerManager()
    {
        var types = Assembly.GetExecutingAssembly().GetTypes()
                            .Where(t => typeof(TrackerHandlerBase).IsAssignableFrom(t) &&
                                        t is { IsInterface: false, IsAbstract: false } &&
                                        t.GetConstructor(Type.EmptyTypes) != null);

        foreach (var type in types)
        {
            if (BlacklistHandlerNames.Contains(type.Name)) continue;

            var instance = Activator.CreateInstance(type);
            if (instance is not TrackerHandlerBase handler) continue;
                
            Handlers.Add(handler);
        }

        ChatHandler = Handlers.Where(x => x.GetType() == typeof(ChatHandler))
                              .OfType<ChatHandler>()
                              .FirstOrDefault();
    }
    
    public static void Init()
    {
        foreach (var handler in Handlers)
            handler.Init();
    }

    public static void Nullify<T>(ref T? handler) where T : TrackerHandlerBase?
    {
        if (handler == null) return;

        handler.Uninit();
        handler = null;
    }

    public static void Uninit() => 
        Handlers.ForEach(x => x.Uninit());
}
